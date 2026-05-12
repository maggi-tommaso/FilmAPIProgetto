namespace FilmAPI.Model;

public class SessioneAccesso
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int UtenteId { get; set; }
    public DateTime CreatoIlUtc { get; set; } = DateTime.UtcNow;
    public DateTime ScadeIlUtc { get; set; }
    public DateTime? RevocatoIlUtc { get; set; }

    public Utente? Utente { get; set; }
}
