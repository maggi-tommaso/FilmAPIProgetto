using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FilmAPI.Data;
using FilmAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Services;

public class TmdbImportService : ITmdbImportService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly FilmDbContext _db;
    private readonly ILogger<TmdbImportService> _logger;
    private readonly IConfiguration _configuration;

    public TmdbImportService(
        IHttpClientFactory httpClientFactory,
        FilmDbContext db,
        ILogger<TmdbImportService> logger,
        IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _db = db;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<TmdbImportResult> ImportNowPlayingAsync(int movieCount = 20, int showDays = 15)
    {
        var result = new TmdbImportResult();
        var client = _httpClientFactory.CreateClient("TmdbImport");

        List<TmdbNowPlayingItem> nowPlayingMovies;
        try
        {
            var npResponse = await client.GetFromJsonAsync<TmdbNowPlayingResponse>(
                $"https://api.themoviedb.org/3/movie/now_playing?language=it-IT&page=1&region=IT");
            nowPlayingMovies = npResponse?.Results?.Take(movieCount).ToList() ?? new List<TmdbNowPlayingItem>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nel recupero now_playing da TMDB");
            result.Errori.Add($"Errore now_playing: {ex.Message}");
            return result;
        }

        var moviesToImport = new List<TmdbMovieDetail>();
        foreach (var np in nowPlayingMovies)
        {
            try
            {
                var detail = await client.GetFromJsonAsync<TmdbMovieDetail>(
                    $"https://api.themoviedb.org/3/movie/{np.Id}?language=it-IT&append_to_response=videos,credits");
                if (detail is not null)
                    moviesToImport.Add(detail);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Saltato film TMDB {TmdbId}: {Message}", np.Id, ex.Message);
                result.Errori.Add($"Saltato film {np.Title}: {ex.Message}");
                result.FilmSaltati++;
            }
        }

        var cinemas = await _db.Cinemas.ToListAsync();
        var sale = await _db.Sale.Include(s => s.Cinema).Where(s => s.IsAttiva).ToListAsync();

        var showKeySet = new HashSet<string>(
            await _db.Shows.Select(s => s.CinemaId + "|" + s.SalaId + "|" + s.StartAtUtc.ToString("yyyyMMddHHmm")).ToListAsync()
        );

        foreach (var detail in moviesToImport)
        {
            try
            {
                var wasNew = await UpsertFilmAsync(detail);
                if (wasNew)
                    result.FilmImportati++;
                else
                    result.FilmAggiornati++;

                var film = await _db.Films.FirstOrDefaultAsync(f => f.TmdbId == detail.Id);
                if (film is null) continue;

                var filmCats = await _db.FilmCategorie.Where(fc => fc.FilmId == film.Id)
                    .Select(fc => fc.CategoriaId).ToListAsync();
                var filmDurata = film.Durata > 0 ? film.Durata : 120;

                foreach (var cinema in cinemas)
                {
                    var cinemaSale = sale.Where(s => s.CinemaId == cinema.Id).ToList();
                    if (cinemaSale.Count == 0) continue;

                    var today = DateTime.UtcNow.Date.AddHours(13);
                    for (int day = 0; day < showDays; day++)
                    {
                        var date = today.AddDays(day).Date;
                        var timeSlots = new[] { 15, 18, 21, 23 };

                        for (int slotIdx = 0; slotIdx < timeSlots.Length; slotIdx++)
                        {
                            var sala = cinemaSale[slotIdx % cinemaSale.Count];
                            var oraItaliana = timeSlots[slotIdx];

                            var timeZone = ResolveItalianTimeZone();
                            var localTime = new DateTime(date.Year, date.Month, date.Day, oraItaliana, 30, 0);
                            var utcTime = TimeZoneInfo.ConvertTimeToUtc(localTime, timeZone);

                            var key = $"{cinema.Id}|{sala.Id}|{utcTime:yyyyMMddHHmm}";
                            if (showKeySet.Contains(key)) continue;

                            var existingShow = await _db.Shows
                                .FirstOrDefaultAsync(s => s.CinemaId == cinema.Id
                                                           && s.SalaId == sala.Id
                                                           && s.StartAtUtc == utcTime);

                            if (existingShow is null)
                            {
                                var show = new Show
                                {
                                    CinemaId = cinema.Id,
                                    SalaId = sala.Id,
                                    FilmId = film.Id,
                                    StartAtUtc = utcTime,
                                    DurataMinutiSnapshot = filmDurata + 15,
                                    PrezzoBase = 8.50m,
                                    SupplementoSala = sala.Supplemento
                                };
                                _db.Shows.Add(show);
                                showKeySet.Add(key);
                                result.ProiezioniGenerate++;
                            }
                        }
                    }
                }

                if (result.ProiezioniGenerate > 0)
                    await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Errore import film {Title}: {Message}", detail.Title, ex.Message);
                result.Errori.Add($"Errore film {detail.Title}: {ex.Message}");
                result.FilmSaltati++;
            }
        }

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(ex, "Alcune proiezioni duplicate ignorate");
        }

        return result;
    }

    public async Task<TmdbImportResult> EnrichPostersAsync()
    {
        var result = new TmdbImportResult();
        var client = _httpClientFactory.CreateClient("TmdbImport");

        var filmsWithoutPoster = await _db.Films
            .Where(f => f.PosterUrl == null || f.CopertinaPath == null)
            .ToListAsync();

        foreach (var film in filmsWithoutPoster)
        {
            try
            {
                var encodedTitle = Uri.EscapeDataString(film.Titolo);
                var searchResponse = await client.GetFromJsonAsync<TmdbSearchResponse>(
                    $"https://api.themoviedb.org/3/search/movie?language=it-IT&query={encodedTitle}&page=1");

                var firstResult = searchResponse?.Results?.FirstOrDefault();
                if (firstResult is null) continue;

                var detail = await client.GetFromJsonAsync<TmdbMovieDetail>(
                    $"https://api.themoviedb.org/3/movie/{firstResult.Id}?language=it-IT&append_to_response=videos,credits");

                if (detail is null) continue;

                var posterUrl = BuildImageUrl(detail.PosterPath, "w500");
                var backdropUrl = BuildImageUrl(detail.BackdropPath, "w1280");

                if (posterUrl is not null)
                {
                    film.PosterUrl = posterUrl;
                    film.CopertinaPath = posterUrl;
                }
                film.BackdropUrl ??= backdropUrl;
                film.VotoMedio ??= detail.VoteAverage;
                film.TitoloOriginale ??= detail.OriginalTitle;
                film.LinguaOriginale ??= detail.OriginalLanguage;
                film.TmdbId ??= detail.Id;

                if (detail.ReleaseDate.HasValue && film.DataRilascio is null)
                    film.DataRilascio = DateOnly.FromDateTime(detail.ReleaseDate.Value);

                if (string.IsNullOrEmpty(film.DescrizioneLunga))
                    film.DescrizioneLunga = detail.Overview;

                if (string.IsNullOrEmpty(film.CastText) && detail.Credits?.Cast is { Count: > 0 })
                {
                    var cast = detail.Credits.Cast.Take(10)
                        .Select(c => c.Name ?? string.Empty)
                        .Where(n => !string.IsNullOrEmpty(n));
                    film.CastText = string.Join(", ", cast);
                }

                result.FilmAggiornati++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Arricchimento poster fallito per {Titolo}: {Message}", film.Titolo, ex.Message);
                result.Errori.Add($"Film {film.Titolo}: {ex.Message}");
                result.FilmSaltati++;
            }
        }

        await _db.SaveChangesAsync();
        return result;
    }

    private async Task<bool> UpsertFilmAsync(TmdbMovieDetail detail)
    {
        var existing = await _db.Films
            .Include(f => f.FilmCategorie)
            .FirstOrDefaultAsync(f => f.TmdbId == detail.Id);

        var posterUrl = BuildImageUrl(detail.PosterPath, "w500");
        var backdropUrl = BuildImageUrl(detail.BackdropPath, "w1280");
        string? trailerUrl = null;

        if (detail.Videos?.Results is { Count: > 0 })
        {
            var trailer = detail.Videos.Results
                .FirstOrDefault(v => v.Site?.Equals("YouTube", StringComparison.OrdinalIgnoreCase) == true
                                     && v.Type?.Equals("Trailer", StringComparison.OrdinalIgnoreCase) == true);
            if (trailer?.Key is not null)
                trailerUrl = $"https://www.youtube.com/watch?v={trailer.Key}";
        }

        var cast = detail.Credits?.Cast?.Take(10)
            .Select(c => c.Name ?? string.Empty)
            .Where(n => !string.IsNullOrEmpty(n));

        int registaId;
        if (detail.Credits?.Crew is { Count: > 0 })
        {
            var director = detail.Credits.Crew
                .FirstOrDefault(c => c.Job?.Equals("Director", StringComparison.OrdinalIgnoreCase) == true);

            if (director?.Name is not null)
            {
                var parts = director.Name.Trim().Split(' ');
                var cognome = parts[^1];
                var nome = string.Join(" ", parts.Take(parts.Length - 1));
                registaId = await GetOrCreateRegistaAsync(nome, cognome);
            }
            else
            {
                registaId = 1;
            }
        }
        else
        {
            registaId = 1;
        }

        var durata = detail.Runtime > 0 ? detail.Runtime : 90;
        var releaseDate = detail.ReleaseDate.HasValue
            ? DateOnly.FromDateTime(detail.ReleaseDate.Value)
            : (DateOnly?)null;

        if (existing is not null)
        {
            existing.Titolo = string.IsNullOrEmpty(existing.Titolo) ? (detail.Title ?? existing.Titolo) : existing.Titolo;
            existing.DescrizioneLunga ??= detail.Overview;
            existing.CastText ??= cast is not null ? string.Join(", ", cast) : null;
            existing.CopertinaPath ??= posterUrl;
            existing.PosterUrl ??= posterUrl;
            existing.BackdropUrl ??= backdropUrl;
            existing.TrailerUrl ??= trailerUrl;
            existing.VotoMedio ??= detail.VoteAverage;
            existing.TitoloOriginale ??= detail.OriginalTitle;
            existing.LinguaOriginale ??= detail.OriginalLanguage;
            existing.DataRilascio ??= releaseDate;
            if (existing.Durata == 0) existing.Durata = durata;
            existing.DataProduzione = releaseDate?.ToDateTime(TimeOnly.MinValue) ?? existing.DataProduzione;

            await SyncCategorieForFilmAsync(existing, detail.Genres);
            await _db.SaveChangesAsync();
            return false;
        }

        var film = new Film
        {
            Titolo = detail.Title ?? "Senza titolo",
            TitoloOriginale = detail.OriginalTitle,
            LinguaOriginale = detail.OriginalLanguage,
            DataProduzione = releaseDate?.ToDateTime(TimeOnly.MinValue) ?? DateTime.UtcNow,
            RegistaId = registaId,
            Durata = durata,
            CopertinaPath = posterUrl,
            PosterUrl = posterUrl,
            BackdropUrl = backdropUrl,
            TrailerUrl = trailerUrl,
            DescrizioneLunga = detail.Overview,
            CastText = cast is not null ? string.Join(", ", cast) : null,
            DataRilascio = releaseDate,
            TmdbId = detail.Id,
            VotoMedio = detail.VoteAverage
        };

        _db.Films.Add(film);
        await _db.SaveChangesAsync();

        await SyncCategorieForFilmAsync(film, detail.Genres);
        await _db.SaveChangesAsync();
        return true;
    }

    private async Task<int> GetOrCreateRegistaAsync(string nome, string cognome)
    {
        var regista = await _db.Registi
            .FirstOrDefaultAsync(r => r.Nome == nome && r.Cognome == cognome);

        if (regista is not null)
            return regista.Id;

        regista = new Regista
        {
            Nome = string.IsNullOrWhiteSpace(nome) ? "Da determinare" : nome,
            Cognome = cognome,
            Nazionalita = "Da determinare"
        };

        _db.Registi.Add(regista);
        await _db.SaveChangesAsync();
        return regista.Id;
    }

    private async Task SyncCategorieForFilmAsync(Film film, List<TmdbGenre>? genres)
    {
        if (genres is null or { Count: 0 }) return;

        var existingLinks = await _db.FilmCategorie
            .Where(fc => fc.FilmId == film.Id)
            .Select(fc => fc.CategoriaId)
            .ToListAsync();

        var mappedIds = new HashSet<int>();
        foreach (var genre in genres)
        {
            if (string.IsNullOrWhiteSpace(genre.Name)) continue;
            var mappedName = MapTmdbGenreToLocal(genre.Name);
            var categoria = await GetOrCreateCategoriaAsync(mappedName);

            if (!existingLinks.Contains(categoria.Id) && mappedIds.Add(categoria.Id))
            {
                _db.FilmCategorie.Add(new FilmCategoria
                {
                    FilmId = film.Id,
                    CategoriaId = categoria.Id
                });
            }
        }
    }

    private async Task<Categoria> GetOrCreateCategoriaAsync(string nome)
    {
        var existing = await _db.Categorie.FirstOrDefaultAsync(c => c.Nome == nome);
        if (existing is not null) return existing;

        var newCat = new Categoria { Nome = nome };
        _db.Categorie.Add(newCat);
        return newCat;
    }

    private static string? MapTmdbGenreToLocal(string genreName)
    {
        return genreName switch
        {
            "Action" or "Azione" => "Azione",
            "Adventure" or "Avventura" => "Avventura",
            "Animation" or "Animazione" => "Animazione",
            "Comedy" or "Commedia" => "Commedia",
            "Crime" or "Crimine" => "Thriller",
            "Documentary" or "Documentario" => "Documentario",
            "Drama" or "Dramma" => "Drammatico",
            "Family" or "Famiglia" => "Animazione",
            "Fantasy" => "Fantasy",
            "History" or "Storia" => "Storico",
            "Horror" => "Horror",
            "Music" or "Musica" => "Documentario",
            "Mystery" or "Mistero" => "Thriller",
            "Romance" or "Romantico" => "Romantico",
            "Science Fiction" or "Fantascienza" => "Fantascienza",
            "Thriller" => "Thriller",
            "War" or "Guerra" => "Storico",
            "Western" => "Azione",
            "TV Movie" => "Drammatico",
            _ => genreName
        };
    }

    private static string? BuildImageUrl(string? path, string size)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        return $"https://image.tmdb.org/t/p/{size}{path}";
    }

    private static TimeZoneInfo ResolveItalianTimeZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Europe/Rome"); }
        catch { }
        try { return TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time"); }
        catch { }
        try { return TimeZoneInfo.FindSystemTimeZoneById("Central Europe Standard Time"); }
        catch { }
        return TimeZoneInfo.Utc;
    }

    private class TmdbNowPlayingResponse
    {
        [JsonPropertyName("results")]
        public List<TmdbNowPlayingItem>? Results { get; set; }
    }

    private class TmdbNowPlayingItem
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }
    }

    private class TmdbMovieDetail
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("original_title")]
        public string? OriginalTitle { get; set; }

        [JsonPropertyName("original_language")]
        public string? OriginalLanguage { get; set; }

        [JsonPropertyName("overview")]
        public string? Overview { get; set; }

        [JsonPropertyName("release_date")]
        public DateTime? ReleaseDate { get; set; }

        [JsonPropertyName("runtime")]
        public int Runtime { get; set; }

        [JsonPropertyName("poster_path")]
        public string? PosterPath { get; set; }

        [JsonPropertyName("backdrop_path")]
        public string? BackdropPath { get; set; }

        [JsonPropertyName("vote_average")]
        public decimal? VoteAverage { get; set; }

        [JsonPropertyName("genres")]
        public List<TmdbGenre>? Genres { get; set; }

        [JsonPropertyName("videos")]
        public TmdbVideos? Videos { get; set; }

        [JsonPropertyName("credits")]
        public TmdbCredits? Credits { get; set; }
    }

    private class TmdbGenre
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    private class TmdbVideos
    {
        [JsonPropertyName("results")]
        public List<TmdbVideo>? Results { get; set; }
    }

    private class TmdbVideo
    {
        [JsonPropertyName("key")]
        public string? Key { get; set; }

        [JsonPropertyName("site")]
        public string? Site { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }
    }

    private class TmdbCredits
    {
        [JsonPropertyName("cast")]
        public List<TmdbCastMember>? Cast { get; set; }

        [JsonPropertyName("crew")]
        public List<TmdbCrewMember>? Crew { get; set; }
    }

    private class TmdbCastMember
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    private class TmdbCrewMember
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("job")]
        public string? Job { get; set; }
    }

    private class TmdbSearchResponse
    {
        [JsonPropertyName("results")]
        public List<TmdbSearchResultItem>? Results { get; set; }
    }

    private class TmdbSearchResultItem
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
    }
}
