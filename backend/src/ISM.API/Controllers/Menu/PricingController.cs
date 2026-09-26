using ISM.Application.Interfaces;
using ISM.Application.DTOs;
using ISM.Application.Interfaces.Menu;
using ISM.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ISM.API.Controllers;
[ApiController]
[Route("api/menu/dishes")]
[Authorize(Policy = IsmPolicies.RestaurantAnyUser)]

public sealed class PricingController : ControllerBase
{
    private readonly IPricingCmvService _pricingCmvService;

    public PricingController(IPricingCmvService priceCmvService)
    {
        _pricingCmvService = priceCmvService;
    }

    [HttpGet("{id:int}/pricing")]
    [ProducesResponseType(typeof(DishPricingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DishPricingResponse>> GetPricing(int id,
        CancellationToken cancellationToken = default)
    {
        var pricing = await _pricingCmvService.GetPricingForDishAsync(id, cancellationToken);
        return pricing is null ? NotFound() : Ok(pricing);
    }
}