using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ISM.Domain.Entities;
using ISM.Domain.Modules.System.Enums;

namespace ISM.Domain.Modules.System.Entities;

public sealed class SystemAlert : BaseEntity
{
    [Key]
    public new int Id { get; set; }

    public int RestaurantId { get; set; }
    [ForeignKey(nameof(RestaurantId))]
    public Restaurant? Restaurant { get; set; }

    public SystemAlertType AlertType { get; set; }

    public AlertSeverity Severity { get; set; }

    [Required]
    [MaxLength(150)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(400)]
    public string Message { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? ReferenceEntityType { get; set; }

    public int? ReferenceEntityId { get; set; }

    [MaxLength(4000)]
    public string? PayloadSerializedJson { get; set; }

    public bool IsRead { get; set; }
    public bool IsDismissed { get; set; }

    [Column(TypeName = "datetime(6)")]
    public DateTime GeneratedAtUtc { get; set; }

    [Column(TypeName = "datetime(6)")]
    public DateTime? ReadAtUtc { get; set; }

    [Column(TypeName = "datetime(6)")]
    public DateTime? DismissedAtUtc { get; set; }

    public int? DismissedByUserId { get; set; }
    [ForeignKey(nameof(DismissedByUserId))]
    public User? DismissedByUser { get; set; }
}
