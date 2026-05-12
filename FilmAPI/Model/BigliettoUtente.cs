namespace FilmAPI.Model;

public class BigliettoUtente
{
    public int Id { get; set; }
    public int UtenteId { get; set; }
    public string Codice { get; set; } = string.Empty;
    public DateTime AcquistatoIlUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ConvalidatoIlUtc { get; set; }
    public bool IsConvalidato { get; set; }

    public Utente? Utente { get; set; }
}
