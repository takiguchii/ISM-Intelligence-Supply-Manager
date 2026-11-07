using ISM.Domain.Entities;
using ISM.Application.DTOs.Notifications;

namespace ISM.Application.Interfaces.Notifications;

public sealed record NotificationDeliveryView(long Id, string Channel, string Status, string Subject,
    int Attempts, DateTime CreatedAtUtc, DateTime? SentAtUtc, string? LastError, bool IsTest = false, string? Scenario = null, int? SourceAlertId = null);
public sealed record NotificationChannelsStatus(bool EmailConfigured, bool WhatsAppConfigured);

public interface INotificationDeliveryService
{
    NotificationChannelsStatus GetChannelsStatus();
    Task<NotificationScenarioCatalog> GetScenariosAsync(int userId, CancellationToken ct = default);
    Task<NotificationPreview> PreviewAsync(int userId, string channel, string scenario, int? alertId, CancellationToken ct = default);
    Task<long> QueueScenarioAsync(int userId, string channel, string scenario, int? alertId, CancellationToken ct = default);
    Task<long> QueueTestAsync(int userId, string channel, CancellationToken ct = default);
    Task<IReadOnlyList<NotificationDeliveryView>> GetHistoryAsync(int userId, CancellationToken ct = default);
    Task ProcessPendingAsync(CancellationToken ct = default);
}

public interface INotificationTransport
{
    Task SendEmailAsync(NotificationDelivery delivery, CancellationToken ct);
    Task SendWhatsAppAsync(NotificationDelivery delivery, CancellationToken ct);
}

public interface IWhatsAppConnection
{
    Task<string> GetStatusAsync(CancellationToken ct);
    Task StartAsync(CancellationToken ct);
    Task<byte[]> GetQrAsync(CancellationToken ct);
}
