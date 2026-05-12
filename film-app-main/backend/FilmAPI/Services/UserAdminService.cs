using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Services;

public class UserAdminService : IUserAdminService
{
    private readonly FilmDbContext _context;
    private readonly IAccountTokenService _tokenService;
    private readonly IAccountEmailService _accountEmail;
    private readonly IUserSecurityAuditService _audit;
    private readonly string _frontendBaseUrl;

    public UserAdminService(
        FilmDbContext context,
        IAccountTokenService tokenService,
        IAccountEmailService accountEmail,
        IUserSecurityAuditService audit)
    {
        _context = context;
        _tokenService = tokenService;
        _accountEmail = accountEmail;
        _audit = audit;
        _frontendBaseUrl = Environment.GetEnvironmentVariable("FRONTEND_BASE_URL") ?? "http://localhost:5001";
    }

    public async Task<List<UserAdminDTO>> GetAllUsersAsync()
    {
        return await _context.Users
            .Select(u => new UserAdminDTO
            {
                Id = u.Id,
                Email = u.Email,
                Nome = u.Nome,
                Cognome = u.Cognome,
                Telefono = u.Telefono,
                Ruolo = u.Ruolo.ToString(),
                DataRegistrazione = u.DataRegistrazione
            })
            .ToListAsync();
    }

    public async Task<AdminUserPagedResultDTO> GetUsersPagedAsync(string? search, string? role, int page, int pageSize)
    {
        var query = _context.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchUpper = search.ToUpperInvariant();
            query = query.Where(u => u.NormalizedEmail.Contains(searchUpper) || u.Nome.Contains(search) || u.Cognome.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(role) && Enum.TryParse<UserRole>(role, out var roleFilter))
        {
            query = query.Where(u => u.Ruolo == roleFilter);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(u => u.DataRegistrazione)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new AdminUserListItemDTO
            {
                Id = u.Id,
                Email = u.Email,
                Nome = u.Nome,
                Cognome = u.Cognome,
                Ruolo = u.Ruolo.ToString(),
                HasLocalPassword = u.LocalCredentialsEnabled && !string.IsNullOrEmpty(u.PasswordHash),
                IsDisabled = u.IsDisabled,
                ConnectedProviders = u.ExternalLogins.Where(l => l.RevokedAtUtc == null).Select(l => l.Provider.ToString()).ToList(),
                DataRegistrazione = u.DataRegistrazione,
                LastLoginAtUtc = u.LastLoginAtUtc
            })
            .ToListAsync();

        return new AdminUserPagedResultDTO
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<UserAdminDTO?> UpdateUserRoleAsync(int userId, UpdateRuoloDTO dto, int requestingUserId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user is null) return null;

        if (!Enum.TryParse<UserRole>(dto.NuovoRuolo, out var newRole))
            throw new InvalidOperationException("Ruolo non valido");

        if (user.IsDisabled)
            throw new InvalidOperationException("Impossibile modificare il ruolo di un account disabilitato.");

        if (newRole != UserRole.User && !user.LocalCredentialsEnabled)
            throw new InvalidOperationException("Impossibile promuovere un account senza password locale. Richiedi prima l'impostazione della password.");

        if (user.Ruolo == UserRole.Admin && newRole != UserRole.Admin)
        {
            var adminCount = await _context.Users.CountAsync(u => u.Ruolo == UserRole.Admin);
            if (adminCount <= 1)
                throw new InvalidOperationException("Non e possibile degradare l'ultimo admin");
        }

        var oldRole = user.Ruolo;
        user.Ruolo = newRole;
        user.AuthVersion++;

        var tokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
            .ToListAsync();
        foreach (var token in tokens)
            token.RevokedAt = DateTime.UtcNow;

        await _audit.LogAsync(userId, requestingUserId, "RoleChanged",
            metadataJson: System.Text.Json.JsonSerializer.Serialize(new { OldRole = oldRole.ToString(), NewRole = newRole.ToString() }));

        await _context.SaveChangesAsync();

        return new UserAdminDTO
        {
            Id = user.Id,
            Email = user.Email,
            Nome = user.Nome,
            Cognome = user.Cognome,
            Telefono = user.Telefono,
            Ruolo = user.Ruolo.ToString(),
            DataRegistrazione = user.DataRegistrazione
        };
    }

    public async Task<AdminUserInviteResultDTO> CreateAdminInviteAsync(CreateAdminUserInviteDTO dto, int requestingUserId)
    {
        if (!Enum.TryParse<UserRole>(dto.Ruolo, out var targetRole) || targetRole == UserRole.User)
            throw new InvalidOperationException("Ruolo non valido per invito admin.");

        var normalizedEmail = dto.Email.Trim().ToUpperInvariant();
        var existing = await _context.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail);
        if (existing is not null)
            throw new InvalidOperationException("Un utente con questa email esiste gia. Usa la promozione invece dell'invito.");

        var user = new User
        {
            Email = dto.Email.Trim(),
            NormalizedEmail = normalizedEmail,
            PasswordHash = null,
            LocalCredentialsEnabled = false,
            Nome = dto.Nome,
            Cognome = dto.Cognome,
            Ruolo = targetRole,
            DataRegistrazione = DateTime.UtcNow,
            CreditoResiduo = 0,
            AuthVersion = 0,
            MustChangePassword = true,
            IsDisabled = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var ttl = TimeSpan.FromHours(int.Parse(Environment.GetEnvironmentVariable("ADMIN_INVITE_TOKEN_TTL_HOURS") ?? "24"));
        var token = await _tokenService.CreateTokenAsync(user.Id, AccountActionTokenPurpose.AdminInvite, ttl, requestingUserId);

        var inviteUrl = $"{_frontendBaseUrl}/reimposta-password.html?token={Uri.EscapeDataString(token)}";
        await _accountEmail.SendAdminInviteAsync(user, inviteUrl, targetRole.ToString());

        await _audit.LogAsync(user.Id, requestingUserId, "AdminInviteCreated",
            metadataJson: System.Text.Json.JsonSerializer.Serialize(new { Role = targetRole.ToString() }));

        return new AdminUserInviteResultDTO
        {
            UserId = user.Id,
            Message = $"Invito inviato a {dto.Email}"
        };
    }

    public async Task RequestPasswordSetupAsync(int userId, int requestingUserId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user is null || user.IsDisabled)
            throw new InvalidOperationException("Utente non trovato o disabilitato.");

        if (user.LocalCredentialsEnabled && !string.IsNullOrEmpty(user.PasswordHash))
            throw new InvalidOperationException("L'utente ha gia una password locale.");

        var ttl = TimeSpan.FromMinutes(int.Parse(Environment.GetEnvironmentVariable("SET_PASSWORD_TOKEN_TTL_MINUTES") ?? "60"));
        var token = await _tokenService.CreateTokenAsync(user.Id, AccountActionTokenPurpose.SetPassword, ttl, requestingUserId);

        var setupUrl = $"{_frontendBaseUrl}/reimposta-password.html?token={Uri.EscapeDataString(token)}";
        await _accountEmail.SendSetPasswordAsync(user, setupUrl);

        await _audit.LogAsync(userId, requestingUserId, "SetPasswordRequested");
    }

    public async Task<AdminUserSecurityDTO> GetUserSecurityAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user is null)
            throw new InvalidOperationException("Utente non trovato.");

        var externalLogins = await _context.UserExternalLogins
            .Where(l => l.UserId == userId && l.RevokedAtUtc == null)
            .Select(l => new UserExternalLoginDTO
            {
                Provider = l.Provider.ToString(),
                EmailAtLogin = l.EmailAtLogin,
                LinkedAtUtc = l.LinkedAtUtc,
                LastLoginAtUtc = l.LastLoginAtUtc
            })
            .ToListAsync();

        return new AdminUserSecurityDTO
        {
            Id = user.Id,
            Email = user.Email,
            Ruolo = user.Ruolo.ToString(),
            HasLocalPassword = user.LocalCredentialsEnabled && !string.IsNullOrEmpty(user.PasswordHash),
            IsDisabled = user.IsDisabled,
            PasswordChangedAtUtc = user.PasswordChangedAtUtc,
            LastLoginAtUtc = user.LastLoginAtUtc,
            LastLoginProvider = user.LastLoginProvider,
            AuthVersion = user.AuthVersion,
            ExternalLogins = externalLogins
        };
    }
}
