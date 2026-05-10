using ISM.Application.Abstractions;
using ISM.Application.Modules.System.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace ISM.API.Controllers;

[ApiController]
[Route("api/system/status")]
public sealed class SystemStatusController : ControllerBase
{
    private readonly ISystemStatusService _systemStatusService;

    public SystemStatusController(ISystemStatusService systemStatusService)
    {
        _systemStatusService = systemStatusService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(SystemStatusResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<SystemStatusResponse>> Get(CancellationToken cancellationToken)
    {
        var response = await _systemStatusService.GetCurrentStatusAsync(cancellationToken);
        return Ok(response);
    }
}
