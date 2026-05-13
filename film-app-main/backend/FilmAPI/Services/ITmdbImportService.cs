namespace FilmAPI.Services;

public class TmdbImportResult
{
    public int FilmImportati { get; set; }
    public int FilmAggiornati { get; set; }
    public int FilmSaltati { get; set; }
    public int ProiezioniGenerate { get; set; }
    public List<string> Errori { get; set; } = new();
}

public interface ITmdbImportService
{
    Task<TmdbImportResult> ImportNowPlayingAsync(int movieCount = 20, int showDays = 15);
    Task<TmdbImportResult> EnrichPostersAsync();
}
