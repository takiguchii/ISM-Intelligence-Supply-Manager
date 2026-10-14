using ISM.Domain.Modules.System.Entities;
using ISM.Domain.Modules.System.Enums;

namespace ISM.Domain.Interfaces;

public interface ISystemAlertRepository
{
    Task<SystemAlert?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<SystemAlert> Items, int TotalCount)> GetPagedAsync(
        int? restaurantId,
        int pageNumber,
        int pageSize,
        SystemAlertType? alertType = null,
        AlertSeverity? severity = null,
        bool? isRead = null,
        bool? isDismissed = null,
        string? search = null,
        CancellationToken cancellationToken = default);
    Task<SystemAlert> CreateAsync(SystemAlert alert, CancellationToken cancellationToken = default);
    Task<SystemAlert?> UpdateAsync(SystemAlert alert, CancellationToken cancellationToken = default);
    Task<bool> ExistsSameTypeNonDismissedWithinAsync(
        int restaurantId,
        SystemAlertType alertType,
        string? referenceEntityType,
        int? referenceEntityId,
        TimeSpan withinWindow,
        string? deduplicationKey = null,
        CancellationToken cancellationToken = default);
}
