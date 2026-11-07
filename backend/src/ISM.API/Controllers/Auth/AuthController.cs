using ISM.Application.DTOs;
using ISM.Application.Interfaces;
using ISM.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ISM.API.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("auth")]
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var response = await _authService.LoginAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (TwoFactorRequiredException)
        {
            return BadRequest(new { requiresTwoFactor = true, message = "Informe o código do aplicativo autenticador ou um código de recuperação." });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpGet("two-factor")]
    [Authorize]
    public async Task<IActionResult> TwoFactorStatus(CancellationToken ct) =>
        Ok(new { enabled = await _authService.TwoFactorEnabledAsync(ct) });

    [HttpPost("two-factor/setup")]
    [Authorize]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("auth")]
    public async Task<IActionResult> SetupTwoFactor(TwoFactorPasswordRequest request, CancellationToken ct)
    {
        try { return Ok(await _authService.SetupTwoFactorAsync(request.Password, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("two-factor/enable")]
    [Authorize]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("auth")]
    public async Task<IActionResult> EnableTwoFactor(TwoFactorCodeRequest request, CancellationToken ct)
    {
        try { return Ok(new { recoveryCodes = await _authService.EnableTwoFactorAsync(request.Code, ct) }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("two-factor/disable")]
    [Authorize]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("auth")]
    public async Task<IActionResult> DisableTwoFactor(DisableTwoFactorRequest request, CancellationToken ct)
    {
        try { await _authService.DisableTwoFactorAsync(request.Password, request.Code, ct); return NoContent(); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("auth")]
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        try
        {
            await _authService.ChangePasswordAsync(request, ct);
            return NoContent();
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("sessions")]
    [Authorize]
    public async Task<IActionResult> Sessions(
        [FromServices] ISM.Domain.Interfaces.IAuthSessionRepository sessions, CancellationToken ct)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var currentId = User.FindFirstValue("sid");
        var list = await sessions.ListAsync(userId, ct);
        return Ok(list.Select(s => new { s.Id, s.Device, s.CreatedAtUtc, s.ExpiresAtUtc, IsCurrent = s.Id == currentId }));
    }

    [HttpDelete("sessions/{id}")]
    [Authorize]
    public async Task<IActionResult> RevokeSession(string id,
        [FromServices] ISM.Domain.Interfaces.IAuthSessionRepository sessions, CancellationToken ct)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var session = await sessions.FindAsync(id, ct);
        if (session == null || session.UserId != userId) return NotFound();
        await sessions.RevokeAsync(userId, id, ct);
        return NoContent();
    }

    [HttpPost("register")]
    [Authorize(Policy = IsmPolicies.UserManagement)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var response = await _authService.RegisterAsync(request, cancellationToken);
            return CreatedAtAction(nameof(Register), response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpPost("register-tenant")]
    [Authorize(Policy = IsmPolicies.SuperAdminOnly)]
    [ProducesResponseType(typeof(RegisterTenantResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<RegisterTenantResponse>> RegisterTenant([FromBody] RegisterTenantRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var response = await _authService.RegisterTenantAsync(request, cancellationToken);
            return CreatedAtAction(nameof(RegisterTenant), response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("me")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [Authorize]
    public async Task<ActionResult<UserDto>> Me(CancellationToken cancellationToken)
    {
        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == "sub");
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized();

        var user = await _authService.GetUserByIdAsync(userId, cancellationToken);
        if (user == null)
            return Unauthorized();

        return Ok(user);
    }
}
