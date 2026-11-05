using ISM.Application.DTOs;
using ISM.Application.Security;
using ISM.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ISM.API.Controllers.Users;

[ApiController, Authorize, Route("api/me/notification-preferences")]
public sealed class NotificationPreferencesController(IUserRepository users, ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var user = await users.GetByIdAsync(currentUser.UserId, ct);
        return user == null ? NotFound() : Ok(new NotificationPreferences(user.NotifyStock, user.NotifySuppliers, user.NotifyMenu,
            user.EmailNotificationsEnabled, user.WhatsAppNotificationsEnabled,
            user.NotificationEmailAddress ?? user.Email, user.WhatsAppPhone));
    }

    [HttpPut]
    public async Task<IActionResult> Update(NotificationPreferences preferences, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(currentUser.UserId, ct);
        if (user == null) return NotFound();
        if (preferences.WhatsAppEnabled && string.IsNullOrWhiteSpace(preferences.WhatsAppPhone))
            return BadRequest(new { message = "Informe o número de WhatsApp com país e DDD." });
        user.EmailNotificationsEnabled = preferences.EmailEnabled;
        user.WhatsAppNotificationsEnabled = preferences.WhatsAppEnabled;
        user.NotificationEmailAddress = string.IsNullOrWhiteSpace(preferences.EmailAddress) ? null : preferences.EmailAddress.Trim();
        user.WhatsAppPhone = string.IsNullOrWhiteSpace(preferences.WhatsAppPhone) ? null : preferences.WhatsAppPhone.Trim();
        user.NotifyStock = preferences.Stock;
        user.NotifySuppliers = preferences.Suppliers;
        user.NotifyMenu = preferences.Menu;
        users.Update(user);
        await users.SaveChangesAsync(ct);
        return NoContent();
    }
}
