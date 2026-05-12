using System.Net;
using System.Text;
using FilmAPI.DTO;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;
using System.Globalization;

namespace FilmAPI.Services;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly string? _smtpHost;
    private readonly int _smtpPort;
    private readonly string? _smtpUser;
    private readonly string? _smtpPassword;
    private readonly string? _fromEmail;
    private readonly string? _fromName;

    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger;
        _smtpHost = ReadSetting("SMTP_HOST");
        _smtpPort = int.TryParse(Environment.GetEnvironmentVariable("SMTP_PORT"), out var port) ? port : 587;
        _smtpUser = ReadSetting("SMTP_USER");
        _smtpPassword = ReadSetting("SMTP_PASSWORD");
        _fromEmail = ReadSetting("SMTP_FROM_EMAIL");
        _fromName = ReadSetting("SMTP_FROM_NAME") ?? "CineBase";
    }

    public async Task<EmailSendResult> SendOrderTicketsAsync(OrdineTicketDocumentDTO orderDocument, byte[] pdfBytes, string fileName, CancellationToken cancellationToken = default)
    {
        if (!HasCompleteConfiguration())
        {
            return new EmailSendResult
            {
                Success = false,
                ErrorMessage = "Configurazione SMTP incompleta. Verificare le variabili SMTP_* del backend."
            };
        }

        if (string.IsNullOrWhiteSpace(orderDocument.RecipientEmail))
        {
            return new EmailSendResult
            {
                Success = false,
                ErrorMessage = "Email destinatario non disponibile per l'ordine richiesto."
            };
        }

        try
        {
            var smtpHost = _smtpHost!;
            var smtpUser = _smtpUser!;
            var smtpPassword = _smtpPassword!;
            var fromEmail = _fromEmail!;

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_fromName, fromEmail));
            message.To.Add(MailboxAddress.Parse(orderDocument.RecipientEmail));
            message.Subject = $"CineBase - Biglietti ordine {orderDocument.CodiceOrdine}";

            var bodyBuilder = new BodyBuilder
            {
                TextBody = BuildTextBody(orderDocument),
                HtmlBody = BuildHtmlBody(orderDocument)
            };

            bodyBuilder.Attachments.Add(fileName, pdfBytes, ContentType.Parse("application/pdf"));
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(smtpHost, _smtpPort, SecureSocketOptions.StartTls, cancellationToken);
            await client.AuthenticateAsync(smtpUser, smtpPassword, cancellationToken);
            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            return new EmailSendResult
            {
                Success = true,
                SentAtUtc = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Invio email ticket fallito per ordine {OrderId}", orderDocument.OrdineId);
            return new EmailSendResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private bool HasCompleteConfiguration()
    {
        return !string.IsNullOrWhiteSpace(_smtpHost)
            && !string.IsNullOrWhiteSpace(_smtpUser)
            && !string.IsNullOrWhiteSpace(_smtpPassword)
            && !string.IsNullOrWhiteSpace(_fromEmail);
    }

    private static string BuildTextBody(OrdineTicketDocumentDTO orderDocument)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Conferma acquisto ordine {orderDocument.CodiceOrdine}");
        builder.AppendLine();
        builder.AppendLine($"Film: {orderDocument.FilmTitolo}");
        builder.AppendLine($"Cinema: {orderDocument.CinemaNome}");
        builder.AppendLine($"Sala: {orderDocument.SalaNome}");
        builder.AppendLine($"Data e ora: {FormatShowDateTime(orderDocument.StartAtUtc)}");
        builder.AppendLine($"Numero biglietti: {orderDocument.NumeroBiglietti}");
        builder.AppendLine($"Totale: {FormatAmount(orderDocument.TotaleLordo)} EUR");
        builder.AppendLine();
        builder.AppendLine("Codici ticket:");

        foreach (var ticket in orderDocument.Tickets)
            builder.AppendLine($"- {ticket.CodiceBiglietto} | {ticket.Settore} fila {ticket.Fila} posto {ticket.Numero}");

        builder.AppendLine();
        builder.AppendLine("In allegato trovi il PDF multipagina dei biglietti.");
        builder.AppendLine("Il profilo utente resta il punto di recupero ufficiale dell'ordine.");
        return builder.ToString();
    }

    private static string BuildHtmlBody(OrdineTicketDocumentDTO orderDocument)
    {
        var title = WebUtility.HtmlEncode(orderDocument.FilmTitolo);
        var cinema = WebUtility.HtmlEncode(orderDocument.CinemaNome);
        var sala = WebUtility.HtmlEncode(orderDocument.SalaNome);
        var orderCode = WebUtility.HtmlEncode(orderDocument.CodiceOrdine);

        var ticketsHtml = string.Join(string.Empty, orderDocument.Tickets.Select(ticket =>
            $"<li><strong>{WebUtility.HtmlEncode(ticket.CodiceBiglietto)}</strong> - {WebUtility.HtmlEncode(ticket.Settore)} fila {ticket.Fila} posto {ticket.Numero}</li>"));

        return $"""
<html>
  <body style="font-family: Arial, sans-serif; color: #111827; line-height: 1.5;">
    <h1 style="margin-bottom: 8px;">Conferma acquisto CineBase</h1>
    <p>Ordine <strong>{orderCode}</strong> completato con successo.</p>
    <p><strong>Film:</strong> {title}<br />
       <strong>Cinema:</strong> {cinema}<br />
       <strong>Sala:</strong> {sala}<br />
       <strong>Data e ora:</strong> {FormatShowDateTime(orderDocument.StartAtUtc)}<br />
       <strong>Totale:</strong> {FormatAmount(orderDocument.TotaleLordo)} EUR</p>
    <p><strong>Biglietti emessi:</strong></p>
    <ul>{ticketsHtml}</ul>
    <p>In allegato trovi il PDF multipagina dei biglietti.</p>
    <p>Il profilo utente resta il punto di recupero ufficiale dell'ordine.</p>
  </body>
</html>
""";
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
        return value.StartsWith('<') && value.EndsWith('>');
    }

    private static string FormatShowDateTime(DateTime startAtUtc)
    {
        var timeZone = ResolveItalyTimeZone();
        var localTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(startAtUtc, DateTimeKind.Utc), timeZone);
        return localTime.ToString("dd/MM/yyyy HH:mm");
    }

    private static string FormatAmount(decimal amount)
    {
        return amount.ToString("0.00", CultureInfo.GetCultureInfo("it-IT"));
    }

    private static TimeZoneInfo ResolveItalyTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Europe/Rome");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
        }
    }
}
