namespace FilmAPI.Model;

public class Cinema
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Indirizzo { get; set; } = string.Empty;
    public string Citta { get; set; } = string.Empty;

    public ICollection<Proiezione> Proiezioni { get; set; } = new List<Proiezione>();
}
