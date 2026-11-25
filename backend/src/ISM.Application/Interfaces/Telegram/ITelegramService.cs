namespace ISM.Application.Interfaces.Telegram;

public sealed class TelegramOptions
{
    public bool Enabled { get; set; }
    public string BotToken { get; set; } = "";
    public string BotUsername { get; set; } = "";
    public long? BotId => long.TryParse(BotToken.Split(':')[0], out var id) ? id : null;
    public bool IsConfigured => Enabled && !string.IsNullOrWhiteSpace(BotToken) &&
        global::System.Text.RegularExpressions.Regex.IsMatch(BotUsername, "^[A-Za-z0-9_]{5,32}$");
}

public sealed record TelegramIncoming(long UpdateId, long ChatId, long SenderId, string ChatType, string Text);
public sealed record TelegramStatus(bool Configured, bool Connected, bool AlertsEnabled, string? BotUsername, long? BotId = null, long? TelegramUserId = null);
public sealed record TelegramLink(string Url, DateTime ExpiresAtUtc);

public interface ITelegramTransport
{
    Task<IReadOnlyList<TelegramIncoming>> GetUpdatesAsync(long offset, CancellationToken ct);
    Task SendAsync(long chatId, string text, CancellationToken ct);
    Task ConfigureAsync(CancellationToken ct);
}

public interface ITelegramService
{
    Task<TelegramStatus> StatusAsync(CancellationToken ct);
    Task<TelegramLink> CreateLinkAsync(CancellationToken ct, long? botId = null, long? telegramUserId = null);
    Task DisconnectAsync(CancellationToken ct);
    Task SetAlertsAsync(bool enabled, CancellationToken ct);
    Task<string?> HandleAsync(TelegramIncoming message, CancellationToken ct);
}
