namespace FilmAPI.DTO;

public record RegisterRequestDTO(
    string Username,
    string Nome,
    string Cognome,
    string Email,
    string Password);
