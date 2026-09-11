using ISM.Application.DTOs;
using ISM.Application.Interfaces.System;
using ISM.Application.Security;
using ISM.Domain.Interfaces;
using ISM.Domain.Modules.System.Entities;
using ISM.Domain.Modules.System.Enums;

namespace ISM.Application.Services.System;

public sealed class SystemAlertService : ISystemAlertService
{
    private readonly ISystemAlertRepository _systemAlertRepository;
    private readonly ICurrentUser _currentUser;

    public SystemAlertService(
        ISystemAlertRepository systemAlertRepository,
        ICurrentUser currentUser)
    {
        _systemAlertRepository = systemAlertRepository;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<SystemAlertResponse>> GetPagedFilteredAsync(
        int pageNumber = 1,
        int pageSize = 10,
        SystemAlertType? alertType = null,
        AlertSeverity? severity = null,
        bool? isRead = null,
        bool? isDismissed = null,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 200) pageSize = 200;

        int? restaurantFilter = null;
        if (!_currentUser.IsSuperAdmin)
        {
            restaurantFilter = _currentUser.RestaurantId;
            if (!restaurantFilter.HasValue)
            {
                return new PagedResult<SystemAlertResponse>(
                    Array.Empty<SystemAlertResponse>(),
                    pageNumber, pageSize, 0, 0, false, false);
            }
        }

        var (items, totalCount) = await _systemAlertRepository.GetPagedAsync(
            restaurantFilter,
            pageNumber,
            pageSize,
            alertType,
            severity,
            isRead,
            isDismissed,
            search,
            cancellationToken);

        int totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);
        var responses = items.Select(Map).ToList();

        return new PagedResult<SystemAlertResponse>(
            responses,
            pageNumber,
            pageSize,
            totalCount,
            totalPages,
            pageNumber > 1,
            pageNumber < totalPages);
    }

    public async Task<SystemAlertResponse?> MarkReadAsync(int alertId, CancellationToken cancellationToken = default)
    {
        var alert = await _systemAlertRepository.GetByIdAsync(alertId, cancellationToken);
        if (alert is null) return null;

        if (!_currentUser.IsSuperAdmin &&
            _currentUser.RestaurantId.HasValue &&
            alert.RestaurantId != _currentUser.RestaurantId.Value)
        {
            return null;
        }

        alert.IsRead = true;
        alert.ReadAtUtc = DateTime.UtcNow;

        var updated = await _systemAlertRepository.UpdateAsync(alert, cancellationToken);
        return updated is null ? null : Map(updated);
    }

    public async Task<SystemAlertResponse?> DismissAsync(int alertId, CancellationToken cancellationToken = default)
    {
        var alert = await _systemAlertRepository.GetByIdAsync(alertId, cancellationToken);
        if (alert is null) return null;

        if (!_currentUser.IsSuperAdmin &&
            _currentUser.RestaurantId.HasValue &&
            alert.RestaurantId != _currentUser.RestaurantId.Value)
        {
            return null;
        }

        alert.IsDismissed = true;
        alert.DismissedAtUtc = DateTime.UtcNow;
        alert.DismissedByUserId = _currentUser.UserId;

        var updated = await _systemAlertRepository.UpdateAsync(alert, cancellationToken);
        return updated is null ? null : Map(updated);
    }

    private static SystemAlertResponse Map(SystemAlert a)
        => new(
            a.Id,
            a.RestaurantId,
            a.AlertType,
            a.Severity,
            a.Title,
            a.Message,
            a.ReferenceEntityType,
            a.ReferenceEntityId,
            a.PayloadSerializedJson,
            a.IsRead,
            a.IsDismissed,
            a.GeneratedAtUtc,
            a.ReadAtUtc,
            a.DismissedAtUtc,
            a.DismissedByUserId,
            a.CreatedAtUtc,
            a.UpdatedAtUtc);
}
