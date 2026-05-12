using System.ComponentModel.DataAnnotations;

namespace FilmAPI.Model;

public class ExternalAuthState
{
    [Key]
    public int Id { get; set; }

    [Required]
    public ExternalLoginProvider Provider { get; set; }

    [Required]
    [MaxLength(128)]
    public string StateHash { get; set; } = string.Empty;

    [Required]
    [MaxLength(256)]
    public string CodeVerifier { get; set; } = string.Empty;

    [Required]
    [MaxLength(128)]
    public string Nonce { get; set; } = string.Empty;

    [Required]
    [MaxLength(512)]
    public string RedirectPath { get; set; } = string.Empty;

    [Required]
    public DateTime CreatedAtUtc { get; set; }

    [Required]
    public DateTime ExpiresAtUtc { get; set; }

    public DateTime? ConsumedAtUtc { get; set; }

    [MaxLength(64)]
    public string? RequestIp { get; set; }

    [MaxLength(512)]
    public string? UserAgent { get; set; }

    public bool IsConsumed => ConsumedAtUtc != null;
    public bool IsExpired => DateTime.UtcNow > ExpiresAtUtc;
}
