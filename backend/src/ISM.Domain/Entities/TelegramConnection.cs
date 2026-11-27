namespace ISM.Domain.Entities;

public sealed class TelegramConnection
{
    public int UserId { get; set; }
    public int RestaurantId { get; set; }
    public long? ExpectedUserId { get; set; }
    public long? ChatId { get; set; }
    public string? CodeHash { get; set; }
    public DateTime? CodeExpiresAtUtc { get; set; }
    public bool AlertsEnabled { get; set; }
    public int? ThreadId { get; set; }
    public DateTime? LastMessageAtUtc { get; set; }
    public long LastUpdateId { get; set; }
    public int Version { get; set; }
}

public sealed class TelegramPollingCursor
{
    public string BotUsername { get; set; } = "";
    public long Offset { get; set; }
}
