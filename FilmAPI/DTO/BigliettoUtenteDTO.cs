namespace FilmAPI.DTO;

public record BigliettoUtenteDTO(
    string Codice,
    DateTime AcquistatoIlUtc,
    DateTime? ConvalidatoIlUtc,
    bool IsConvalidato);
