namespace FilmAPI.Model;

public class Film
{
    public int Id { get; set; }
    public string Titolo { get; set; } = string.Empty;
    public DateOnly DataProduzione { get; set; }
    public int RegistaId { get; set; }
    public int Durata { get; set; }
    public string? CopertinaPath { get; set; }
    public string? FilmatoPath { get; set; }

    public Regista? Regista { get; set; }
    public ICollection<Proiezione> Proiezioni { get; set; } = new List<Proiezione>();
}
