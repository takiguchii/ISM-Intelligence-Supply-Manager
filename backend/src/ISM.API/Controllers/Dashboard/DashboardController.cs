using ISM.Application.DTOs.Dashboard;
using ISM.Application.Interfaces.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ISM.API.Controllers.Dashboard;

[ApiController]
[Route("api/dashboard")]
[Authorize(Roles = "Admin,Manager")]
public sealed class DashboardController(IDashboardService dashboard) : ControllerBase
{
    [HttpGet("metrics")]
    public async Task<ActionResult<DashboardResponse>> Metrics([FromQuery] int? restaurantId, CancellationToken cancellationToken) => Ok(await dashboard.GetAsync(restaurantId, cancellationToken));
}
