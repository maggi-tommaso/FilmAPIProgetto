namespace FilmAPI.Services;

public class NotificaDTO
{
    public int Id { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Priorita { get; set; } = "normale";
    public string Messaggio { get; set; } = string.Empty;
    public string? AzioneLabel { get; set; }
    public string? AzioneUrl { get; set; }
    public int? FilmId { get; set; }
    public string? FilmTitolo { get; set; }
    public string? FilmCopertina { get; set; }
}

public class ValutazioneCreateDTO
{
    public int FilmId { get; set; }
    public int Rating { get; set; }
}

public interface INotificheService
{
    Task<List<NotificaDTO>> GetNotificheAsync(int userId);
    Task<NotificaDTO> ValutaFilmAsync(int userId, int filmId, int rating);
}
