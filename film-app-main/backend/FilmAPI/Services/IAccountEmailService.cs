using FilmAPI.Model;

namespace FilmAPI.Services;

public interface IAccountEmailService
{
    Task SendPasswordResetAsync(User user, string resetUrl, CancellationToken ct = default);
    Task SendSetPasswordAsync(User user, string setupUrl, CancellationToken ct = default);
    Task SendAdminInviteAsync(User user, string inviteUrl, string ruolo, CancellationToken ct = default);
    Task SendPasswordChangedAsync(User user, CancellationToken ct = default);
    Task SendEmailVerificationAsync(User user, string verifyUrl, CancellationToken ct = default);
}
