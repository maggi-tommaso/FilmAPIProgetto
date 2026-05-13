using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FilmAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Services;

public class TmdbService : ITmdbService
{
    private readonly HttpClient _httpClient;
    private readonly FilmDbContext _db;
    private readonly ILogger<TmdbService> _logger;

    public TmdbService(HttpClient httpClient, FilmDbContext db, ILogger<TmdbService> logger)
    {
        _httpClient = httpClient;
        _db = db;
        _logger = logger;
    }

    public async Task<TmdbSearchResult?> SearchMovieAsync(string title)
    {
        try
        {
            var encodedTitle = Uri.EscapeDataString(title);
            var searchResponse = await _httpClient.GetFromJsonAsync<TmdbPagedResponse>(
                $"https://api.themoviedb.org/3/search/movie?language=it-IT&query={encodedTitle}&page=1");

            var firstResult = searchResponse?.Results?.FirstOrDefault();
            if (firstResult is null)
                return null;

            var detail = await _httpClient.GetFromJsonAsync<TmdbMovieDetail>(
                $"https://api.themoviedb.org/3/movie/{firstResult.Id}?language=it-IT&append_to_response=videos,credits");

            if (detail is null)
                return null;

            return await MapToSearchResultAsync(detail);
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("Timeout nella ricerca TMDB per '{Title}'", title);
            return null;
        }
        catch (HttpRequestException ex)
        {
            if (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                _logger.LogWarning("TMDB API key non valida (401) per ricerca '{Title}'", title);
            else
                _logger.LogError(ex, "Errore HTTP nella ricerca TMDB per '{Title}'", title);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nella ricerca TMDB per '{Title}'", title);
            return null;
        }
    }

    private async Task<TmdbSearchResult> MapToSearchResultAsync(TmdbMovieDetail detail)
    {
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

        string? registaNome = null;
        string? registaCognome = null;
        int? registaId = null;

        if (detail.Credits?.Crew is { Count: > 0 })
        {
            var director = detail.Credits.Crew
                .FirstOrDefault(c => c.Job?.Equals("Director", StringComparison.OrdinalIgnoreCase) == true);

            if (director?.Name is not null)
            {
                var parts = director.Name.Trim().Split(' ');
                registaCognome = parts[^1];
                registaNome = string.Join(" ", parts.Take(parts.Length - 1));

                var existingRegista = await _db.Registi
                    .FirstOrDefaultAsync(r => r.Nome == registaNome && r.Cognome == registaCognome);
                registaId = existingRegista?.Id;
            }
        }

        var generi = detail.Genres?.Select(g => g.Name ?? string.Empty)
            .Where(n => !string.IsNullOrEmpty(n))
            .Select(n => n!)
            .ToList() ?? new List<string>();

        var categorieIds = new List<int>();
        foreach (var genere in generi)
        {
            var mapped = MapTmdbGenreToLocal(genere);
            if (mapped is not null)
            {
                var cat = await _db.Categorie.FirstOrDefaultAsync(c => c.Nome == mapped);
                if (cat is not null)
                    categorieIds.Add(cat.Id);
            }
        }

        var cast = detail.Credits?.Cast?.Take(10)
            .Select(c => c.Name ?? string.Empty)
            .Where(n => !string.IsNullOrEmpty(n));

        return new TmdbSearchResult
        {
            TmdbId = detail.Id,
            Titolo = detail.Title ?? string.Empty,
            TitoloOriginale = detail.OriginalTitle,
            LinguaOriginale = detail.OriginalLanguage,
            Anno = detail.ReleaseDate?.Year,
            Durata = detail.Runtime > 0 ? detail.Runtime : 90,
            DescrizioneLunga = detail.Overview,
            CastText = cast is not null ? string.Join(", ", cast) : null,
            PosterUrl = posterUrl,
            BackdropUrl = backdropUrl,
            TrailerUrl = trailerUrl,
            VotoMedio = detail.VoteAverage,
            RegistaNome = registaNome,
            RegistaCognome = registaCognome,
            RegistaId = registaId,
            Generi = generi,
            CategorieIds = categorieIds
        };
    }

    private static string? BuildImageUrl(string? path, string size)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;
        return $"https://image.tmdb.org/t/p/{size}{path}";
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

    private class TmdbPagedResponse
    {
        [JsonPropertyName("results")]
        public List<TmdbSearchResultItem>? Results { get; set; }
    }

    private class TmdbSearchResultItem
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
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
}
