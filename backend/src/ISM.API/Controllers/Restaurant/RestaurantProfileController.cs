using ISM.Application.DTOs;
using ISM.Application.Interfaces;
using ISM.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ISM.API.Controllers.Restaurant;

[ApiController]
[Authorize]
public sealed class RestaurantProfileController : ControllerBase
{
    private readonly IRestaurantService _service;
    private readonly ICurrentUser _currentUser;

    public RestaurantProfileController(
        IRestaurantService service,
        ICurrentUser currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    [HttpGet]
    [Route("api/me/restaurant/profile")]
    [Authorize(Policy = IsmPolicies.RestaurantManagerOrAbove)]
    public async Task<ActionResult<RestaurantDto>> GetMyProfile(CancellationToken ct)
    {
        if (!_currentUser.RestaurantId.HasValue)
            return BadRequest("Usuário não pertence a um restaurante.");

        var dto = await _service.GetRestaurantByIdAsync(_currentUser.RestaurantId.Value, ct);
        if (dto == null)
            return NotFound("Restaurante não encontrado.");

        return Ok(dto);
    }

    [HttpPut]
    [Route("api/me/restaurant/profile")]
    [Authorize(Policy = IsmPolicies.RestaurantManagerOrAbove)]
    public async Task<ActionResult> UpdateMyProfile(
        [FromBody] RestaurantDto dto,
        CancellationToken ct)
    {
        if (!_currentUser.RestaurantId.HasValue)
            return BadRequest("Usuário não pertence a um restaurante.");

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        dto.Id = _currentUser.RestaurantId.Value;
        var updated = await _service.UpdateAsync(_currentUser.RestaurantId.Value, dto, ct);
        if (!updated)
            return NotFound("Restaurante não encontrado.");

        return NoContent();
    }

    [HttpGet]
    [Route("api/restaurants/{restaurantId:int}/profile")]
    [Authorize(Policy = IsmPolicies.SuperAdminOnly)]
    public async Task<ActionResult<RestaurantDto>> GetByRestaurant(
        [FromRoute] int restaurantId,
        CancellationToken ct)
    {
        var dto = await _service.GetRestaurantByIdAsync(restaurantId, ct);
        if (dto == null)
            return NotFound("Restaurante não encontrado.");

        return Ok(dto);
    }

    [HttpPut]
    [Route("api/restaurants/{restaurantId:int}/profile")]
    [Authorize(Policy = IsmPolicies.SuperAdminOnly)]
    public async Task<ActionResult> UpdateByRestaurant(
        [FromRoute] int restaurantId,
        [FromBody] RestaurantDto dto,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        dto.Id = restaurantId;
        var updated = await _service.UpdateAsync(restaurantId, dto, ct);
        if (!updated)
            return NotFound("Restaurante não encontrado.");

        return NoContent();
    }
}
