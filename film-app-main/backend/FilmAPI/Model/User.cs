using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FilmAPI.Model;

public class User
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string NormalizedEmail { get; set; } = string.Empty;

    public string? PasswordHash { get; set; }

    public bool LocalCredentialsEnabled { get; set; } = true;

    public DateTime? EmailVerifiedAtUtc { get; set; }

    public DateTime? PasswordChangedAtUtc { get; set; }

    public bool MustChangePassword { get; set; }

    public int AuthVersion { get; set; }

    public DateTime? LastLoginAtUtc { get; set; }

    [MaxLength(30)]
    public string? LastLoginProvider { get; set; }

    public bool IsDisabled { get; set; }

    [Required]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Cognome { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Telefono { get; set; }

    [Required]
    public UserRole Ruolo { get; set; }

    [Required]
    public DateTime DataRegistrazione { get; set; }

    public int? CinemaPreferitoId { get; set; }

    [ForeignKey(nameof(CinemaPreferitoId))]
    public Cinema? CinemaPreferito { get; set; }

    public int? FilmPreferitoId { get; set; }

    [ForeignKey(nameof(FilmPreferitoId))]
    public Film? FilmPreferito { get; set; }

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal CreditoResiduo { get; set; }

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<Ordine> Ordini { get; set; } = new List<Ordine>();
    public ICollection<Biglietto> Biglietti { get; set; } = new List<Biglietto>();
    public ICollection<UserExternalLogin> ExternalLogins { get; set; } = new List<UserExternalLogin>();
    public ICollection<AccountActionToken> ActionTokens { get; set; } = new List<AccountActionToken>();
}
