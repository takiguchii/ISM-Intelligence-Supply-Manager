using ISM.Application.DTOs;
using ISM.Application.Interfaces;
using ISM.Application.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ISM.API.Controllers;

[ApiController]
[Route("api/fornecedores")]
public sealed class FornecedorController : ControllerBase
{
    private readonly IFornecedorService _fornecedorService;

    public FornecedorController(IFornecedorService fornecedorService)
    {
        _fornecedorService = fornecedorService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<FornecedorDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<FornecedorDto>>> GetAll(CancellationToken cancellationToken)
    {
        var response = await _fornecedorService.GetAllAsync(cancellationToken);
        return Ok(response);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(FornecedorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FornecedorDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var response = await _fornecedorService.GetByIdAsync(id, cancellationToken);
        if (response == null)
            return NotFound();

        return Ok(response);
    }

    [HttpPost]
    [ProducesResponseType(typeof(FornecedorDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<FornecedorDto>> Create([FromBody] FornecedorDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var response = await _fornecedorService.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] FornecedorDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var success = await _fornecedorService.UpdateAsync(id, dto, cancellationToken);
        if (!success)
            return NotFound();

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var success = await _fornecedorService.DeleteAsync(id, cancellationToken);
        if (!success)
            return NotFound();

        return NoContent();
    }
}
