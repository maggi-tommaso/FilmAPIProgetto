namespace FilmAPI.Model;

public class Proiezione
{
    public int Id { get; set; }
    public int CinemaId { get; set; }
    public int FilmId { get; set; }
    public DateOnly Data { get; set; }
    public TimeOnly Ora { get; set; }

    public Cinema? Cinema { get; set; }
    public Film? Film { get; set; }
}
