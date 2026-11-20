using ISM.Domain.Entities;

namespace ISM.Domain.Modules.Assistant.Entities;

public sealed class AssistantMessage : BaseEntity
{
    public int RestaurantId { get; set; }
    public int ThreadId { get; set; }
    public int? UserId { get; set; }
    public string Role { get; set; } = "assistant";
    public string Content { get; set; } = string.Empty;
    public string? Mode { get; set; }
    public string? Source { get; set; }
}
