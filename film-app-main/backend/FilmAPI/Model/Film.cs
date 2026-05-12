using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FilmAPI.Model;

public class Film
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Titolo { get; set; } = string.Empty;

    [Required]
    public DateTime DataProduzione { get; set; }

    [Required]
    public int RegistaId { get; set; }

    [ForeignKey(nameof(RegistaId))]
    public Regista? Regista { get; set; }

    [Required]
    public int Durata { get; set; }

    [MaxLength(500)]
    public string? CopertinaPath { get; set; }

    [MaxLength(500)]
    public string? FilmatoPath { get; set; }

    [MaxLength(2000)]
    public string? DescrizioneLunga { get; set; }

    [MaxLength(2000)]
    public string? CastText { get; set; }

    public DateOnly? DataRilascio { get; set; }

    public ICollection<FilmCategoria> FilmCategorie { get; set; } = new List<FilmCategoria>();
    public ICollection<Show> Shows { get; set; } = new List<Show>();
}
