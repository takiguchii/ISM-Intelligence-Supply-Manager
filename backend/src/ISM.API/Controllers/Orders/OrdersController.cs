using ISM.Application.DTOs.Orders;
using ISM.Application.Interfaces.Orders;
using ISM.Application.Security;
using ISM.Domain.Modules.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ISM.API.Controllers.Orders;

[ApiController]
[Route("api/orders")]
[Authorize(Policy = IsmPolicies.RestaurantAnyUser)]
public sealed class OrdersController(IOrderService orders, ITabStore tabs, ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OrderResponse>>> GetAll(CancellationToken cancellationToken) => Ok(await orders.GetAllAsync(cancellationToken));

    [HttpGet("tabs/open")]
    [Authorize(Roles = IsmRoles.Cashier + "," + IsmRoles.Manager + "," + IsmRoles.Admin)]
    public async Task<ActionResult> GetOpenTab([FromQuery] string? tableReference, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(tableReference))
            return Ok(await tabs.GetOpenTabsAsync(currentUser.RestaurantId!.Value, cancellationToken));
        if (tableReference.Trim().Length > 100)
            return BadRequest("Informe a mesa da comanda.");

        var tab = await tabs.GetOpenAsync(currentUser.RestaurantId!.Value, tableReference.Trim(), cancellationToken);
        return tab is null ? NotFound() : Ok(tab);
    }

    [HttpPost("tabs/{tabId:int}/payments")]
    [Authorize(Roles = IsmRoles.Cashier + "," + IsmRoles.Manager + "," + IsmRoles.Admin)]
    public async Task<ActionResult<TabPaymentResponse>> PayTab(int tabId, PayTabRequest request, CancellationToken cancellationToken)
    {
        if (request.IdempotencyKey == Guid.Empty ||
            !Enum.IsDefined(request.PaymentMethod) || request.PaymentMethod == PaymentMethod.Unspecified ||
            request.Total < 0 || request.Total != decimal.Round(request.Total, 2))
            return BadRequest("Informe uma chave, uma forma de pagamento e um total válidos.");

        try
        {
            var payment = await tabs.PayAsync(currentUser.RestaurantId!.Value, tabId, request, cancellationToken);
            return payment is null ? NotFound() : Ok(payment);
        }
        catch (TabPaymentConflictException exception)
        {
            return Conflict(new ProblemDetails { Status = StatusCodes.Status409Conflict, Detail = exception.Message });
        }
    }

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
    [Authorize(Policy = IsmPolicies.RestaurantOrderOperator)]
    public async Task<ActionResult<OrderResponse>> Create(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var order = await orders.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }

    [HttpPut("{id:int}/status")]
    [Authorize(Policy = IsmPolicies.RestaurantOrderOperator)]
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
