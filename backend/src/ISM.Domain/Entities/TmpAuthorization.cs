using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ISM.Domain.Entities;

public sealed class TmpAuthorization : BaseEntity
{
    [Required]
    public int RestaurantId { get; set; }

    [ForeignKey(nameof(RestaurantId))]
    public Restaurant? Restaurant { get; set; }

    [Required]
    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(512)]
    public string TokenHash { get; set; } = string.Empty;

    [Required]
    [MaxLength(80)]
    public string Scope { get; set; } = string.Empty;

    public int? CreatedByUserId { get; set; }

    [ForeignKey(nameof(CreatedByUserId))]
    public User? CreatedByUser { get; set; }

    public DateTime? AutoRotateAtUtc { get; set; }

    [Required]
    public DateTime ExpiresAtUtc { get; set; }

    public DateTime? RevokedAtUtc { get; set; }

    public int? RevokedByUserId { get; set; }

    [ForeignKey(nameof(RevokedByUserId))]
    public User? RevokedByUser { get; set; }

    public DateTime? LastUsedAtUtc { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    [NotMapped]
    public bool IsActive => RevokedAtUtc == null && ExpiresAtUtc > DateTime.UtcNow;
}
