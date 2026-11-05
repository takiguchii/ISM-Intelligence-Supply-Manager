using System.ComponentModel.DataAnnotations;
using ISM.Application.Interfaces.Restaurant;
using ISM.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ISM.API.Controllers.Restaurant;

[ApiController, Authorize]
public sealed class SubscriptionController(IDemoSubscriptionService service, ICurrentUser user,
    IConfiguration configuration, IHostEnvironment environment) : ControllerBase
{
    private bool DemoEnabled => environment.IsDevelopment() || configuration.GetValue<bool>("Billing:DemoEnabled");

    [HttpGet("api/me/restaurant/subscription")]
    [Authorize(Policy = IsmPolicies.RestaurantManagerOrAbove)]
    public Task<IActionResult> GetMine(CancellationToken ct) => Get(user.RestaurantId!.Value, ct);

    [HttpGet("api/restaurants/{restaurantId:int}/subscription")]
    [Authorize(Policy = IsmPolicies.SuperAdminOnly)]
    public Task<IActionResult> GetByRestaurant(int restaurantId, CancellationToken ct) => Get(restaurantId, ct);

    private async Task<IActionResult> Get(int id, CancellationToken ct)
    {
        var subscription = await service.GetAsync(id, ct);
        return subscription == null ? NotFound() : Ok(new { demoEnabled = DemoEnabled, subscription });
    }

    [HttpPost("api/me/restaurant/subscription/plan")]
    [Authorize(Policy = IsmPolicies.RestaurantManagerOrAbove)]
    public Task<IActionResult> ChangeMine(ChangePlanRequest request, CancellationToken ct) => Change(user.RestaurantId!.Value, request, ct);

    [HttpPost("api/restaurants/{restaurantId:int}/subscription/plan")]
    [Authorize(Policy = IsmPolicies.SuperAdminOnly)]
    public Task<IActionResult> ChangeByRestaurant(int restaurantId, ChangePlanRequest request, CancellationToken ct) => Change(restaurantId, request, ct);

    private async Task<IActionResult> Change(int id, ChangePlanRequest request, CancellationToken ct)
    {
        if (!DemoEnabled) return BadRequest(new { message = "Assinaturas demonstrativas estão desativadas neste ambiente." });
        try { await service.ChangePlanAsync(id, request.PlanId, ct); return NoContent(); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }
}

public sealed class ChangePlanRequest
{
    [Range(1, int.MaxValue)]
    public int PlanId { get; set; }
}
