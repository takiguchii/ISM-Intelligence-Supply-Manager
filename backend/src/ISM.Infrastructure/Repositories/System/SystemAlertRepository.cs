using ISM.Domain.Interfaces;
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
