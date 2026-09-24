using ISM.Application.DTOs;
using ISM.Domain.Modules.System.Enums;

namespace ISM.Application.Interfaces.System;

public interface ISystemAlertService
{
    Task<PagedResult<SystemAlertResponse>> GetPagedFilteredAsync(
        int pageNumber = 1,
        int pageSize = 10,
        SystemAlertType? alertType = null,
        AlertSeverity? severity = null,
        bool? isRead = null,
        bool? isDismissed = null,
        string? search = null,
        CancellationToken cancellationToken = default);

    Task<SystemAlertResponse?> MarkReadAsync(int alertId, CancellationToken cancellationToken = default);

    Task<SystemAlertResponse?> DismissAsync(int alertId, CancellationToken cancellationToken = default);
}
