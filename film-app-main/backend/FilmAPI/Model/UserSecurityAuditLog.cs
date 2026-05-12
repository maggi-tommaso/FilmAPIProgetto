using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FilmAPI.Model;

public class UserSecurityAuditLog
{
    [Key]
    public int Id { get; set; }

    public int? UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    public int? ActorUserId { get; set; }

    [ForeignKey(nameof(ActorUserId))]
    public User? ActorUser { get; set; }

    [Required]
    [MaxLength(80)]
    public string EventType { get; set; } = string.Empty;

    [MaxLength(30)]
    public string? Provider { get; set; }

    [MaxLength(64)]
    public string? IpAddress { get; set; }

    [MaxLength(512)]
    public string? UserAgent { get; set; }

    [MaxLength(4000)]
    public string? MetadataJson { get; set; }

    [Required]
    public DateTime CreatedAtUtc { get; set; }
}
