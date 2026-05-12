using FilmAPI.DTO;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace FilmAPI.Services;

public static class EmailService
{
    public static async Task SendConfirmationEmailAsync(SmtpConfig config, string toEmail, Guid token, string apiBaseUrl, CancellationToken ct = default)
    {
        if (!config.IsConfigured)
        {
            return;
        }

        var confirmationLink = $"{apiBaseUrl}/auth/conferma-email?token={token:N}";

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(config.From));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = "Sala Luce - Conferma la tua email";

        message.Body = new TextPart("html")
        {
            Text = $"""
            <div style="font-family:Arial,sans-serif;max-width:600px;margin:0 auto;padding:20px">
              <h1 style="color:#1e293b">Sala Luce</h1>
              <h2 style="color:#334155">Conferma il tuo indirizzo email</h2>
              <p style="color:#475569">Grazie per esserti registrato! Clicca sul pulsante qui sotto per confermare il tuo indirizzo email e iniziare ad acquistare biglietti.</p>
              <p style="margin-top:24px">
                <a href="{confirmationLink}" style="display:inline-block;background-color:#1152d4;color:#fff;padding:12px 24px;border-radius:8px;text-decoration:none;font-weight:bold">Conferma email</a>
              </p>
              <p style="margin-top:24px;color:#94a3b8;font-size:12px">Se non hai richiesto questa registrazione, ignora questa email. Il link scade dopo 24 ore.</p>
              <p style="color:#94a3b8;font-size:12px">Se il pulsante non funziona, copia e incolla questo link nel browser:<br/>{confirmationLink}</p>
            </div>
            """
        };

        using var client = new SmtpClient();
        await client.ConnectAsync(config.Host, config.Port, config.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto, ct);
        await client.AuthenticateAsync(config.User, config.Password, ct);
        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);
    }
}
