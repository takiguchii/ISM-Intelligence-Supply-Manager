using ISM.Application.DTOs;
using ISM.Application.Interfaces;
using ISM.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ISM.API.Controllers;

[ApiController]
[Authorize]
[Route("api/tmp-authorizations")]
public sealed class TmpAuthorizationController : ControllerBase
{
    private readonly ITmpAuthorizationService _service;
    private readonly ICurrentUser _currentUser;

    public TmpAuthorizationController(ITmpAuthorizationService service, ICurrentUser currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    [HttpGet]
    [Authorize(Policy = "RestaurantManagerOrAbove")]
    public async Task<ActionResult<IReadOnlyList<TmpAuthorizationDto>>> ListByRestaurant(CancellationToken ct)
    {
        if (_currentUser.IsSuperAdmin || !_currentUser.RestaurantId.HasValue)
            return BadRequest("Usuário não pertence a um restaurante.");
        var list = await _service.ListByRestaurantAsync(_currentUser.RestaurantId.Value, ct);
        return Ok(list);
    }

    [HttpPost]
    [Authorize(Policy = "RestaurantManagerOrAbove")]
    public async Task<ActionResult<CreateTmpAuthorizationResult>> Create(
        [FromBody] CreateTmpAuthorizationRequest request,
        CancellationToken ct)
    {
        if (_currentUser.IsSuperAdmin || !_currentUser.RestaurantId.HasValue || _currentUser.UserId <= 0)
            return BadRequest("Usuário inválido.");
        var result = await _service.CreateAsync(
            _currentUser.RestaurantId.Value,
            _currentUser.UserId,
            request,
            ct);
        return CreatedAtAction(nameof(ListByRestaurant), result);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "RestaurantManagerOrAbove")]
    public async Task<ActionResult> Revoke([FromRoute] int id, CancellationToken ct)
    {
        if (_currentUser.IsSuperAdmin || !_currentUser.RestaurantId.HasValue || _currentUser.UserId <= 0)
            return BadRequest("Usuário inválido.");
        var ok = await _service.RevokeAsync(id, _currentUser.RestaurantId.Value, _currentUser.UserId, ct);
        if (!ok) return NotFound();
        return NoContent();
    }

    [HttpPost("{id:int}/rotate")]
    [Authorize(Policy = "RestaurantManagerOrAbove")]
    public async Task<ActionResult<TmpAuthorizationDto>> Rotate([FromRoute] int id, CancellationToken ct)
    {
        if (_currentUser.IsSuperAdmin || !_currentUser.RestaurantId.HasValue || _currentUser.UserId <= 0)
            return BadRequest("Usuário inválido.");
        var rotated = await _service.RotateAsync(id, _currentUser.RestaurantId.Value, _currentUser.UserId, ct);
        if (rotated == null) return NotFound();
        return Ok(rotated);
    }

    [HttpPost("validate")]
    [AllowAnonymous]
    public async Task<ActionResult<ValidateTmpAuthorizationResult>> Validate(
        [FromBody] ValidateTokenBody body,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.Token)) return BadRequest("Token vazio.");
        var result = await _service.ValidateTokenAsync(body.Token, body.RequiredScope, ct);
        return result.IsValid ? Ok(result) : Unauthorized(result);
    }

    public sealed record ValidateTokenBody(string Token, string? RequiredScope = null);
}
