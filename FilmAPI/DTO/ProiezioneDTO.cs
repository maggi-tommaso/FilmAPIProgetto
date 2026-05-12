namespace FilmAPI.DTO;

public record ProiezioneDTO(
    int Id,
    int CinemaId,
    int FilmId,
    DateOnly Data,
    TimeOnly Ora);
