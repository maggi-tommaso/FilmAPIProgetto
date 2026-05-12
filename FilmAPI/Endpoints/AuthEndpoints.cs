using FilmAPI.Data;
using FilmAPI.DTO;
using FilmAPI.Model;
using FilmAPI.Services;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace FilmAPI.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app, string? googleClientId, SmtpConfig smtpConfig, string apiBaseUrl = "http://localhost:5072")
    {
        var group = app.MapGroup("/auth").WithTags("Auth");

        group.MapGet("/google/config", () =>
        {
            var enabled = !string.IsNullOrWhiteSpace(googleClientId);
            return TypedResults.Ok(new GoogleClientConfigDTO(enabled, enabled ? googleClientId : null));
        });

        group.MapPost("/register", (RegisterRequestDTO input, FilmDbContext db, CancellationToken ct) =>
            Register(input, db, smtpConfig, apiBaseUrl, ct));
        group.MapPost("/login", Login);
        group.MapPost("/login/google", (LoginGoogleRequestDTO input, FilmDbContext db, CancellationToken ct) =>
            LoginGoogle(input, db, googleClientId, ct));
        group.MapPost("/logout", Logout);
        group.MapGet("/me", GetMe);
        group.MapGet("/conferma-email", (Guid token, FilmDbContext db, CancellationToken ct) =>
            ConfermaEmail(token, db, ct));

        return app;
    }

    private static async Task<Results<Created<LoginResponseDTO>, BadRequest<string>, Conflict<string>>> Register(
        RegisterRequestDTO input,
        FilmDbContext db,
        SmtpConfig smtpConfig,
        string apiBaseUrl,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input.Username) ||
            string.IsNullOrWhiteSpace(input.Nome) ||
            string.IsNullOrWhiteSpace(input.Cognome) ||
            string.IsNullOrWhiteSpace(input.Email) ||
            string.IsNullOrWhiteSpace(input.Password))
        {
            return TypedResults.BadRequest("Username, nome, cognome, email e password sono obbligatori.");
        }

        if (input.Password.Length < 3)
        {
            return TypedResults.BadRequest("La password deve avere almeno 3 caratteri.");
        }

        var username = NormalizeUsername(input.Username);
        var email = input.Email.Trim().ToLowerInvariant();

        if (await db.Utenti.AnyAsync(u => u.Username == username, ct))
        {
            return TypedResults.Conflict("Username gia in uso.");
        }

        if (await db.Utenti.AnyAsync(u => u.Email == email, ct))
        {
            return TypedResults.Conflict("Email gia registrata.");
        }

        var confirmationToken = Guid.NewGuid();
        var user = new Utente
        {
            Username = username,
            Nome = input.Nome.Trim(),
            Cognome = input.Cognome.Trim(),
            Email = email,
            EmailConfermata = false,
            EmailTokenConferma = confirmationToken,
            EmailTokenScadeIlUtc = DateTime.UtcNow.AddHours(24),
            Provider = "local",
            PasswordHash = HashPassword(input.Password),
            CreatoIlUtc = DateTime.UtcNow,
            UltimoAccessoUtc = DateTime.UtcNow
        };

        db.Utenti.Add(user);
        await db.SaveChangesAsync(ct);

        var session = new SessioneAccesso
        {
            UtenteId = user.Id,
            CreatoIlUtc = DateTime.UtcNow,
            ScadeIlUtc = DateTime.UtcNow.AddHours(12)
        };

        db.SessioniAccesso.Add(session);
        await db.SaveChangesAsync(ct);

        _ = Task.Run(async () =>
        {
            try
            {
                await EmailService.SendConfirmationEmailAsync(smtpConfig, email, confirmationToken, apiBaseUrl, CancellationToken.None);
            }
            catch
            {
                // L'invio email è best-effort; il token resta salvato nel DB.
            }
        });

        var response = ToLoginResponse(user, session);
        return TypedResults.Created("/auth/me", response);
    }

    private static async Task<Results<Ok<LoginResponseDTO>, BadRequest<string>, UnauthorizedHttpResult>> Login(
        LoginRequestDTO input,
        FilmDbContext db,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input.Identifier) || string.IsNullOrWhiteSpace(input.Password))
        {
            return TypedResults.BadRequest("Username o email e password sono obbligatori.");
        }

        var identifier = input.Identifier.Trim();
        var normalizedEmail = identifier.ToLowerInvariant();
        var normalizedUsername = NormalizeUsername(identifier);

        var user = await db.Utenti.FirstOrDefaultAsync(
            u => u.Email == normalizedEmail || u.Username == normalizedUsername,
            ct);

        if (user is null || string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            return TypedResults.Unauthorized();
        }

        var inputHash = HashPassword(input.Password);
        if (!string.Equals(user.PasswordHash, inputHash, StringComparison.Ordinal))
        {
            return TypedResults.Unauthorized();
        }

        var session = new SessioneAccesso
        {
            UtenteId = user.Id,
            CreatoIlUtc = DateTime.UtcNow,
            ScadeIlUtc = DateTime.UtcNow.AddHours(12)
        };

        user.UltimoAccessoUtc = DateTime.UtcNow;

        db.SessioniAccesso.Add(session);
        await db.SaveChangesAsync(ct);

        return TypedResults.Ok(ToLoginResponse(user, session));
    }

    private static async Task<Results<Ok<LoginResponseDTO>, BadRequest<string>, UnauthorizedHttpResult>> LoginGoogle(
        LoginGoogleRequestDTO input,
        FilmDbContext db,
        string? googleClientId,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(googleClientId))
        {
            return TypedResults.BadRequest("OAuth Google non configurato lato server.");
        }

        if (string.IsNullOrWhiteSpace(input.IdToken))
        {
            return TypedResults.BadRequest("IdToken Google mancante.");
        }

        GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(
                input.IdToken,
                new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { googleClientId }
                });
        }
        catch (InvalidJwtException)
        {
            return TypedResults.Unauthorized();
        }

        if (!payload.EmailVerified)
        {
            return TypedResults.Unauthorized();
        }

        var email = payload.Email.Trim().ToLowerInvariant();
        var googleSubject = payload.Subject;

        var user = await db.Utenti.FirstOrDefaultAsync(u => u.GoogleSubject == googleSubject, ct)
            ?? await db.Utenti.FirstOrDefaultAsync(u => u.Email == email, ct);

        if (user is null)
        {
            var username = await EnsureUniqueUsernameAsync(payload.Email.Split('@')[0], db, ct);
            user = new Utente
            {
                Username = username,
                Nome = payload.GivenName ?? payload.Name ?? "Utente",
                Cognome = payload.FamilyName ?? "Google",
                Email = email,
                EmailConfermata = true,
                GoogleSubject = googleSubject,
                Provider = "google",
                PasswordHash = null,
                CreatoIlUtc = DateTime.UtcNow,
                UltimoAccessoUtc = DateTime.UtcNow
            };

            db.Utenti.Add(user);
            await db.SaveChangesAsync(ct);
        }
        else
        {
            user.GoogleSubject ??= googleSubject;
            user.EmailConfermata = true;
            user.Provider = "google";
            user.Nome = string.IsNullOrWhiteSpace(payload.GivenName) ? user.Nome : payload.GivenName;
            user.Cognome = string.IsNullOrWhiteSpace(payload.FamilyName) ? user.Cognome : payload.FamilyName;
            user.UltimoAccessoUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }

        var session = new SessioneAccesso
        {
            UtenteId = user.Id,
            CreatoIlUtc = DateTime.UtcNow,
            ScadeIlUtc = DateTime.UtcNow.AddHours(12)
        };

        db.SessioniAccesso.Add(session);
        await db.SaveChangesAsync(ct);

        return TypedResults.Ok(ToLoginResponse(user, session));
    }

    private static async Task<NoContent> Logout(HttpRequest request, FilmDbContext db, CancellationToken ct)
    {
        if (!TryReadBearerToken(request, out var token))
        {
            return TypedResults.NoContent();
        }

        if (Guid.TryParseExact(token, "N", out var sessionId))
        {
            var session = await db.SessioniAccesso.FirstOrDefaultAsync(s => s.Id == sessionId, ct);
            if (session is not null && session.RevocatoIlUtc is null)
            {
                session.RevocatoIlUtc = DateTime.UtcNow;
                await db.SaveChangesAsync(ct);
            }
        }

        return TypedResults.NoContent();
    }

    private static async Task<Results<Ok<ProfiloUtenteDTO>, UnauthorizedHttpResult>> GetMe(
        HttpRequest request,
        FilmDbContext db,
        CancellationToken ct)
    {
        if (!TryReadBearerToken(request, out var token) || !Guid.TryParseExact(token, "N", out var sessionId))
        {
            return TypedResults.Unauthorized();
        }

        var session = await db.SessioniAccesso
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == sessionId, ct);

        if (session is null || session.RevocatoIlUtc is not null || session.ScadeIlUtc <= DateTime.UtcNow)
        {
            return TypedResults.Unauthorized();
        }

        var user = await db.Utenti
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == session.UtenteId, ct);

        if (user is null)
        {
            return TypedResults.Unauthorized();
        }

        var tickets = await db.BigliettiUtente
            .AsNoTracking()
            .Where(b => b.UtenteId == user.Id)
            .OrderByDescending(b => b.AcquistatoIlUtc)
            .Select(b => new BigliettoUtenteDTO(b.Codice, b.AcquistatoIlUtc, b.ConvalidatoIlUtc, b.IsConvalidato))
            .ToListAsync(ct);

        var validated = tickets.Where(t => t.IsConvalidato).ToList();
        var pending = tickets.Where(t => !t.IsConvalidato).ToList();

        return TypedResults.Ok(new ProfiloUtenteDTO(
            user.Username,
            user.Nome,
            user.Cognome,
            user.Email,
            MaskEmail(user.Email),
            user.Provider,
            user.EmailConfermata,
            user.CreatoIlUtc,
            user.UltimoAccessoUtc,
            validated,
            pending));
    }

    private static bool TryReadBearerToken(HttpRequest request, out string token)
    {
        token = string.Empty;
        var auth = request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(auth) || !auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        token = auth[7..].Trim();
        return !string.IsNullOrWhiteSpace(token);
    }

    private static LoginResponseDTO ToLoginResponse(Utente user, SessioneAccesso session) =>
        new(
            session.Id.ToString("N"),
            session.ScadeIlUtc,
            user.Username,
            user.Nome,
            user.Cognome,
            user.Email,
            user.Provider,
            user.EmailConfermata);

    private static async Task<Results<ContentHttpResult, NotFound<string>>> ConfermaEmail(
        Guid token,
        FilmDbContext db,
        CancellationToken ct)
    {
        var user = await db.Utenti.FirstOrDefaultAsync(u => u.EmailTokenConferma == token, ct);
        if (user is null)
        {
            return TypedResults.NotFound("Token di conferma non valido.");
        }

        if (user.EmailTokenScadeIlUtc <= DateTime.UtcNow)
        {
            return TypedResults.NotFound("Token di conferma scaduto.");
        }

        user.EmailConfermata = true;
        user.EmailTokenConferma = null;
        user.EmailTokenScadeIlUtc = null;
        await db.SaveChangesAsync(ct);

        return TypedResults.Content("""<!doctype html><html lang="it"><head><meta charset="UTF-8"><meta name="viewport" content="width=device-width,initial-scale=1.0"><title>Email Confermata - Sala Luce</title><script src="https://cdn.tailwindcss.com?plugins=forms"></script><link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700;800;900&display=swap" rel="stylesheet"><script>tailwind.config={theme:{extend:{colors:{primary:"#1152d4","background-light":"#f6f6f8"},fontFamily:{display:["Inter","sans-serif"]}}}}</script></head><body class="bg-background-light font-display text-slate-900 flex min-h-screen items-center justify-center p-4"><div class="rounded-2xl border border-slate-200 bg-white p-8 shadow-sm max-w-md w-full text-center"><div class="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-emerald-100"><span class="text-3xl">&#10003;</span></div><h1 class="text-2xl font-black">Email confermata!</h1><p class="mt-3 text-slate-600">Il tuo indirizzo email è stato verificato con successo. Ora puoi acquistare biglietti.</p><a href="/index.html" class="mt-6 inline-block rounded-lg bg-primary px-6 py-3 text-sm font-bold text-white">Vai alla Home</a></div></body></html>""", "text/html; charset=utf-8");
    }

    private static string NormalizeUsername(string value)
    {
        var clean = new string(value.Trim().Where(ch => char.IsLetterOrDigit(ch) || ch == '_' || ch == '.').ToArray());
        if (string.IsNullOrWhiteSpace(clean))
        {
            return "utente";
        }

        return clean.ToLowerInvariant();
    }

    private static string HashPassword(string password)
    {
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = sha.ComputeHash(bytes);
        return Convert.ToHexString(hash);
    }

    private static string MaskEmail(string email)
    {
        var at = email.IndexOf('@');
        if (at <= 1)
        {
            return "***";
        }

        var userPart = email[..at];
        var domainPart = email[at..];
        return $"{userPart[0]}***{userPart[^1]}{domainPart}";
    }

    private static async Task<string> EnsureUniqueUsernameAsync(string desired, FilmDbContext db, CancellationToken ct)
    {
        var normalized = NormalizeUsername(desired);
        var candidate = normalized;
        var counter = 1;
        while (await db.Utenti.AnyAsync(u => u.Username == candidate, ct))
        {
            counter++;
            candidate = $"{normalized}{counter}";
        }

        return candidate;
    }
}
