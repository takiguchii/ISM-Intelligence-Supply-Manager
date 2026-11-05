using ISM.Domain.Interfaces;
using ISM.Domain.Entities;
using ISM.Application.Services.Notifications;
using ISM.Domain.Modules.System.Entities;
using ISM.Domain.Modules.System.Enums;
using ISM.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace ISM.Infrastructure.Repositories.System;

public sealed class SystemAlertRepository : ISystemAlertRepository
{
    private readonly IsmDbContext _dbContext;

    public SystemAlertRepository(IsmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<SystemAlert?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => await _dbContext.Set<SystemAlert>()
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<SystemAlert> Items, int TotalCount)> GetPagedAsync(
        int? restaurantId,
        int pageNumber,
        int pageSize,
        SystemAlertType? alertType = null,
        AlertSeverity? severity = null,
        bool? isRead = null,
        bool? isDismissed = null,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Set<SystemAlert>()
            .AsNoTracking();

        if (restaurantId.HasValue && restaurantId.Value > 0)
            query = query.Where(a => a.RestaurantId == restaurantId.Value);

        if (alertType.HasValue)
            query = query.Where(a => a.AlertType == alertType.Value);

        if (severity.HasValue)
            query = query.Where(a => a.Severity == severity.Value);

        if (isRead.HasValue)
            query = query.Where(a => a.IsRead == isRead.Value);

        if (isDismissed.HasValue)
            query = query.Where(a => a.IsDismissed == isDismissed.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(a =>
                a.Title.ToLower().Contains(term) ||
                a.Message.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(a => a.GeneratedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<SystemAlert> CreateAsync(SystemAlert alert, CancellationToken cancellationToken = default)
    {
        _dbContext.Set<SystemAlert>().Add(alert);
        var recipients = await _dbContext.Users.IgnoreQueryFilters()
            .Where(u => u.IsActive && u.RestaurantId == alert.RestaurantId &&
                (u.Role == "Admin" || u.Role == "Manager") &&
                (u.EmailNotificationsEnabled || u.WhatsAppNotificationsEnabled))
            .ToListAsync(cancellationToken);
        await _dbContext.Restaurants.FirstOrDefaultAsync(r => r.Id == alert.RestaurantId, cancellationToken);
        var now = DateTime.UtcNow;
        foreach (var user in recipients.Where(u => NotificationPolicy.CanReceive(u, alert)))
        {
            void Queue(string channel, string recipient)
            {
                var restaurant = _dbContext.Restaurants.Local.FirstOrDefault(r => r.Id == alert.RestaurantId);
                var message = NotificationMessageFormatter.Render(NotificationMessageFormatter.ScenarioFor(alert.AlertType), channel,
                    restaurant?.Name ?? "Seu restaurante", restaurant?.TimeZone ?? "America/Sao_Paulo",
                    alert.GeneratedAtUtc, NotificationLinkService.PublicAppUrl, false, alert);
                _dbContext.Set<NotificationDelivery>().Add(new()
                {
                    Alert = alert, UserId = user.Id, RestaurantId = alert.RestaurantId,
                    Channel = channel, Recipient = recipient, Subject = message.Subject, Body = message.Text,
                    HtmlBody = message.Html, Scenario = message.Scenario,
                    CreatedAtUtc = now, NextAttemptAtUtc = now
                });
            }
            if (user.EmailNotificationsEnabled)
                Queue("email", user.NotificationEmailAddress ?? user.Email);
            if (user.WhatsAppNotificationsEnabled && !string.IsNullOrWhiteSpace(user.WhatsAppPhone))
                Queue("whatsapp", user.WhatsAppPhone);
        }
        await _dbContext.SaveChangesAsync(cancellationToken);
        return alert;
    }

    public async Task<SystemAlert?> UpdateAsync(SystemAlert alert, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.Set<SystemAlert>().FirstOrDefaultAsync(a => a.Id == alert.Id, cancellationToken);
        if (existing is null) return null;

        existing.IsRead = alert.IsRead;
        existing.IsDismissed = alert.IsDismissed;
        existing.ReadAtUtc = alert.ReadAtUtc;
        existing.DismissedAtUtc = alert.DismissedAtUtc;
        existing.DismissedByUserId = alert.DismissedByUserId;
        existing.UpdatedAtUtc = DateTime.UtcNow;

        _dbContext.Set<SystemAlert>().Update(existing);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public async Task<bool> ExistsSameTypeNonDismissedWithinAsync(
        int restaurantId,
        SystemAlertType alertType,
        string? referenceEntityType,
        int? referenceEntityId,
        TimeSpan withinWindow,
        string? deduplicationKey = null,
        CancellationToken cancellationToken = default)
    {
        var threshold = DateTime.UtcNow.Subtract(withinWindow);

        var query = _dbContext.Set<SystemAlert>()
            .IgnoreQueryFilters()
            .Where(a =>
                a.RestaurantId == restaurantId &&
                a.AlertType == alertType &&
                !a.IsDismissed &&
                a.GeneratedAtUtc >= threshold);

        if (!string.IsNullOrWhiteSpace(referenceEntityType))
            query = query.Where(a => a.ReferenceEntityType == referenceEntityType);

        if (referenceEntityId.HasValue)
            query = query.Where(a => a.ReferenceEntityId == referenceEntityId.Value);

        if (!string.IsNullOrWhiteSpace(deduplicationKey))
            query = query.Where(a => a.PayloadSerializedJson != null && a.PayloadSerializedJson.Contains(deduplicationKey));

        return await query.AnyAsync(cancellationToken);
    }
}
