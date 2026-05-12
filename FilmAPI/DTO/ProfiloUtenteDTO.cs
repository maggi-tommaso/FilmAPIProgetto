namespace FilmAPI.DTO;

public record ProfiloUtenteDTO(
    string Username,
    string Nome,
    string Cognome,
    string Email,
    string EmailCensurata,
    string Provider,
    bool EmailConfermata,
    DateTime CreatoIlUtc,
    DateTime? UltimoAccessoUtc,
    IReadOnlyList<BigliettoUtenteDTO> BigliettiConvalidati,
    IReadOnlyList<BigliettoUtenteDTO> BigliettiNonConvalidati);
