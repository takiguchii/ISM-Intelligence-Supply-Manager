using ISM.Application.Interfaces;
using ISM.Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace ISM.API.Controllers;

[ApiController]
[Route("api/menu/dishes")]
public sealed class DishesController : ControllerBase
{
    private readonly IDishService _dishService;

    public DishesController(IDishService dishService)
    {
        _dishService = dishService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<DishResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DishResponse>>> GetAll(
        [FromQuery] int? restaurantId,
        [FromQuery] int? categoryId,
        CancellationToken cancellationToken)
    {
        var dishes = await _dishService.GetAllDishesAsync(restaurantId, categoryId, cancellationToken);
        return Ok(dishes);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(DishResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DishResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var dish = await _dishService.GetDishByIdAsync(id, cancellationToken);
        return dish is null ? NotFound() : Ok(dish);
    }

    [HttpPost]
    [ProducesResponseType(typeof(DishResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<DishResponse>> Create([FromBody] CreateDishRequest request, CancellationToken cancellationToken)
    {
        var created = await _dishService.CreateDishAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(DishResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DishResponse>> Update(
        int id,
        [FromBody] UpdateDishRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await _dishService.UpdateDishAsync(id, request, cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await _dishService.DeleteDishAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}