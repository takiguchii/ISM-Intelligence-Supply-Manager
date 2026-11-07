using System.Text.Json;
using ISM.Application.DTOs;
using ISM.Application.Security;
using ISM.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ISM.API.Controllers.Users;

[ApiController, Authorize, Route("api/me/appearance")]
public sealed class AppearancePreferencesController(IUserRepository users, ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var user = await users.GetByIdAsync(currentUser.UserId, ct);
        if (user == null) return NotFound();
        return Ok(JsonSerializer.Deserialize<AppearancePreferences>(user.AppearancePreferencesJson) ?? new());
    }

    [HttpPut]
    public async Task<IActionResult> Update(AppearancePreferences preferences, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(currentUser.UserId, ct);
        if (user == null) return NotFound();
        user.AppearancePreferencesJson = JsonSerializer.Serialize(preferences);
        users.Update(user);
        await users.SaveChangesAsync(ct);
        return NoContent();
    }
}
