namespace FilmAPI.Services;

public class TmdbSearchResult
{
    public int TmdbId { get; set; }
    public string Titolo { get; set; } = string.Empty;
    public string? TitoloOriginale { get; set; }
    public string? LinguaOriginale { get; set; }
    public int? Anno { get; set; }
    public int? Durata { get; set; }
    public string? DescrizioneLunga { get; set; }
    public string? CastText { get; set; }
    public string? PosterUrl { get; set; }
    public string? BackdropUrl { get; set; }
    public string? TrailerUrl { get; set; }
    public decimal? VotoMedio { get; set; }
    public string? RegistaNome { get; set; }
    public string? RegistaCognome { get; set; }
    public List<string> Generi { get; set; } = new();
    public int? RegistaId { get; set; }
    public List<int> CategorieIds { get; set; } = new();
}

public interface ITmdbService
{
    Task<TmdbSearchResult?> SearchMovieAsync(string title);
}
