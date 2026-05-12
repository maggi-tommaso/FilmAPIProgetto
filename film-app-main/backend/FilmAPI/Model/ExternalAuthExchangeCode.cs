using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FilmAPI.Model;

public class ExternalAuthExchangeCode
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [Required]
    [MaxLength(128)]
    public string CodeHash { get; set; } = string.Empty;

    [Required]
    [MaxLength(512)]
    public string RedirectPath { get; set; } = string.Empty;

    [Required]
    public DateTime CreatedAtUtc { get; set; }

    [Required]
    public DateTime ExpiresAtUtc { get; set; }

    public DateTime? ConsumedAtUtc { get; set; }

    [Required]
    public ExternalLoginProvider Provider { get; set; }

    public bool IsConsumed => ConsumedAtUtc != null;
    public bool IsExpired => DateTime.UtcNow > ExpiresAtUtc;
}
