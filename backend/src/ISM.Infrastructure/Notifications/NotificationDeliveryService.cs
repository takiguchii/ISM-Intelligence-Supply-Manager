using ISM.Application.DTOs.Notifications;
using ISM.Application.Services.Notifications;
using ISM.Application.Interfaces.Notifications;
using ISM.Domain.Entities;
using ISM.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ISM.Infrastructure.Notifications;

public sealed class NotificationDeliveryService(IsmDbContext db, INotificationTransport transport,
    IOptions<SmtpOptions> smtp, IOptions<WhatsAppOptions> whatsapp,
    ILogger<NotificationDeliveryService> logger) : INotificationDeliveryService
{
    public NotificationChannelsStatus GetChannelsStatus() => new(smtp.Value.IsConfigured, whatsapp.Value.IsConfigured);

    public async Task<NotificationScenarioCatalog> GetScenariosAsync(int userId, CancellationToken ct = default)
    {
        var user = await RequireUserAsync(userId, ct);
        var alerts = await db.SystemAlerts.AsNoTracking().Where(a => a.RestaurantId == user.RestaurantId && !a.IsDismissed)
            .OrderByDescending(a => a.GeneratedAtUtc).Take(20)
            .Select(a => new NotificationAlertOption(a.Id, a.Title, a.AlertType.ToString())).ToListAsync(ct);
        return new(NotificationMessageFormatter.Scenarios, alerts);
    }

    private async Task<User> RequireUserAsync(int userId, CancellationToken ct)
    {
        var user = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new InvalidOperationException("Usuário não encontrado.");
        if (!user.IsActive || !user.RestaurantId.HasValue || user.Role is not ("Admin" or "Manager"))
            throw new InvalidOperationException("Apenas administradores e gerentes do restaurante podem enviar testes.");
        return user;
    }

    public async Task<NotificationPreview> PreviewAsync(int userId, string channel, string scenario, int? alertId, CancellationToken ct = default)
    {
        var user = await RequireUserAsync(userId, ct);
        var restaurant = await db.Restaurants.AsNoTracking().FirstOrDefaultAsync(r => r.Id == user.RestaurantId, ct);
        ISM.Domain.Modules.System.Entities.SystemAlert? alert = null;
        if (scenario == "existing-alert")
        {
            alert = await db.SystemAlerts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == alertId &&
                a.RestaurantId == user.RestaurantId && !a.IsDismissed, ct)
                ?? throw new InvalidOperationException("Escolha um alerta ativo do seu restaurante.");
        }
        else if (alertId.HasValue) throw new InvalidOperationException("O alerta real só pode ser usado no cenário de alerta existente.");
        return NotificationMessageFormatter.Render(scenario, channel, restaurant?.Name ?? "Seu restaurante",
            restaurant?.TimeZone ?? "America/Sao_Paulo", alert?.GeneratedAtUtc ?? DateTime.UtcNow,
            NotificationLinkService.PublicAppUrl, true, alert);
    }

    public Task<long> QueueTestAsync(int userId, string channel, CancellationToken ct = default) =>
        QueueScenarioAsync(userId, channel, "connectivity", null, ct);

    public async Task<long> QueueScenarioAsync(int userId, string channel, string scenario, int? alertId, CancellationToken ct = default)
    {
        var user = await RequireUserAsync(userId, ct);
        var message = await PreviewAsync(userId, channel, scenario, alertId, ct);
        var channels = GetChannelsStatus();
        var recipient = channel switch
        {
            "email" when channels.EmailConfigured && user.EmailNotificationsEnabled => user.NotificationEmailAddress ?? user.Email,
            "whatsapp" when channels.WhatsAppConfigured && user.WhatsAppNotificationsEnabled && user.WhatsAppPhone != null => user.WhatsAppPhone,
            _ => throw new InvalidOperationException("Configure o canal, salve suas preferências e ative o recebimento antes de enviar um teste.")
        };
        if (await db.Set<NotificationDelivery>().AnyAsync(d => d.UserId == userId && d.Channel == channel &&
            d.AlertId == null && d.Scenario == message.Scenario && d.SourceAlertId == alertId && d.CreatedAtUtc > DateTime.UtcNow.AddMinutes(-1), ct))
            throw new InvalidOperationException("Aguarde um minuto antes de repetir o mesmo cenário neste canal.");
        var now = DateTime.UtcNow;
        var delivery = new NotificationDelivery
        {
            UserId = userId, RestaurantId = user.RestaurantId!.Value, Channel = channel, Recipient = recipient,
            Subject = message.Subject, Body = message.Text, HtmlBody = message.Html, IsTest = true,
            Scenario = message.Scenario, SourceAlertId = alertId,
            CreatedAtUtc = now, NextAttemptAtUtc = now
        };
        db.Set<NotificationDelivery>().Add(delivery);
        await db.SaveChangesAsync(ct);
        return delivery.Id;
    }

    public async Task<IReadOnlyList<NotificationDeliveryView>> GetHistoryAsync(int userId, CancellationToken ct = default) =>
        await db.Set<NotificationDelivery>().AsNoTracking().Where(d => d.UserId == userId)
            .OrderByDescending(d => d.CreatedAtUtc).Take(50)
            .Select(d => new NotificationDeliveryView(d.Id, d.Channel, d.Status, d.Subject,
                d.Attempts, d.CreatedAtUtc, d.SentAtUtc, d.LastError, d.IsTest, d.Scenario, d.SourceAlertId)).ToListAsync(ct);

    public async Task ProcessPendingAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var candidates = await db.Set<NotificationDelivery>().AsNoTracking()
            .Where(d => (d.Status == "Pending" && d.NextAttemptAtUtc <= now) ||
                (d.Status == "Sending" && d.LeaseExpiresAtUtc < now))
            .OrderBy(d => d.CreatedAtUtc).Take(20).Select(d => d.Id).ToListAsync(ct);
        foreach (var id in candidates)
        {
            ct.ThrowIfCancellationRequested();
            var delivery = await db.Set<NotificationDelivery>().Include(d => d.Alert).FirstAsync(d => d.Id == id, ct);
            if (delivery.Status is not ("Pending" or "Sending") ||
                (delivery.Status == "Sending" && delivery.LeaseExpiresAtUtc > DateTime.UtcNow)) continue;
            var user = await db.Users.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(u => u.Id == delivery.UserId, ct);
            var enabled = user is { IsActive: true } && user.RestaurantId == delivery.RestaurantId &&
                user.Role is "Admin" or "Manager" &&
                (delivery.Alert == null || (!delivery.Alert.IsDismissed && NotificationPolicy.CanReceive(user, delivery.Alert))) &&
                (delivery.Channel == "email" ? user.EmailNotificationsEnabled && delivery.Recipient == (user.NotificationEmailAddress ?? user.Email)
                    : user.WhatsAppNotificationsEnabled && delivery.Recipient == user.WhatsAppPhone);
            if (!enabled)
            {
                delivery.Status = "Cancelled";
                delivery.LastError = "Envio cancelado após alteração do destinatário ou das preferências.";
                await SaveOrSkipAsync(ct);
                continue;
            }
            var channels = GetChannelsStatus();
            if (delivery.Channel == "email" ? !channels.EmailConfigured : !channels.WhatsAppConfigured)
            {
                delivery.Status = "Pending";
                delivery.LeaseExpiresAtUtc = null;
                delivery.LastError = "Canal ainda não configurado no servidor.";
                delivery.NextAttemptAtUtc = DateTime.UtcNow.AddMinutes(1);
                await SaveOrSkipAsync(ct);
                continue;
            }
            if (delivery.Attempts >= 3)
            {
                delivery.Status = "Failed";
                delivery.LastError = "O envio não foi confirmado após três tentativas.";
                await SaveOrSkipAsync(ct);
                continue;
            }
            delivery.Status = "Sending";
            delivery.Attempts++;
            delivery.LeaseExpiresAtUtc = DateTime.UtcNow.AddMinutes(2);
            if (!await SaveOrSkipAsync(ct)) continue;
            try
            {
                if (delivery.Channel == "email") await transport.SendEmailAsync(delivery, ct);
                else await transport.SendWhatsAppAsync(delivery, ct);
                delivery.Status = "Sent";
                delivery.SentAtUtc = DateTime.UtcNow;
                delivery.LastError = null;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                delivery.Status = delivery.Attempts >= 3 ? "Failed" : "Pending";
                delivery.LastError = delivery.Channel == "email"
                    ? "O servidor SMTP não confirmou o envio. Confira conexão, autenticação e remetente."
                    : "O WhatsApp não confirmou o envio. Confira a conexão do número e o destinatário.";
                delivery.NextAttemptAtUtc = DateTime.UtcNow.AddSeconds(30 * delivery.Attempts);
                logger.LogWarning("Falha na notificação {DeliveryId}, tentativa {Attempt}, tipo {ErrorType}",
                    delivery.Id, delivery.Attempts, ex.GetType().Name);
            }
            delivery.LeaseExpiresAtUtc = null;
            await SaveOrSkipAsync(ct);
        }
    }

    private async Task<bool> SaveOrSkipAsync(CancellationToken ct)
    {
        try { await db.SaveChangesAsync(ct); return true; }
        catch (DbUpdateConcurrencyException) { db.ChangeTracker.Clear(); return false; }
    }
}
