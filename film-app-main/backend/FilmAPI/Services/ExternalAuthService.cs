using System.Security.Cryptography;
using System.Text;
using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Services;

public interface IExternalAuthService
{
    Task<string> StartAsync(ExternalLoginProvider provider, string redirectPath);
    Task<(User User, string RedirectPath)> CallbackAsync(ExternalLoginProvider provider, string state, string code);
    Task<AuthResponseDTO> ExchangeAsync(string code, string? deviceId);
    List<ExternalProviderDTO> GetProviders();
    Task<List<UserExternalLoginDTO>> GetUserExternalLoginsAsync(int userId);
}

public class ExternalAuthService : IExternalAuthService
{
    private readonly FilmDbContext _context;
    private readonly GoogleExternalAuthProvider _google;
    private readonly MicrosoftExternalAuthProvider _microsoft;
    private readonly IAuthService _auth;
    private readonly IUserSecurityAuditService _audit;
    private readonly string _baseRedirectUri;

    public ExternalAuthService(
        FilmDbContext context,
        GoogleExternalAuthProvider google,
        MicrosoftExternalAuthProvider microsoft,
        IAuthService auth,
        IUserSecurityAuditService audit)
    {
        _context = context;
        _google = google;
        _microsoft = microsoft;
        _auth = auth;
        _audit = audit;
        _baseRedirectUri = Environment.GetEnvironmentVariable("GOOGLE_OAUTH_REDIRECT_URI")
            ?? Environment.GetEnvironmentVariable("MICROSOFT_OAUTH_REDIRECT_URI")
            ?? "http://localhost:5000/auth/external";
    }

    public List<ExternalProviderDTO> GetProviders()
    {
        return new List<ExternalProviderDTO>
        {
            new() { Provider = "Google", DisplayName = "Google", IsEnabled = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GOOGLE_OAUTH_CLIENT_ID")) },
            new() { Provider = "Microsoft", DisplayName = "Microsoft", IsEnabled = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MICROSOFT_OAUTH_CLIENT_ID")) }
        };
    }

    public async Task<string> StartAsync(ExternalLoginProvider provider, string redirectPath)
    {
        if (!RedirectUrlValidator.IsValidRelativePath(redirectPath))
            redirectPath = "/index.html";

        var stateBytes = RandomNumberGenerator.GetBytes(32);
        var state = Convert.ToHexStringLower(stateBytes);
        var codeVerifierBytes = RandomNumberGenerator.GetBytes(32);
        var codeVerifier = Base64UrlEncode(codeVerifierBytes);
        var codeChallenge = Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier)));
        var nonceBytes = RandomNumberGenerator.GetBytes(16);
        var nonce = Convert.ToHexStringLower(nonceBytes);

        var authState = new ExternalAuthState
        {
            Provider = provider,
            StateHash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(state))),
            CodeVerifier = codeVerifier,
            Nonce = nonce,
            RedirectPath = redirectPath,
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(10)
        };

        _context.ExternalAuthStates.Add(authState);
        await _context.SaveChangesAsync();

        var providerService = GetProvider(provider);
        var redirectUri = $"{_baseRedirectUri}/{provider.ToString().ToLower()}/callback";
        return providerService.GetAuthorizationUrl(state, codeChallenge, redirectUri);
    }

    public async Task<(User User, string RedirectPath)> CallbackAsync(ExternalLoginProvider provider, string state, string code)
    {
        var stateHash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(state)));
        var authState = await _context.ExternalAuthStates
            .FirstOrDefaultAsync(s => s.StateHash == stateHash && s.Provider == provider && !s.IsConsumed && !s.IsExpired);
        if (authState is null)
            throw new UnauthorizedAccessException("State non valido o scaduto.");

        authState.ConsumedAtUtc = DateTime.UtcNow;

        var providerService = GetProvider(provider);
        var redirectUri = $"{_baseRedirectUri}/{provider.ToString().ToLower()}/callback";
        var externalUser = await providerService.ExchangeCodeAsync(code, authState.CodeVerifier, redirectUri);

        if (string.IsNullOrEmpty(externalUser.Email))
            throw new InvalidOperationException("Il provider non ha restituito un'email valida.");

        if (provider == ExternalLoginProvider.Google && !externalUser.EmailVerified)
            throw new InvalidOperationException("Email Google non verificata.");

        var normalizedEmail = externalUser.Email.Trim().ToUpperInvariant();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail);

        if (user is not null)
        {
            if (user.IsDisabled)
                throw new UnauthorizedAccessException("Account disabilitato.");
            if (user.Ruolo != UserRole.User)
                throw new UnauthorizedAccessException("Impossibile accedere con social login a un account con ruoli elevati. Usa email e password.");
        }

        if (user is null)
        {
            user = new User
            {
                Email = externalUser.Email.Trim(),
                NormalizedEmail = normalizedEmail,
                PasswordHash = null,
                LocalCredentialsEnabled = false,
                Nome = externalUser.Name ?? externalUser.Email.Split('@')[0],
                Cognome = "-",
                Ruolo = UserRole.User,
                DataRegistrazione = DateTime.UtcNow,
                CreditoResiduo = 0,
                AuthVersion = 1
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }

        var existingLogin = await _context.UserExternalLogins
            .FirstOrDefaultAsync(l => l.Provider == provider && l.ProviderUserId == externalUser.ProviderUserId);
        if (existingLogin is not null)
        {
            existingLogin.LastLoginAtUtc = DateTime.UtcNow;
        }
        else
        {
            _context.UserExternalLogins.Add(new UserExternalLogin
            {
                UserId = user.Id,
                Provider = provider,
                ProviderUserId = externalUser.ProviderUserId,
                ProviderTenantId = externalUser.TenantId,
                EmailAtLogin = externalUser.Email,
                LinkedAtUtc = DateTime.UtcNow,
                LastLoginAtUtc = DateTime.UtcNow
            });
        }

        var exchangeRaw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var exchangeCode = new ExternalAuthExchangeCode
        {
            UserId = user.Id,
            CodeHash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(exchangeRaw))),
            RedirectPath = authState.RedirectPath,
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(2),
            Provider = provider
        };
        _context.ExternalAuthExchangeCodes.Add(exchangeCode);

        await _audit.LogAsync(user.Id, null, "ExternalLoginSucceeded", provider: provider.ToString());

        await _context.SaveChangesAsync();

        return (user, $"/social-login-complete.html?code={Uri.EscapeDataString(exchangeRaw)}");
    }

    public async Task<AuthResponseDTO> ExchangeAsync(string code, string? deviceId)
    {
        var codeHash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(code)));
        var exchangeCode = await _context.ExternalAuthExchangeCodes
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.CodeHash == codeHash && !e.IsConsumed && !e.IsExpired);
        if (exchangeCode is null)
            throw new UnauthorizedAccessException("Exchange code non valido o scaduto.");

        exchangeCode.ConsumedAtUtc = DateTime.UtcNow;
        exchangeCode.User!.LastLoginAtUtc = DateTime.UtcNow;
        exchangeCode.User.LastLoginProvider = exchangeCode.Provider.ToString().ToLower();

        await _context.SaveChangesAsync();

        return await _auth.GenerateTokensAsync(exchangeCode.User, deviceId);
    }

    public async Task<List<UserExternalLoginDTO>> GetUserExternalLoginsAsync(int userId)
    {
        return await _context.UserExternalLogins
            .Where(l => l.UserId == userId && l.RevokedAtUtc == null)
            .Select(l => new UserExternalLoginDTO
            {
                Provider = l.Provider.ToString(),
                EmailAtLogin = l.EmailAtLogin,
                LinkedAtUtc = l.LinkedAtUtc,
                LastLoginAtUtc = l.LastLoginAtUtc
            })
            .ToListAsync();
    }

    private IExternalAuthProvider GetProvider(ExternalLoginProvider provider) => provider switch
    {
        ExternalLoginProvider.Google => _google,
        ExternalLoginProvider.Microsoft => _microsoft,
        _ => throw new ArgumentException("Provider non supportato.")
    };

    private static string Base64UrlEncode(byte[] data)
    {
        return Convert.ToBase64String(data).Replace('/', '_').Replace('+', '-').TrimEnd('=');
    }
}
