using ISM.Domain.Entities;

namespace ISM.Domain.Entities;

public sealed class PlatformModule : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public int SortOrder { get; set; }
}
