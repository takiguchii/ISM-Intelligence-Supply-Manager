using ISM.Application.Interfaces.Notifications;
using ISM.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ISM.API.Controllers.Users;

// A sessão é compartilhada pelo servidor. Somente Admin pode conectá-la.
[ApiController, Authorize(Policy = IsmPolicies.SuperAdminOnly), Route("api/notifications/whatsapp")]
public sealed class WhatsAppConnectionController(IWhatsAppConnection connection,
    INotificationDeliveryService notifications) : ControllerBase
{
    [HttpGet("status")]
    public async Task<IActionResult> Status(CancellationToken ct)
    {
        if (!notifications.GetChannelsStatus().WhatsAppConfigured) return Ok(new { status = "NOT_CONFIGURED" });
        try { return Ok(new { status = await connection.GetStatusAsync(ct) }); }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
        { return StatusCode(503, new { message = "Não foi possível acessar o serviço de WhatsApp." }); }
    }

    [HttpPost("connect")]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("auth")]
    public async Task<IActionResult> Connect(CancellationToken ct)
    {
        if (!notifications.GetChannelsStatus().WhatsAppConfigured)
            return BadRequest(new { message = "Configure o serviço de WhatsApp no servidor primeiro." });
        try { await connection.StartAsync(ct); return Accepted(new { message = "Conexão iniciada. Atualize o QR Code em alguns segundos." }); }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
        { return StatusCode(503, new { message = "Não foi possível iniciar a conexão do WhatsApp." }); }
    }

    [HttpGet("qr")]
    public async Task<IActionResult> Qr(CancellationToken ct)
    {
        Response.Headers.CacheControl = "no-store";
        try { return File(await connection.GetQrAsync(ct), "image/png"); }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
        { return BadRequest(new { message = "QR Code indisponível. Confira o status e tente novamente." }); }
    }
}
