using System.ComponentModel.DataAnnotations;
using ISM.Application.Interfaces.Telegram;
using ISM.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ISM.API.Controllers.Users;

[ApiController, Route("api/me/telegram"), Authorize(Policy = IsmPolicies.RestaurantManagerOrAbove)]
public sealed class TelegramController(ITelegramService telegram) : ControllerBase
{
    [HttpGet]
    public Task<TelegramStatus> Status(CancellationToken ct) => telegram.StatusAsync(ct);

    public sealed record SaveIdentityRequest([Required, Range(1, long.MaxValue)] long TelegramUserId);

    [HttpPost("save")]
    public async Task<TelegramStatus> Save(SaveIdentityRequest request, CancellationToken ct)
        => await telegram.SaveIdentityAsync(request.TelegramUserId, ct);

    [HttpPost("test")]
    public async Task<TestTelegramResult> Test(CancellationToken ct)
        => await telegram.TestConnectionAsync(ct);

    public sealed record IdentityRequest([Required, Range(1, long.MaxValue)] long? BotId, [Required, Range(1, long.MaxValue)] long? TelegramUserId);

    [HttpPost("link"), Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("auth")]
    public async Task<TelegramLink> Link(IdentityRequest request, CancellationToken ct)
    {
        Response.Headers.CacheControl = "no-store";
        return await telegram.CreateLinkAsync(ct, request.BotId, request.TelegramUserId);
    }

    [HttpDelete]
    public async Task<IActionResult> Disconnect(CancellationToken ct)
    {
        await telegram.DisconnectAsync(ct);
        return NoContent();
    }

    public sealed record AlertPreference(bool Enabled);

    [HttpPut("alerts")]
    public async Task<IActionResult> Alerts(AlertPreference preference, CancellationToken ct)
    {
        await telegram.SetAlertsAsync(preference.Enabled, ct);
        return NoContent();
    }
}
