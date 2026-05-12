using System.ComponentModel.DataAnnotations;

namespace FilmAPI.DTO;

public class UserAdminDTO
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Cognome { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string Ruolo { get; set; } = string.Empty;
    public DateTime DataRegistrazione { get; set; }
}

public class UpdateRuoloDTO
{
    [Required]
    [RegularExpression("^(User|PowerUser|Admin)$")]
    public string NuovoRuolo { get; set; } = string.Empty;
}
