using ISM.Application.DTOs;
using ISM.Application.Interfaces;
using ISM.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ISM.API.Controllers;

[ApiController]
[Route("api/fornecedores")]
[Authorize(Policy = IsmPolicies.RestaurantAnyUser)]
public sealed class FornecedorController : ControllerBase
{
    private readonly IFornecedorService _fornecedorService;

    public FornecedorController(IFornecedorService fornecedorService)
    {
        _fornecedorService = fornecedorService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<FornecedorResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<FornecedorResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var fornecedores = await _fornecedorService.GetAllAsync(cancellationToken);
        return Ok(fornecedores);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(FornecedorResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FornecedorResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var fornecedor = await _fornecedorService.GetByIdAsync(id, cancellationToken);
        return fornecedor is null ? NotFound() : Ok(fornecedor);
    }

    [HttpPost]
    [Authorize(Policy = IsmPolicies.RestaurantManagerOrAbove)]
    [ProducesResponseType(typeof(FornecedorResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<FornecedorResponse>> Create(
        [FromBody] CreateFornecedorRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var created = await _fornecedorService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = IsmPolicies.RestaurantManagerOrAbove)]
    [ProducesResponseType(typeof(FornecedorResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FornecedorResponse>> Update(
        int id,
        [FromBody] UpdateFornecedorRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var updated = await _fornecedorService.UpdateAsync(id, request, cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = IsmPolicies.RestaurantManagerOrAbove)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await _fornecedorService.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
