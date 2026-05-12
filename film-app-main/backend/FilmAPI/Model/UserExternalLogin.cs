using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FilmAPI.Model;

public class UserExternalLogin
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [Required]
    public ExternalLoginProvider Provider { get; set; }

    [Required]
    [MaxLength(255)]
    public string ProviderUserId { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? ProviderTenantId { get; set; }

    [Required]
    [MaxLength(255)]
    public string EmailAtLogin { get; set; } = string.Empty;

    [Required]
    public DateTime LinkedAtUtc { get; set; }

    public DateTime? LastLoginAtUtc { get; set; }

    public DateTime? RevokedAtUtc { get; set; }

    public bool IsRevoked => RevokedAtUtc != null;
}
