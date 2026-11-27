using ISM.Application.Interfaces.Telegram;
using ISM.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ISM.API.Controllers.Users;

[ApiController, Route("api/admin/telegram/restaurants/{restaurantId:int}"), Authorize(Policy = IsmPolicies.SuperAdminOnly)]
public sealed class AdminTelegramController(ITelegramService telegram) : ControllerBase
{
    [HttpGet]
    public Task<TelegramStatus> Status(int restaurantId, CancellationToken ct)
        => telegram.StatusAsync(ct, restaurantId);

    [HttpPost("link"), Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("auth")]
    public async Task<TelegramLink> Link(int restaurantId, TelegramController.IdentityRequest request, CancellationToken ct)
    {
        Response.Headers.CacheControl = "no-store";
        return await telegram.CreateLinkAsync(ct, request.BotId, request.TelegramUserId, restaurantId);
    }

    [HttpDelete]
    public async Task<IActionResult> Disconnect(int restaurantId, CancellationToken ct)
    {
        await telegram.DisconnectAsync(ct, restaurantId);
        return NoContent();
    }

    [HttpPut("alerts")]
    public async Task<IActionResult> Alerts(int restaurantId, TelegramController.AlertPreference preference, CancellationToken ct)
    {
        await telegram.SetAlertsAsync(preference.Enabled, ct, restaurantId);
        return NoContent();
    }
}
