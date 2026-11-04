using ISM.Application.DTOs.Orders;
using ISM.Application.Interfaces.Orders;
using ISM.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ISM.API.Controllers.Orders;

[ApiController]
[Route("api/orders")]
[Authorize(Policy = IsmPolicies.RestaurantAnyUser)]
public sealed class OrdersController(IOrderService orders) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OrderResponse>>> GetAll(CancellationToken cancellationToken) => Ok(await orders.GetAllAsync(cancellationToken));

    [HttpGet("admin")]
    [Authorize(Policy = IsmPolicies.RestaurantManagerOrAbove)]
    public async Task<ActionResult<OrderPage>> GetAdminPage([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null, CancellationToken cancellationToken = default) =>
        Ok(await orders.GetAdminPageAsync(pageNumber, pageSize, search, cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var order = await orders.GetByIdAsync(id, cancellationToken);
        return order is null ? NotFound() : Ok(order);
    }

    [HttpPost]
    public async Task<ActionResult<OrderResponse>> Create(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var order = await orders.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }

    [HttpPut("{id:int}/status")]
    public async Task<ActionResult<OrderResponse>> UpdateStatus(int id, UpdateOrderStatusRequest request, CancellationToken cancellationToken)
    {
        var order = await orders.UpdateStatusAsync(id, request, cancellationToken);
        return order is null ? NotFound() : Ok(order);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = IsmPolicies.RestaurantManagerOrAbove)]
    public async Task<ActionResult<OrderResponse>> Update(int id, UpdateOrderRequest request, CancellationToken cancellationToken)
    {
        var order = await orders.UpdateAsync(id, request, cancellationToken);
        return order is null ? NotFound() : Ok(order);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = IsmPolicies.RestaurantManagerOrAbove)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        await orders.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound();
}
