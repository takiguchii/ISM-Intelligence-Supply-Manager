using System.ComponentModel.DataAnnotations;
using ISM.Application.Interfaces.Notifications;
using ISM.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ISM.API.Controllers.Users;

[ApiController, Authorize(Policy = IsmPolicies.UserManagement), Route("api/me/notifications")]
public sealed class NotificationDeliveryController(INotificationDeliveryService deliveries, ICurrentUser user) : ControllerBase
{
    [HttpGet("channels")]
    public IActionResult Channels() => Ok(deliveries.GetChannelsStatus());

    [HttpGet("deliveries")]
    public async Task<IActionResult> History(CancellationToken ct) => Ok(await deliveries.GetHistoryAsync(user.UserId, ct));

    [HttpGet("scenarios")]
    public async Task<IActionResult> Scenarios(CancellationToken ct) => Ok(await deliveries.GetScenariosAsync(user.UserId, ct));

    [HttpPost("preview")]
    public async Task<IActionResult> Preview(TestNotificationRequest request, CancellationToken ct)
    {
        try { return Ok(await deliveries.PreviewAsync(user.UserId, request.Channel, request.Scenario, request.AlertId, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("test")]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("auth")]
    public async Task<IActionResult> Test(TestNotificationRequest request, CancellationToken ct)
    {
        try
        {
            var id = await deliveries.QueueScenarioAsync(user.UserId, request.Channel, request.Scenario, request.AlertId, ct);
            return Accepted(new { id, message = "Teste adicionado à fila de envio." });
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }
}

public sealed class TestNotificationRequest
{
    [Required, RegularExpression("^(connectivity|stock-low|stock-risk|supplier-price|supplier-alternative|dish-margin|existing-alert)$")]
    public string Scenario { get; set; } = "connectivity";
    [Range(1, int.MaxValue)]
    public int? AlertId { get; set; }
    [Required, RegularExpression("^(email|whatsapp)$")]
    public string Channel { get; set; } = string.Empty;
}
