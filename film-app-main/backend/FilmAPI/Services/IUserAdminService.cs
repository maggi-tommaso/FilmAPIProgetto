using FilmAPI.DTO;
using FilmAPI.Model;

namespace FilmAPI.Services;

public interface IUserAdminService
{
    Task<List<UserAdminDTO>> GetAllUsersAsync();
    Task<AdminUserPagedResultDTO> GetUsersPagedAsync(string? search, string? role, int page, int pageSize);
    Task<UserAdminDTO?> UpdateUserRoleAsync(int userId, UpdateRuoloDTO dto, int requestingUserId);
    Task<AdminUserInviteResultDTO> CreateAdminInviteAsync(CreateAdminUserInviteDTO dto, int requestingUserId);
    Task RequestPasswordSetupAsync(int userId, int requestingUserId);
    Task<AdminUserSecurityDTO> GetUserSecurityAsync(int userId);
}

public class AdminUserPagedResultDTO
{
    public List<AdminUserListItemDTO> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class AdminUserListItemDTO
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Cognome { get; set; } = string.Empty;
    public string Ruolo { get; set; } = string.Empty;
    public bool HasLocalPassword { get; set; }
    public bool IsDisabled { get; set; }
    public List<string> ConnectedProviders { get; set; } = new();
    public DateTime DataRegistrazione { get; set; }
    public DateTime? LastLoginAtUtc { get; set; }
}

public class CreateAdminUserInviteDTO
{
    public string Email { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Cognome { get; set; } = string.Empty;
    public string Ruolo { get; set; } = string.Empty;
}

public class AdminUserInviteResultDTO
{
    public int UserId { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class AdminUserSecurityDTO
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Ruolo { get; set; } = string.Empty;
    public bool HasLocalPassword { get; set; }
    public bool IsDisabled { get; set; }
    public DateTime? PasswordChangedAtUtc { get; set; }
    public DateTime? LastLoginAtUtc { get; set; }
    public string? LastLoginProvider { get; set; }
    public int AuthVersion { get; set; }
    public List<UserExternalLoginDTO> ExternalLogins { get; set; } = new();
}
