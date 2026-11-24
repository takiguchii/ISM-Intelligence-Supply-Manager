using ISM.Application.DTOs.Menu;
using ISM.Application.Interfaces.Menu;
using ISM.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ISM.API.Controllers.Menu;

[ApiController]
[Authorize]
[Route("api/menu/ifood-catalog")]


public sealed class IFoodCatalogController : ControllerBase
{
    private readonly IIFoodCatalogSyncService _syncService;
    private readonly ICurrentUser _currentUser;

    public IFoodCatalogController(IIFoodCatalogSyncService syncService, ICurrentUser currentUser)
    {
        _syncService = syncService;
        _currentUser = currentUser;
    }

    [HttpPost("sync")]
    [Authorize(Policy = IsmPolicies.RestaurantManagerOrAbove)]
    public async Task<ActionResult<IFoodCatalogSyncResultDto>> Sync(CancellationToken ct)
    {
        if (!_currentUser.RestaurantId.HasValue)
            return BadRequest("Usuário não pertence a um restaurante.");
        
        try {return Ok(await _syncService.SyncRestaurantCatalogAsync(_currentUser.RestaurantId.Value, ct)); } 
        catch (InvalidOperationException ex) {return BadRequest(ex.Message); }
    }

    [HttpGet("status")]
    [Authorize(Policy = IsmPolicies.RestaurantManagerOrAbove)]
    public async Task<ActionResult<IFoodCatalogStatusDto>> Status(CancellationToken ct)
    {
        if (!_currentUser.RestaurantId.HasValue)
            return BadRequest("Usuário não pertence a um restaurante.");
        
        return Ok(await _syncService.GetStatusAsync(_currentUser.RestaurantId.Value, ct));
    }
}