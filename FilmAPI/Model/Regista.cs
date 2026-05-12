namespace FilmAPI.Model;

public class Regista
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Cognome { get; set; } = string.Empty;
    public string Nazionalita { get; set; } = string.Empty;

    public ICollection<Film> Films { get; set; } = new List<Film>();
}
