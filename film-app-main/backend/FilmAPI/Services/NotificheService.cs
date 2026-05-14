using FilmAPI.Data;
using FilmAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Services;

public class NotificheService : INotificheService
{
    private readonly FilmDbContext _db;

    public NotificheService(FilmDbContext db)
    {
        _db = db;
    }

    public async Task<List<NotificaDTO>> GetNotificheAsync(int userId)
    {
        var notifiche = new List<NotificaDTO>();
        var now = DateTime.UtcNow;
        var notificaId = 0;

        // 1) Email non verificata
        var user = await _db.Users.FindAsync(userId);
        if (user is not null && user.EmailVerifiedAtUtc is null)
        {
            notificaId++;
            notifiche.Add(new NotificaDTO
            {
                Id = notificaId,
                Tipo = "email_verifica",
                Priorita = "alta",
                Messaggio = "Il tuo indirizzo email non è stato ancora verificato.",
                AzioneLabel = "Reinvia email di verifica",
                AzioneUrl = "/profilo"
            });
        }

        // 2) Biglietti da convalidare (Issued, show non ancora terminato)
        var bigliettiDaValidare = await _db.Biglietti
            .Include(b => b.Show).ThenInclude(s => s!.Film)
            .Include(b => b.Show).ThenInclude(s => s!.Cinema)
            .Where(b => b.UserId == userId
                        && b.Stato == BigliettoState.Issued
                        && b.Show!.StartAtUtc.AddMinutes(b.Show.DurataMinutiSnapshot) >= now)
            .OrderBy(b => b.Show!.StartAtUtc)
            .Take(5)
            .ToListAsync();

        foreach (var b in bigliettiDaValidare)
        {
            notificaId++;
            var filmTitolo = b.Show?.Film?.Titolo ?? "Film";
            var startDate = b.Show!.StartAtUtc;
            var isImminent = startDate <= now.AddHours(2);
            var messaggio = isImminent
                ? $"Ricordati di convalidare il biglietto per \"{filmTitolo}\" delle {startDate:HH:mm}!"
                : $"Hai un biglietto per \"{filmTitolo}\" il {startDate:dd/MM} alle {startDate:HH:mm}";
            notifiche.Add(new NotificaDTO
            {
                Id = notificaId,
                Tipo = "validazione_biglietto",
                Priorita = isImminent ? "alta" : "normale",
                Messaggio = messaggio,
                AzioneLabel = "Vai alla validazione",
                AzioneUrl = $"/validazione-biglietti.html?codice={Uri.EscapeDataString(b.CodiceBiglietto)}",
                FilmId = b.Show!.FilmId,
                FilmTitolo = filmTitolo
            });
        }

        // 3) Film da valutare (biglietto validato, show passato, non ancora valutato)
        var ratedFilmIds = await _db.ValutazioniFilm
            .Where(v => v.UserId == userId)
            .Select(v => v.FilmId)
            .ToListAsync();

        var bigliettiDaValutareQuery = _db.Biglietti
            .Include(b => b.Show).ThenInclude(s => s!.Film)
            .Where(b => b.UserId == userId
                        && b.Stato == BigliettoState.Validated
                        && b.Show!.StartAtUtc.AddMinutes(b.Show.DurataMinutiSnapshot) < now
                        && !ratedFilmIds.Contains(b.Show!.FilmId));

        var filmsDaValutare = await bigliettiDaValutareQuery
            .Select(b => new { b.Show!.FilmId, b.Show.Film!.Titolo, b.Show.Film!.CopertinaPath })
            .Distinct()
            .Take(20)
            .ToListAsync();

        foreach (var f in filmsDaValutare)
        {
            notificaId++;
            notifiche.Add(new NotificaDTO
            {
                Id = notificaId,
                Tipo = "valutazione_film",
                Priorita = "normale",
                Messaggio = $"Hai visto \"{f.Titolo}\"? Lascia una valutazione!",
                AzioneLabel = null,
                FilmId = f.FilmId,
                FilmTitolo = f.Titolo,
                FilmCopertina = f.CopertinaPath
            });
        }

        return notifiche;
    }

    public async Task<NotificaDTO> ValutaFilmAsync(int userId, int filmId, int rating)
    {
        if (rating < 1 || rating > 5)
            throw new ArgumentException("Il rating deve essere tra 1 e 5");

        var existing = await _db.ValutazioniFilm
            .FirstOrDefaultAsync(v => v.UserId == userId && v.FilmId == filmId);

        if (existing is not null)
        {
            existing.Rating = rating;
            existing.CreatedAtUtc = DateTime.UtcNow;
        }
        else
        {
            _db.ValutazioniFilm.Add(new ValutazioneFilm
            {
                UserId = userId,
                FilmId = filmId,
                Rating = rating,
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();

        var film = await _db.Films.FindAsync(filmId);
        return new NotificaDTO
        {
            Id = 0,
            Tipo = "conferma_valutazione",
            Priorita = "normale",
            Messaggio = $"Hai valutato \"{film?.Titolo ?? "Film"}\" con {rating} stelle!",
            FilmId = filmId,
            FilmTitolo = film?.Titolo
        };
    }
}
