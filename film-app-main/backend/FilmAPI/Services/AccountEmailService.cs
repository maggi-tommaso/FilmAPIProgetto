using System.Net;
using System.Text;
using FilmAPI.Model;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace FilmAPI.Services;

public class AccountEmailService : IAccountEmailService
{
    private readonly ILogger<AccountEmailService> _logger;
    private readonly string? _smtpHost;
    private readonly int _smtpPort;
    private readonly string? _smtpUser;
    private readonly string? _smtpPassword;
    private readonly string? _fromEmail;
    private readonly string? _fromName;

    public AccountEmailService(ILogger<AccountEmailService> logger)
    {
        _logger = logger;
        _smtpHost = ReadSetting("SMTP_HOST");
        _smtpPort = int.TryParse(Environment.GetEnvironmentVariable("SMTP_PORT"), out var port) ? port : 587;
        _smtpUser = ReadSetting("SMTP_USER");
        _smtpPassword = ReadSetting("SMTP_PASSWORD");
        _fromEmail = ReadSetting("SMTP_FROM_EMAIL");
        _fromName = ReadSetting("SMTP_FROM_NAME") ?? "RedCurtain";
    }

    public async Task SendPasswordResetAsync(User user, string resetUrl, CancellationToken ct = default)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"<h2>Ciao {user.Nome},</h2>");
        sb.AppendLine("<p>Hai richiesto il recupero della password per il tuo account RedCurtain.</p>");
        sb.AppendLine($"<p>Clicca il link qui sotto per reimpostare la password (scade tra 30 minuti):</p>");
        sb.AppendLine($"<p><a href=\"{resetUrl}\" style=\"display:inline-block;padding:12px 24px;background:#d4a017;color:#0d0d0d;border-radius:8px;text-decoration:none;font-weight:bold;\">Reimposta Password</a></p>");
        sb.AppendLine("<p>Se non hai richiesto questa operazione, ignora questa email.</p>");
        await SendEmailAsync(user.Email, "RedCurtain - Recupero Password", sb.ToString(), ct);
    }

    public async Task SendSetPasswordAsync(User user, string setupUrl, CancellationToken ct = default)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"<h2>Ciao {user.Nome},</h2>");
        sb.AppendLine("<p>Ti e stato inviato un link per impostare la password del tuo account RedCurtain.</p>");
        sb.AppendLine($"<p>Clicca il link qui sotto (scade tra 60 minuti):</p>");
        sb.AppendLine($"<p><a href=\"{setupUrl}\" style=\"display:inline-block;padding:12px 24px;background:#d4a017;color:#0d0d0d;border-radius:8px;text-decoration:none;font-weight:bold;\">Imposta Password</a></p>");
        await SendEmailAsync(user.Email, "RedCurtain - Imposta Password", sb.ToString(), ct);
    }

    public async Task SendAdminInviteAsync(User user, string inviteUrl, string ruolo, CancellationToken ct = default)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"<h2>Ciao {user.Nome},</h2>");
        sb.AppendLine($"<p>Sei stato invitato su RedCurtain come <strong>{ruolo}</strong>.</p>");
        sb.AppendLine($"<p>Clicca il link qui sotto per impostare la password e attivare il tuo account (scade tra 24 ore):</p>");
        sb.AppendLine($"<p><a href=\"{inviteUrl}\" style=\"display:inline-block;padding:12px 24px;background:#d4a017;color:#0d0d0d;border-radius:8px;text-decoration:none;font-weight:bold;\">Attiva Account</a></p>");
        await SendEmailAsync(user.Email, "RedCurtain - Invito Staff", sb.ToString(), ct);
    }

    public async Task SendPasswordChangedAsync(User user, CancellationToken ct = default)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"<h2>Ciao {user.Nome},</h2>");
        sb.AppendLine("<p>La password del tuo account RedCurtain e stata modificata.</p>");
        sb.AppendLine("<p>Se non sei stato tu, contatta immediatamente un amministratore.</p>");
        await SendEmailAsync(user.Email, "RedCurtain - Password Modificata", sb.ToString(), ct);
    }

    public async Task SendEmailVerificationAsync(User user, string verifyUrl, CancellationToken ct = default)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"<h2>Ciao {user.Nome},</h2>");
        sb.AppendLine("<p>Benvenuto su RedCurtain! Per completare la registrazione e poter acquistare biglietti, verifica il tuo indirizzo email.</p>");
        sb.AppendLine($"<p>Clicca il link qui sotto (scade tra 24 ore):</p>");
        sb.AppendLine($"<p><a href=\"{verifyUrl}\" style=\"display:inline-block;padding:12px 24px;background:#d4a017;color:#0d0d0d;border-radius:8px;text-decoration:none;font-weight:bold;\">Verifica Email</a></p>");
        sb.AppendLine("<p>Se non hai creato tu questo account, ignora questa email.</p>");
        await SendEmailAsync(user.Email, "RedCurtain - Verifica il tuo indirizzo email", sb.ToString(), ct);
    }

    private async Task SendEmailAsync(string to, string subject, string htmlBody, CancellationToken ct)
    {
        if (!HasCompleteConfiguration())
        {
            _logger.LogWarning("SMTP not configured, skipping email to {To}", to);
            return;
        }

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(15));

        try
        {
            using var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_fromName, _fromEmail));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;
            message.Body = new TextPart("html")
            {
                Text = htmlBody
            };

            using var client = new SmtpClient();
            client.Timeout = 10000;
            await client.ConnectAsync(_smtpHost!, _smtpPort, SecureSocketOptions.StartTls, cts.Token);
            if (!string.IsNullOrEmpty(_smtpUser))
            {
                await client.AuthenticateAsync(
                    new NetworkCredential(_smtpUser, _smtpPassword),
                    cts.Token);
            }
            await client.SendAsync(message, cts.Token);
            await client.DisconnectAsync(true, cts.Token);

            _logger.LogInformation("Account email sent to {To}: {Subject}", to, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send account email to {To}: {Subject}", to, subject);
        }
    }

    private bool HasCompleteConfiguration()
    {
        return !string.IsNullOrWhiteSpace(_smtpHost) &&
               !string.IsNullOrWhiteSpace(_fromEmail);
    }

    private static string? ReadSetting(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        return IsPlaceholder(trimmed) ? null : trimmed;
    }

    private static bool IsPlaceholder(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return true;

        var lower = value.ToLowerInvariant();
        if (lower.StartsWith('<') && lower.EndsWith('>'))
            return true;
        if (lower.Contains("change-me") || lower.Contains("changeme"))
            return true;
        if (lower.Contains("your-") && (lower.Contains("email") || lower.Contains("password") || lower.Contains("host") || lower.Contains("user")))
            return true;
        if (lower.Contains("placeholder") || lower.Contains("example") || lower.Contains("test"))
            return true;

        return false;
    }
}
