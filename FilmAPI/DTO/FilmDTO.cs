namespace FilmAPI.DTO;

public record FilmDTO(
    int Id,
    string Titolo,
    DateOnly DataProduzione,
    int RegistaId,
    int Durata,
    string? CopertinaPath,
    string? FilmatoPath);
