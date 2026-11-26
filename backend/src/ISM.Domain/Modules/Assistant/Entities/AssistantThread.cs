using ISM.Domain.Entities;
using ISM.Domain.Modules.Assistant;

namespace ISM.Domain.Modules.Assistant.Entities;

public sealed class AssistantThread : BaseEntity
{
    public int RestaurantId { get; set; }
    public int UserId { get; set; }
    public int? ParentThreadId { get; set; }
    public int? OriginAlertId { get; set; }
    public string OriginType { get; set; } = "General";
    public string SpecialistId { get; set; } = AssistantSpecialists.General;
    public string? SourceView { get; set; }
    public string? SubjectEntityType { get; set; }
    public int? SubjectEntityId { get; set; }
    public string Subject { get; set; } = "Conversa com o ISM Assist";
    public string ContextSnapshotJson { get; set; } = "{}";
    public DateTime LastMessageAtUtc { get; set; }
    public bool IsArchived { get; set; }
}
