using ISM.Domain.Entities;

namespace ISM.Domain.Modules.Assistant.Entities;

public sealed class AssistantThread : BaseEntity
{
    public int RestaurantId { get; set; }
    public int UserId { get; set; }
    public int? ParentThreadId { get; set; }
    public int? OriginAlertId { get; set; }
    public string OriginType { get; set; } = "General";
    public int? SubjectEntityId { get; set; }
    public string Subject { get; set; } = "Conversa com o ISM Assist";
    public string ContextSnapshotJson { get; set; } = "{}";
    public DateTime LastMessageAtUtc { get; set; }
    public bool IsArchived { get; set; }
}
