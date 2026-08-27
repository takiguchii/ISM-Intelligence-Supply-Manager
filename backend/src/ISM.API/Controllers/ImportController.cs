using ISM.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ISM.Domain.Modules.DataImport;
using ISM.Application.Interfaces.DataImport;

namespace ISM.API.Controllers;

[ApiController]
[Route("api/import")]
[Authorize(Policy = IsmPolicies.RestaurantAnyUser)]
public sealed class ImportController : ControllerBase
{
    private readonly IImportOrchestrator _orchestrator;
    private readonly ICurrentUser _currentUser;

    public ImportController(IImportOrchestrator orchestrator, ICurrentUser currentUser)
    {
        _orchestrator = orchestrator;
        _currentUser = currentUser;
    }

    [HttpPost("stock/products")]
    [Authorize(Policy = IsmPolicies.RestaurantManagerOrAbove)]
    [RequestSizeLimit(50 * 1024 * 1024)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ImportProductsCsv(
        IFormFile file,
        [FromQuery] UpsertStrategy strategy = UpsertStrategy.MergeByNameAndRestaurant,
        [FromQuery(Name = "restaurantId")] int? restaurantIdQuery = null,
        CancellationToken ct = default)
    {
        if (file == null || file.Length == 0)
            return BadRequest("Arquivo não enviado.");

        var restaurantId = ResolveRestaurantId(restaurantIdQuery);

        using var stream = file.OpenReadStream();
        var result = await _orchestrator.ExecuteFileImportAsync(
            stream,
            file.ContentType,
            file.FileName,
            new ImportContext(
                RestaurantId: restaurantId,
                UserId: _currentUser.UserId,
                DataSourceType: DataSourceType.Csv,
                TargetEntity: TargetImportEntity.Product,
                UpsertStrategy: strategy,
                DataSourceName: $"Upload CSV estoque - {file.FileName}",
                OriginalFileName: file.FileName),
            ct);

        return Ok(result);
    }

    [HttpPost("suppliers")]
    [Authorize(Policy = IsmPolicies.RestaurantManagerOrAbove)]
    [RequestSizeLimit(50 * 1024 * 1024)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ImportSuppliersCsv(
        IFormFile file,
        [FromQuery] UpsertStrategy strategy = UpsertStrategy.MergeByNameAndRestaurant,
        [FromQuery(Name = "restaurantId")] int? restaurantIdQuery = null,
        CancellationToken ct = default)
    {
        if (file == null || file.Length == 0)
            return BadRequest("Arquivo não enviado.");

        var restaurantId = ResolveRestaurantId(restaurantIdQuery);

        using var stream = file.OpenReadStream();
        var result = await _orchestrator.ExecuteFileImportAsync(
            stream,
            file.ContentType,
            file.FileName,
            new ImportContext(
                RestaurantId: restaurantId,
                UserId: _currentUser.UserId,
                DataSourceType: DataSourceType.Csv,
                TargetEntity: TargetImportEntity.Supplier,
                UpsertStrategy: strategy,
                DataSourceName: $"Upload CSV suppliers - {file.FileName}",
                OriginalFileName: file.FileName),
            ct);

        return Ok(result);
    }

    [HttpGet("history")]
    [Authorize(Policy = IsmPolicies.RestaurantManagerOrAbove)]
    public async Task<IActionResult> History(
        [FromQuery] int limit = 50,
        [FromQuery(Name = "restaurantId")] int? restaurantIdQuery = null,
        CancellationToken ct = default)
    {
        var restaurantId = ResolveRestaurantId(restaurantIdQuery, allowZeroForSuperAdminAll: true);
        var history = await _orchestrator.GetHistoryAsync(restaurantId, limit, ct);
        return Ok(history);
    }

    [HttpGet("history/{importId:guid}")]
    [Authorize(Policy = IsmPolicies.RestaurantManagerOrAbove)]
    public async Task<IActionResult> HistoryDetail(Guid importId, CancellationToken ct = default)
    {
        int? restaurantId = _currentUser.IsSuperAdmin ? null : _currentUser.RestaurantId;
        var detail = await _orchestrator.GetImportDetailAsync(importId, restaurantId, ct);
        return detail == null ? NotFound() : Ok(detail);
    }

    private int ResolveRestaurantId(int? queryRestaurantId, bool allowZeroForSuperAdminAll = false)
    {
        if (_currentUser.IsSuperAdmin)
        {
            if (queryRestaurantId.HasValue && queryRestaurantId.Value > 0)
                return queryRestaurantId.Value;
            if (allowZeroForSuperAdminAll)
                return 0; // 0 significa "todos restaurantes" para SuperAdmin no history
            throw new UnauthorizedAccessException("Super Admin deve informar ?restaurantId=X ao importar.");
        }

        if (!_currentUser.RestaurantId.HasValue)
            throw new UnauthorizedAccessException("Usuário sem restaurante vinculado.");

        if (queryRestaurantId.HasValue && queryRestaurantId.Value > 0 &&
            queryRestaurantId.Value != _currentUser.RestaurantId.Value)
            throw new UnauthorizedAccessException("Você só pode importar no seu próprio restaurante.");

        return _currentUser.RestaurantId.Value;
    }
}