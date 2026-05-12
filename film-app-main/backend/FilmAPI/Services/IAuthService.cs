using FilmAPI.DTO;
using FilmAPI.Model;

namespace FilmAPI.Services;

public interface IAuthService
{
    Task<AuthResponseDTO> RegisterAsync(RegisterRequestDTO dto);
    Task<AuthResponseDTO> LoginAsync(LoginRequestDTO dto);
    Task<AuthResponseDTO> RefreshAsync(string refreshToken, string? deviceId);
    Task<bool> LogoutAsync(string refreshToken, string? deviceId);
    Task<UserInfoDTO?> GetUserByIdAsync(int id);
    Task ChangePasswordAsync(int userId, ChangePasswordRequestDTO dto);
    Task<string> RequestPasswordResetAsync(ForgotPasswordRequestDTO dto);
    Task<AuthResponseDTO> ResetPasswordAsync(ResetPasswordRequestDTO dto, string? deviceId);
    Task<AccountSecurityDTO> GetAccountSecurityAsync(int userId);
    Task RequestSetPasswordAsync(int userId);
    Task<AuthResponseDTO> GenerateTokensAsync(User user, string? deviceId);
}
