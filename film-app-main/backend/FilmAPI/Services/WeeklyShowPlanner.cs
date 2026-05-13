using FilmAPI.Data;
using FilmAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Services;

public class WeeklyShowPlanner
{
    private readonly FilmDbContext _db;
    private readonly ILogger<WeeklyShowPlanner> _logger;

    public WeeklyShowPlanner(FilmDbContext db, ILogger<WeeklyShowPlanner> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task PlanCurrentWeekAsync()
    {
        var today = DateTime.UtcNow.Date;
        var monday = today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday);
        if (today.DayOfWeek == DayOfWeek.Sunday)
            monday = today.AddDays(-6);

        var sunday = monday.AddDays(6);

        _logger.LogInformation("Show planner: pianificazione settimana {Mon:dd/MM} - {Sun:dd/MM} in corso...",
            monday, sunday);

        var films = await _db.Films.ToListAsync();
        var cinemas = await _db.Cinemas.ToListAsync();
        var sale = await _db.Sale.Where(s => s.IsAttiva).Include(s => s.Cinema).ToListAsync();

        if (films.Count == 0 || cinemas.Count == 0 || sale.Count == 0)
        {
            _logger.LogWarning("Show planner: dati insufficienti (film={F}, cinema={C}, sale={S})",
                films.Count, cinemas.Count, sale.Count);
            return;
        }

        var rng = new Random((int)monday.Ticks);
        var timezone = ResolveTimeZone();
        var timeSlots = new[] { 14, 16, 18, 20, 22 };
        var showKeys = new HashSet<string>();

        var existingKeys = await _db.Shows
            .Where(s => s.StartAtUtc >= monday && s.StartAtUtc < sunday.AddDays(1))
            .Select(s => s.CinemaId + "|" + s.SalaId + "|" + s.StartAtUtc.ToString("yyyyMMddHHmm"))
            .ToListAsync();

        foreach (var k in existingKeys) showKeys.Add(k);

        var showsToAdd = new List<Show>();

        foreach (var cinema in cinemas)
        {
            var cinemaSale = sale.Where(s => s.CinemaId == cinema.Id).ToList();
            if (cinemaSale.Count == 0) continue;

            var filmsForCinema = films.OrderBy(_ => rng.Next()).ToList();

            // Phase 1: guarantee each film at least 1 show in this cinema
            var guaranteedSlots = new List<(Sala Sala, DateTime Date, int Hour, DateTime UtcTime)>();
            var remainingSlots = new List<(Sala Sala, DateTime Date, int Hour, DateTime UtcTime)>();

            for (var day = 0; day < 7; day++)
            {
                var date = monday.AddDays(day);
                foreach (var sala in cinemaSale)
                {
                    var minSlots = filmsForCinema.Count / (cinemaSale.Count * 7) + 1;
                    var slotCount = Math.Min(timeSlots.Length, rng.Next(Math.Max(4, minSlots), timeSlots.Length + 1));
                    var chosenSlots = timeSlots
                        .OrderBy(_ => rng.Next())
                        .Take(slotCount)
                        .OrderBy(t => t)
                        .ToList();

                    foreach (var hour in chosenSlots)
                    {
                        var localTime = new DateTime(date.Year, date.Month, date.Day, hour, 0, 0);
                        var utcTime = TimeZoneInfo.ConvertTimeToUtc(localTime, timezone);
                        var key = $"{cinema.Id}|{sala.Id}|{utcTime:yyyyMMddHHmm}";
                        if (showKeys.Contains(key)) continue;
                        showKeys.Add(key);

                        var slot = (sala, date, hour, utcTime);
                        if (guaranteedSlots.Count < filmsForCinema.Count)
                            guaranteedSlots.Add(slot);
                        else
                            remainingSlots.Add(slot);
                    }
                }
            }

            // Assign guaranteed slots: each film gets exactly 1
            for (int i = 0; i < guaranteedSlots.Count; i++)
            {
                var film = filmsForCinema[i % filmsForCinema.Count];
                var slot = guaranteedSlots[i];
                showsToAdd.Add(MakeShow(cinema.Id, slot.Sala, film, slot.UtcTime, slot.Date, slot.Hour));
            }

            // Fill remaining slots with random films (weighted towards uncovered first)
            var filmShowCount = filmsForCinema.ToDictionary(f => f.Id, f => guaranteedSlots.Any() ? 1 : 0);
            foreach (var slot in remainingSlots)
            {
                var film = filmsForCinema.OrderBy(f => filmShowCount[f.Id]).ThenBy(_ => rng.Next()).First();
                filmShowCount[film.Id]++;
                showsToAdd.Add(MakeShow(cinema.Id, slot.Sala, film, slot.UtcTime, slot.Date, slot.Hour));
            }
        }

        if (showsToAdd.Count > 0)
        {
            _db.Shows.AddRange(showsToAdd);
            await _db.SaveChangesAsync();

            var countsByCinema = showsToAdd
                .GroupBy(s => s.CinemaId)
                .ToDictionary(g => g.Key, g => g.GroupBy(s => s.FilmId).Count());
            var minFilmsPerCinema = countsByCinema.Values.DefaultIfEmpty(0).Min();
            var maxFilmsPerCinema = countsByCinema.Values.DefaultIfEmpty(0).Max();

            _logger.LogInformation("Show planner: {Count} spettacoli, film per cinema: {Min}-{Max} (target={Target})",
                showsToAdd.Count, minFilmsPerCinema, maxFilmsPerCinema, films.Count);
        }
    }

    private static Show MakeShow(int cinemaId, Sala sala, Film film, DateTime utcTime, DateTime date, int hour)
    {
        return new Show
        {
            CinemaId = cinemaId,
            SalaId = sala.Id,
            FilmId = film.Id,
            StartAtUtc = utcTime,
            DurataMinutiSnapshot = film.Durata + 15,
            PrezzoBase = 8.50m,
            SupplementoSala = sala.Supplemento
        };
    }

    private static TimeZoneInfo ResolveTimeZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Europe/Rome"); }
        catch { }
        try { return TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time"); }
        catch { }
        try { return TimeZoneInfo.FindSystemTimeZoneById("Central Europe Standard Time"); }
        catch { }
        return TimeZoneInfo.Utc;
    }
}
