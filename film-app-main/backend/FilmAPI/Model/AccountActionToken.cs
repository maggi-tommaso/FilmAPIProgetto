using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FilmAPI.Model;

public class AccountActionToken
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [Required]
    public AccountActionTokenPurpose Purpose { get; set; }

    [Required]
    [MaxLength(128)]
    public string TokenHash { get; set; } = string.Empty;

    [Required]
    public DateTime ExpiresAtUtc { get; set; }

    [Required]
    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UsedAtUtc { get; set; }

    public DateTime? RevokedAtUtc { get; set; }

    public int? CreatedByUserId { get; set; }

    [ForeignKey(nameof(CreatedByUserId))]
    public User? CreatedByUser { get; set; }

    [MaxLength(64)]
    public string? RequestIp { get; set; }

    [MaxLength(512)]
    public string? UserAgent { get; set; }

    public bool IsConsumed => UsedAtUtc != null || RevokedAtUtc != null;
    public bool IsExpired => DateTime.UtcNow > ExpiresAtUtc;
    public bool IsValid => !IsConsumed && !IsExpired;
}
