using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace FilmAPI.Services;

public class AuthService : IAuthService
{
    private readonly FilmDbContext _context;
    private readonly IAccountTokenService _tokenService;
    private readonly IAccountEmailService _accountEmail;
    private readonly IUserSecurityAuditService _audit;
    private readonly string _jwtSecret;
    private readonly string _jwtIssuer;
    private readonly string _jwtAudience;
    private readonly int _accessTokenExpiryMinutes;
    private readonly int _refreshTokenExpiryDays;
    private readonly string _frontendBaseUrl;
    private const string DefaultDeviceId = "web-default";

    public AuthService(
        FilmDbContext context,
        IAccountTokenService tokenService,
        IAccountEmailService accountEmail,
        IUserSecurityAuditService audit)
    {
        _context = context;
        _tokenService = tokenService;
        _accountEmail = accountEmail;
        _audit = audit;
        _jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET")
            ?? throw new InvalidOperationException("JWT_SECRET environment variable is required but not set.");
        _jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "RedCurtainAPI";
        _jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "RedCurtainWeb";
        _accessTokenExpiryMinutes = int.Parse(Environment.GetEnvironmentVariable("JWT_ACCESS_TOKEN_EXPIRY_MINUTES") ?? "15");
        _refreshTokenExpiryDays = int.Parse(Environment.GetEnvironmentVariable("JWT_REFRESH_TOKEN_EXPIRY_DAYS") ?? "7");
        _frontendBaseUrl = Environment.GetEnvironmentVariable("FRONTEND_BASE_URL") ?? "http://localhost:5001";
    }

    public async Task<AuthResponseDTO> RegisterAsync(RegisterRequestDTO dto)
    {
        if (!dto.AcceptTerms || !dto.AcceptPrivacy)
        {
            throw new InvalidOperationException("E necessario accettare i Termini di Servizio e la Privacy Policy per registrarsi.");
        }

        if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 8)
        {
            throw new ArgumentException("La password deve essere di almeno 8 caratteri.");
        }

        if (!System.Text.RegularExpressions.Regex.IsMatch(dto.Password, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$"))
        {
            throw new ArgumentException("La password deve contenere almeno una maiuscola, una minuscola e un numero.");
        }

        var normalizedEmail = dto.Email.Trim().ToUpperInvariant();
        var exists = await _context.Users.AnyAsync(u => u.NormalizedEmail == normalizedEmail);
        if (exists)
        {
            throw new InvalidOperationException("Email gia registrata");
        }

        var now = DateTime.UtcNow;
        var user = new User
        {
            Email = dto.Email.Trim(),
            NormalizedEmail = normalizedEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            LocalCredentialsEnabled = true,
            Nome = dto.Nome,
            Cognome = dto.Cognome,
            Telefono = dto.Telefono,
            Ruolo = UserRole.User,
            DataRegistrazione = now,
            CreditoResiduo = 0,
            AuthVersion = 1,
            FailedLoginAttempts = 0,
            PrivacyConsentAtUtc = now,
            TermsAcceptedAtUtc = now
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var accessToken = GenerateAccessToken(user);
        var refreshToken = await GenerateRefreshTokenAsync(user.Id, dto.DeviceId);
        await _context.SaveChangesAsync();

        var verifyTtl = TimeSpan.FromHours(24);
        var verifyRawToken = await _tokenService.CreateTokenAsync(user.Id, AccountActionTokenPurpose.EmailVerification, verifyTtl);
        var verifyUrl = $"{_frontendBaseUrl}/verifica-email.html?token={Uri.EscapeDataString(verifyRawToken)}";
        _ = _accountEmail.SendEmailVerificationAsync(user, verifyUrl);

        return new AuthResponseDTO
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            ExpiresAt = refreshToken.ExpiresAt,
            User = MapUserInfo(user)
        };
    }

    public async Task<AuthResponseDTO> LoginAsync(LoginRequestDTO dto)
    {
        var normalizedEmail = dto.Email.Trim().ToUpperInvariant();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail);
        if (user is null || user.IsDisabled || !user.LocalCredentialsEnabled || user.PasswordHash is null)
        {
            throw new UnauthorizedAccessException("Credenziali non valide");
        }

        if (user.LockedOutUntilUtc.HasValue && user.LockedOutUntilUtc.Value > DateTime.UtcNow)
        {
            var remainingSeconds = (int)(user.LockedOutUntilUtc.Value - DateTime.UtcNow).TotalSeconds;
            throw new UnauthorizedAccessException($"Account temporaneamente bloccato. Riprova tra {remainingSeconds} secondi.");
        }

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            user.FailedLoginAttempts++;
            if (user.FailedLoginAttempts >= 5)
            {
                user.LockedOutUntilUtc = DateTime.UtcNow.AddMinutes(15);
            }
            await _context.SaveChangesAsync();
            throw new UnauthorizedAccessException("Credenziali non valide");
        }

        user.FailedLoginAttempts = 0;
        user.LockedOutUntilUtc = null;
        user.LastLoginAtUtc = DateTime.UtcNow;
        user.LastLoginProvider = "local";

        var accessToken = GenerateAccessToken(user);
        var refreshToken = await GenerateRefreshTokenAsync(user.Id, dto.DeviceId);
        await _context.SaveChangesAsync();

        return new AuthResponseDTO
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            ExpiresAt = refreshToken.ExpiresAt,
            User = MapUserInfo(user)
        };
    }

    public async Task<AuthResponseDTO> RefreshAsync(string refreshToken, string? deviceId)
    {
        var storedToken = await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

        if (storedToken is null || !storedToken.IsActive)
        {
            throw new UnauthorizedAccessException("Refresh token non valido o scaduto");
        }

        var normalizedDeviceId = NormalizeDeviceId(deviceId);
        if (!string.Equals(storedToken.DeviceId, normalizedDeviceId, StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException("Refresh token non valido per questo device");
        }

        storedToken.RevokedAt = DateTime.UtcNow;

        var newRefreshToken = await GenerateRefreshTokenAsync(storedToken.UserId, normalizedDeviceId);
        var accessToken = GenerateAccessToken(storedToken.User!);

        await _context.SaveChangesAsync();

        return new AuthResponseDTO
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken.Token,
            ExpiresAt = newRefreshToken.ExpiresAt,
            User = MapUserInfo(storedToken.User!)
        };
    }

    public async Task<bool> LogoutAsync(string refreshToken, string? deviceId)
    {
        var normalizedDeviceId = NormalizeDeviceId(deviceId);
        var storedToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken && rt.DeviceId == normalizedDeviceId);

        if (storedToken is null) return false;

        storedToken.RevokedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<UserInfoDTO?> GetUserByIdAsync(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user is null) return null;

        return MapUserInfo(user);
    }

    private string GenerateAccessToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("role", user.Ruolo.ToString()),
            new Claim("nome", user.Nome),
            new Claim("auth_version", user.AuthVersion.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _jwtIssuer,
            audience: _jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_accessTokenExpiryMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<RefreshToken> GenerateRefreshTokenAsync(int userId, string? deviceId)
    {
        var normalizedDeviceId = NormalizeDeviceId(deviceId);

        var activeTokensForDevice = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.DeviceId == normalizedDeviceId && rt.RevokedAt == null && rt.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();

        foreach (var token in activeTokensForDevice)
        {
            token.RevokedAt = DateTime.UtcNow;
        }

        var refreshToken = new RefreshToken
        {
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            UserId = userId,
            DeviceId = normalizedDeviceId,
            ExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenExpiryDays),
            CreatedAt = DateTime.UtcNow
        };

        _context.RefreshTokens.Add(refreshToken);
        return refreshToken;
    }

    private static string NormalizeDeviceId(string? deviceId)
    {
        return string.IsNullOrWhiteSpace(deviceId)
            ? DefaultDeviceId
            : deviceId.Trim();
    }

    private static UserInfoDTO MapUserInfo(User user)
    {
        return new UserInfoDTO
        {
            Id = user.Id,
            Email = user.Email,
            Nome = user.Nome,
            Cognome = user.Cognome,
            Telefono = user.Telefono,
            Ruolo = user.Ruolo.ToString(),
            DataRegistrazione = user.DataRegistrazione,
            EmailVerified = user.EmailVerifiedAtUtc != null,
            PrivacyConsentAtUtc = user.PrivacyConsentAtUtc,
            TermsAcceptedAtUtc = user.TermsAcceptedAtUtc
        };
    }

    public async Task ChangePasswordAsync(int userId, ChangePasswordRequestDTO dto)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user is null || user.IsDisabled)
            throw new UnauthorizedAccessException("Utente non trovato.");

        if (!user.LocalCredentialsEnabled || user.PasswordHash is null)
            throw new InvalidOperationException("Impossibile cambiare password senza credenziali locali. Usa il recupero password via email.");

        if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
            throw new UnauthorizedAccessException("Password attuale non corretta.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.PasswordChangedAtUtc = DateTime.UtcNow;
        user.AuthVersion++;
        user.MustChangePassword = false;

        await RevokeAllRefreshTokensAsync(userId);

        await _audit.LogAsync(userId, userId, "PasswordChanged");

        await _context.SaveChangesAsync();
    }

    public async Task<string> RequestPasswordResetAsync(ForgotPasswordRequestDTO dto)
    {
        var normalizedEmail = dto.Email.Trim().ToUpperInvariant();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail && !u.IsDisabled);

        if (user is null)
            return string.Empty;

        var ttl = TimeSpan.FromMinutes(int.Parse(Environment.GetEnvironmentVariable("PASSWORD_RESET_TOKEN_TTL_MINUTES") ?? "30"));
        var token = await _tokenService.CreateTokenAsync(user.Id, AccountActionTokenPurpose.PasswordReset, ttl);

        var resetUrl = $"{_frontendBaseUrl}/reimposta-password.html?token={Uri.EscapeDataString(token)}";
        await _accountEmail.SendPasswordResetAsync(user, resetUrl);

        await _audit.LogAsync(user.Id, null, "PasswordResetRequested");

        return token;
    }

    public async Task<AuthResponseDTO> ResetPasswordAsync(ResetPasswordRequestDTO dto, string? deviceId)
    {
        var (userId, token) = await _tokenService.ValidateTokenAsync(dto.Token, AccountActionTokenPurpose.PasswordReset);
        var user = await _context.Users.FindAsync(userId);
        if (user is null || user.IsDisabled)
            throw new UnauthorizedAccessException("Utente non valido.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.LocalCredentialsEnabled = true;
        user.PasswordChangedAtUtc = DateTime.UtcNow;
        user.AuthVersion++;
        user.MustChangePassword = false;

        await RevokeAllRefreshTokensAsync(userId);
        await _tokenService.ConsumeTokenAsync(token.Id);

        var accessToken = GenerateAccessToken(user);
        var refreshToken = await GenerateRefreshTokenAsync(user.Id, deviceId);

        await _audit.LogAsync(userId, null, "PasswordResetCompleted");

        await _context.SaveChangesAsync();

        return new AuthResponseDTO
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            ExpiresAt = refreshToken.ExpiresAt,
            User = MapUserInfo(user)
        };
    }

    public async Task<AccountSecurityDTO> GetAccountSecurityAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user is null)
            throw new UnauthorizedAccessException("Utente non trovato.");

        var providers = await _context.UserExternalLogins
            .Where(l => l.UserId == userId && l.RevokedAtUtc == null)
            .Select(l => l.Provider)
            .ToListAsync();

        return new AccountSecurityDTO
        {
            HasLocalPassword = user.LocalCredentialsEnabled && !string.IsNullOrEmpty(user.PasswordHash),
            PasswordChangedAtUtc = user.PasswordChangedAtUtc,
            LastLoginAtUtc = user.LastLoginAtUtc,
            LastLoginProvider = user.LastLoginProvider,
            ConnectedProviders = providers.Select(p => p.ToString()).ToList()
        };
    }

    public async Task RequestSetPasswordAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user is null || user.IsDisabled)
            throw new UnauthorizedAccessException("Utente non trovato.");

        if (user.LocalCredentialsEnabled && !string.IsNullOrEmpty(user.PasswordHash))
            throw new InvalidOperationException("L'account ha gia una password locale.");

        var ttl = TimeSpan.FromMinutes(int.Parse(Environment.GetEnvironmentVariable("SET_PASSWORD_TOKEN_TTL_MINUTES") ?? "60"));
        var token = await _tokenService.CreateTokenAsync(user.Id, AccountActionTokenPurpose.SetPassword, ttl);

        var setupUrl = $"{_frontendBaseUrl}/reimposta-password.html?token={Uri.EscapeDataString(token)}&type=setpassword";
        await _accountEmail.SendSetPasswordAsync(user, setupUrl);

        await _audit.LogAsync(userId, null, "SetPasswordRequested");
    }

    public async Task<AuthResponseDTO> SetPasswordAsync(ResetPasswordRequestDTO dto, string? deviceId)
    {
        var (userId, token) = await _tokenService.ValidateTokenAsync(dto.Token, AccountActionTokenPurpose.SetPassword);
        var user = await _context.Users.FindAsync(userId);
        if (user is null || user.IsDisabled)
            throw new UnauthorizedAccessException("Utente non valido.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.LocalCredentialsEnabled = true;
        user.PasswordChangedAtUtc = DateTime.UtcNow;
        user.AuthVersion++;
        user.MustChangePassword = false;

        await RevokeAllRefreshTokensAsync(userId);
        await _tokenService.ConsumeTokenAsync(token.Id);

        var accessToken = GenerateAccessToken(user);
        var refreshToken = await GenerateRefreshTokenAsync(user.Id, deviceId);

        await _audit.LogAsync(userId, null, "SetPasswordCompleted");

        await _context.SaveChangesAsync();

        return new AuthResponseDTO
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            ExpiresAt = refreshToken.ExpiresAt,
            User = MapUserInfo(user)
        };
    }

    public async Task VerifyEmailAsync(string token)
    {
        var (userId, tokenEntity) = await _tokenService.ValidateTokenAsync(token, AccountActionTokenPurpose.EmailVerification);
        var user = await _context.Users.FindAsync(userId);
        if (user is null || user.IsDisabled)
            throw new UnauthorizedAccessException("Utente non valido.");

        if (user.EmailVerifiedAtUtc != null)
            return;

        user.EmailVerifiedAtUtc = DateTime.UtcNow;
        await _tokenService.ConsumeTokenAsync(tokenEntity.Id);
        await _audit.LogAsync(userId, null, "EmailVerified");
        await _context.SaveChangesAsync();
    }

    public async Task<AuthResponseDTO> GenerateTokensAsync(User user, string? deviceId)
    {
        user.FailedLoginAttempts = 0;
        user.LockedOutUntilUtc = null;
        user.LastLoginAtUtc = DateTime.UtcNow;

        var accessToken = GenerateAccessToken(user);
        var refreshToken = await GenerateRefreshTokenAsync(user.Id, deviceId);
        await _context.SaveChangesAsync();

        return new AuthResponseDTO
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            ExpiresAt = refreshToken.ExpiresAt,
            User = MapUserInfo(user)
        };
    }

    private async Task RevokeAllRefreshTokensAsync(int userId)
    {
        var tokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
            .ToListAsync();

        foreach (var token in tokens)
        {
            token.RevokedAt = DateTime.UtcNow;
        }
    }
}
