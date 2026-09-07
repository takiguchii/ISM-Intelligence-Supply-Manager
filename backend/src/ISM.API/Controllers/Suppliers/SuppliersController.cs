using ISM.Application.DTOs;
using ISM.Application.Interfaces;
using ISM.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ISM.API.Controllers;

[ApiController]
[Route("api/suppliers")]
[Authorize(Policy = IsmPolicies.RestaurantAnyUser)]
public sealed class SuppliersController : ControllerBase
{
    private readonly ISupplierService _supplierService;

    public SuppliersController(ISupplierService supplierService)
    {
        _supplierService = supplierService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SupplierResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SupplierResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var suppliers = await _supplierService.GetAllAsync(cancellationToken);
        return Ok(suppliers);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(SupplierResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SupplierResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var supplier = await _supplierService.GetByIdAsync(id, cancellationToken);
        return supplier is null ? NotFound() : Ok(supplier);
    }

    [HttpPost]
    [Authorize(Policy = IsmPolicies.RestaurantManagerOrAbove)]
    [ProducesResponseType(typeof(SupplierResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SupplierResponse>> Create(
        [FromBody] CreateSupplierRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var created = await _supplierService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = IsmPolicies.RestaurantManagerOrAbove)]
    [ProducesResponseType(typeof(SupplierResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SupplierResponse>> Update(
        int id,
        [FromBody] UpdateSupplierRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var updated = await _supplierService.UpdateAsync(id, request, cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = IsmPolicies.RestaurantManagerOrAbove)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await _supplierService.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
