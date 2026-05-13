namespace FilmAPI.DTO;

public class FilmDTO
{
    public int Id { get; set; }
    public string Titolo { get; set; } = string.Empty;
    public DateTime DataProduzione { get; set; }
    public int RegistaId { get; set; }
    public string? RegistaNome { get; set; }
    public string? RegistaCognome { get; set; }
    public int Durata { get; set; }
    public string? CopertinaPath { get; set; }
    public string? FilmatoPath { get; set; }
    public string? DescrizioneLunga { get; set; }
    public string? CastText { get; set; }
    public DateOnly? DataRilascio { get; set; }
    public int? TmdbId { get; set; }
    public string? TitoloOriginale { get; set; }
    public string? LinguaOriginale { get; set; }
    public string? PosterUrl { get; set; }
    public string? BackdropUrl { get; set; }
    public string? TrailerUrl { get; set; }
    public decimal? VotoMedio { get; set; }
    public List<CategoriaDTO> Categorie { get; set; } = new();
}

public class FilmPagedResultDTO
{
    public List<FilmDTO> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}

public class FilmCreateDTO
{
    public string Titolo { get; set; } = string.Empty;
    public DateTime DataProduzione { get; set; }
    public int RegistaId { get; set; }
    public int Durata { get; set; }
    public string? CopertinaPath { get; set; }
    public string? FilmatoPath { get; set; }
    public string? DescrizioneLunga { get; set; }
    public string? CastText { get; set; }
    public DateOnly? DataRilascio { get; set; }
    public int? TmdbId { get; set; }
    public string? TitoloOriginale { get; set; }
    public string? LinguaOriginale { get; set; }
    public string? PosterUrl { get; set; }
    public string? BackdropUrl { get; set; }
    public string? TrailerUrl { get; set; }
    public decimal? VotoMedio { get; set; }
    public List<int>? CategorieIds { get; set; }
}

public class FilmUpdateDTO
{
    public string Titolo { get; set; } = string.Empty;
    public DateTime DataProduzione { get; set; }
    public int RegistaId { get; set; }
    public int Durata { get; set; }
    public string? CopertinaPath { get; set; }
    public string? FilmatoPath { get; set; }
    public string? DescrizioneLunga { get; set; }
    public string? CastText { get; set; }
    public DateOnly? DataRilascio { get; set; }
    public int? TmdbId { get; set; }
    public string? TitoloOriginale { get; set; }
    public string? LinguaOriginale { get; set; }
    public string? PosterUrl { get; set; }
    public string? BackdropUrl { get; set; }
    public string? TrailerUrl { get; set; }
    public decimal? VotoMedio { get; set; }
    public List<int>? CategorieIds { get; set; }
}
