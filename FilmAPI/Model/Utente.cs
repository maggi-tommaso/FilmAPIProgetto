namespace FilmAPI.Model;

public class Utente
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Cognome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool EmailConfermata { get; set; }
    public Guid? EmailTokenConferma { get; set; }
    public DateTime? EmailTokenScadeIlUtc { get; set; }
    public string? GoogleSubject { get; set; }
    public string? PasswordHash { get; set; }
    public string Provider { get; set; } = "local";
    public DateTime CreatoIlUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UltimoAccessoUtc { get; set; }

    public ICollection<SessioneAccesso> Sessioni { get; set; } = new List<SessioneAccesso>();
    public ICollection<BigliettoUtente> Biglietti { get; set; } = new List<BigliettoUtente>();
}
