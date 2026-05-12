namespace FilmAPI.DTO;

public record DatiProiezioneDTO(
    int Id,
    string CinemaNome,
    string FilmTitolo,
    DateOnly Data,
    TimeOnly Ora,
    int DurataFilm);
