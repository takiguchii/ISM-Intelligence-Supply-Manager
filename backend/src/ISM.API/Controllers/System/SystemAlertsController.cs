using ISM.Application.DTOs;
using ISM.Application.Interfaces.System;
using ISM.Application.Security;
using ISM.Domain.Modules.System.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ISM.API.Controllers.System;

[ApiController]
[Route("api/system/alerts")]
[Authorize(Policy = IsmPolicies.RestaurantAnyUser)]
public sealed class SystemAlertsController : ControllerBase
{
    private readonly ISystemAlertService _alertService;

    public SystemAlertsController(ISystemAlertService alertService)
    {
        _alertService = alertService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<SystemAlertResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<SystemAlertResponse>>> GetPaged(
        [FromQuery] int? pageNumber = null,
        [FromQuery] int? page = null,
        [FromQuery] int? pageSize = null,
        [FromQuery] int? limit = null,
        [FromQuery] SystemAlertType? alertType = null,
        [FromQuery] AlertSeverity? severity = null,
        [FromQuery] bool? isRead = null,
        [FromQuery] bool? isDismissed = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        int finalPageNumber = pageNumber ?? page ?? 1;
        int finalPageSize = pageSize ?? limit ?? 10;

        var result = await _alertService.GetPagedFilteredAsync(
            finalPageNumber,
            finalPageSize,
            alertType,
            severity,
            isRead,
            isDismissed,
            search,
            cancellationToken);

        return Ok(result);
    }

    [HttpPatch("{id:int}/mark-read")]
    [ProducesResponseType(typeof(SystemAlertResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SystemAlertResponse>> MarkRead(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _alertService.MarkReadAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPatch("{id:int}/dismiss")]
    [Authorize(Policy = IsmPolicies.RestaurantManagerOrAbove)]
    [ProducesResponseType(typeof(SystemAlertResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SystemAlertResponse>> Dismiss(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _alertService.DismissAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
