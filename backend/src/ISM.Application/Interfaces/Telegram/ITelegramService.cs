namespace ISM.Application.Interfaces.Telegram;

public sealed class TelegramOptions
{
    public bool Enabled { get; set; }
    public string BotToken { get; set; } = "";
    public string BotUsername { get; set; } = "";
    public long? BotId { get; set; }
    public string IsmUrl { get; set; } = "";
    public long? ResolvedBotId => BotId ?? (long.TryParse(BotToken.Split(':')[0], out var id) ? id : null);
    public bool IsConfigured => Enabled && !string.IsNullOrWhiteSpace(BotToken) &&
        global::System.Text.RegularExpressions.Regex.IsMatch(BotUsername, "^[A-Za-z0-9_]{5,32}$") &&
        (!BotId.HasValue || BotId == (long.TryParse(BotToken.Split(':')[0], out var tokenId) ? tokenId : null));
}

public sealed record TelegramIncoming(long UpdateId, long ChatId, long SenderId, string ChatType, string Text);
public sealed record TelegramStatus(bool Configured, bool Connected, bool AlertsEnabled, string? BotUsername, long? BotId = null, long? TelegramUserId = null, string? BotUrl = null);
public sealed record TelegramLink(string Url, DateTime ExpiresAtUtc);
public sealed record TestTelegramResult(bool Success, string Message);

public interface ITelegramTransport
{
    Task<IReadOnlyList<TelegramIncoming>> GetUpdatesAsync(long offset, CancellationToken ct);
    Task SendAsync(long chatId, string text, CancellationToken ct);
    Task ConfigureAsync(CancellationToken ct);
}

public interface ITelegramService
{
    Task<TelegramStatus> StatusAsync(CancellationToken ct, int? targetRestaurantId = null);
    Task<TelegramStatus> SaveIdentityAsync(long telegramUserId, CancellationToken ct, int? targetRestaurantId = null);
    Task<TestTelegramResult> TestConnectionAsync(CancellationToken ct, int? targetRestaurantId = null);
    Task<TelegramLink> CreateLinkAsync(CancellationToken ct, long? botId = null, long? telegramUserId = null, int? targetRestaurantId = null);
    Task DisconnectAsync(CancellationToken ct, int? targetRestaurantId = null);
    Task SetAlertsAsync(bool enabled, CancellationToken ct, int? targetRestaurantId = null);
    Task<string?> HandleAsync(TelegramIncoming message, CancellationToken ct);
}
