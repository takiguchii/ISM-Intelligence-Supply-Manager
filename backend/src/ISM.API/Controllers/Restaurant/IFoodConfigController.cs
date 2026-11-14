using ISM.Application.DTOs.Restaurant;
using ISM.Application.Interfaces.Restaurant;
using ISM.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace ISM.API.Controllers.Restaurant;

[ApiController]
[Authorize]

public sealed class IFoodConfigController : ControllerBase
{
    private readonly IIFoodConfigService _service;
    private readonly ICurrentUser _currentUser;

    public IFoodConfigController(IIFoodConfigService service, ICurrentUser currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    [HttpGet]
    [Route("api/me/restaurant/ifood-config")]
    [Authorize(Policy = IsmPolicies.RestaurantManagerOrAbove)]
    public async Task<ActionResult<IFoodConfigDto>> GetMyConfig(CancellationToken ct)
    {
        if (!_currentUser.RestaurantId.HasValue)
            return BadRequest("Usuário não pertence a um restaurante.");
        
        return Ok(await _service.GetMyConfigAsync(_currentUser.RestaurantId.Value, ct));
        
    }

    [HttpPut]
    [Route("api/me/restaurant/ifood-config")]
    [Authorize(Policy = IsmPolicies.RestaurantManagerOrAbove)]
    public async Task<ActionResult> UpdateMyConfig([FromBody] UpdateIFoodConfigDto dto, CancellationToken ct)
    {
        if (! _currentUser.RestaurantId.HasValue)
            return BadRequest("Usuário não pertence a um restaurante");

        try
        {
            await _service.UpdateMyConfigAsync(_currentUser.RestaurantId.Value, dto, ct);
        }
        catch (InvalidOperationException ex) 
        {
            return BadRequest(ex.Message);
        }
        
        return NoContent();
    }

    [HttpPost]
    [Route("api/me/restaurant/ifood-config/test")]
    [Authorize(Policy = IsmPolicies.RestaurantManagerOrAbove)]
    public async Task<ActionResult<TestIFoodConfigResultDto>> TestMyConfig(CancellationToken ct)
    {
        if (! _currentUser.RestaurantId.HasValue)
            return BadRequest("Usuário não pertence a um restaurante");
        
        return Ok(await _service.TestMyConfigAsync(_currentUser.RestaurantId.Value, ct));
    }
}