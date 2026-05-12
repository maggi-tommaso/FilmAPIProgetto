namespace FilmAPI.DTO;

public record LoginResponseDTO(
    string Token,
    DateTime ExpiresAtUtc,
    string Username,
    string Nome,
    string Cognome,
    string Email,
    string Provider,
    bool EmailConfermata);
