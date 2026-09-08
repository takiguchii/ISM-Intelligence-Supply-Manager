using ISM.Application.DTOs.Restaurant;
using ISM.Application.Interfaces.Restaurant;
using ISM.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ISM.API.Controllers.Restaurant;

[ApiController]
[Authorize]
public sealed class RestaurantAiConfigController : ControllerBase
{
    private readonly IRestaurantAiConfigService _service;
    private readonly ICurrentUser _currentUser;

    public RestaurantAiConfigController(
        IRestaurantAiConfigService service,
        ICurrentUser currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    [HttpGet]
    [Route("api/me/restaurant/ai-config")]
    [Authorize(Policy = IsmPolicies.RestaurantManagerOrAbove)]
    public async Task<ActionResult<RestaurantAiPhotoConfigDto>> GetMyConfig(CancellationToken ct)
    {
        if (!_currentUser.RestaurantId.HasValue)
            return BadRequest("Usuário não pertence a um restaurante.");

        var dto = await _service.GetMyRestaurantAsync(_currentUser.RestaurantId.Value, ct);
        return Ok(dto);
    }

    [HttpPut]
    [Route("api/me/restaurant/ai-config")]
    [Authorize(Policy = IsmPolicies.RestaurantManagerOrAbove)]
    public async Task<ActionResult> UpdateMyConfig(
        [FromBody] UpdateRestaurantAiPhotoConfigDto dto,
        CancellationToken ct)
    {
        if (!_currentUser.RestaurantId.HasValue)
            return BadRequest("Usuário não pertence a um restaurante.");

        try
        {
            await _service.UpdateMyRestaurantAsync(_currentUser.RestaurantId.Value, dto, ct);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }

        return NoContent();
    }

    [HttpPost]
    [Route("api/me/restaurant/ai-config/test")]
    [Authorize(Policy = IsmPolicies.RestaurantManagerOrAbove)]
    public async Task<ActionResult<TestRestaurantAiPhotoConfigResultDto>> TestMyConfig(
        [FromBody] TestRestaurantAiPhotoConfigRequestDto? req,
        CancellationToken ct)
    {
        if (!_currentUser.RestaurantId.HasValue)
            return BadRequest("Usuário não pertence a um restaurante.");

        var result = await _service.TestMyRestaurantAsync(_currentUser.RestaurantId.Value, req, ct);
        return Ok(result);
    }

    [HttpGet]
    [Route("api/restaurants/{restaurantId:int}/ai-config")]
    [Authorize(Policy = IsmPolicies.SuperAdminOnly)]
    public async Task<ActionResult<RestaurantAiPhotoConfigDto>> GetByRestaurant(
        [FromRoute] int restaurantId,
        CancellationToken ct)
    {
        try
        {
            var dto = await _service.GetByRestaurantIdAsync(restaurantId, ct);
            return Ok(dto);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPut]
    [Route("api/restaurants/{restaurantId:int}/ai-config")]
    [Authorize(Policy = IsmPolicies.SuperAdminOnly)]
    public async Task<ActionResult> UpdateByRestaurant(
        [FromRoute] int restaurantId,
        [FromBody] UpdateRestaurantAiPhotoConfigDto dto,
        CancellationToken ct)
    {
        try
        {
            await _service.UpdateByRestaurantIdAsync(restaurantId, dto, ct);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Restaurante") && ex.Message.Contains("não encontrado"))
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }

        return NoContent();
    }

    [HttpPost]
    [Route("api/restaurants/{restaurantId:int}/ai-config/test")]
    [Authorize(Policy = IsmPolicies.SuperAdminOnly)]
    public async Task<ActionResult<TestRestaurantAiPhotoConfigResultDto>> TestByRestaurant(
        [FromRoute] int restaurantId,
        [FromBody] TestRestaurantAiPhotoConfigRequestDto? req,
        CancellationToken ct)
    {
        var result = await _service.TestByRestaurantIdAsync(restaurantId, req, ct);
        return Ok(result);
    }
}
