using System.Text.Json;
using ISM.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ISM.Domain.Modules.DataImport;
using ISM.Application.Interfaces.DataImport;
using ISM.Application.DTOs.DataImport;
using ISM.Application.Services.DataImport;

namespace ISM.API.Controllers;

[ApiController]
[Route("api/import")]
[Authorize(Policy = IsmPolicies.RestaurantAnyUser)]
public sealed class ImportController : ControllerBase
{
    private readonly IImportOrchestrator _orchestrator;
    private readonly ICurrentUser _currentUser;
    private readonly ReceiptPreviewService _receiptPreview;
    private readonly IReceiptConfirmationService _receiptConfirmation;

    public ImportController(IImportOrchestrator orchestrator, ICurrentUser currentUser,
        ReceiptPreviewService receiptPreview, IReceiptConfirmationService receiptConfirmation)
    {
        _orchestrator = orchestrator;
        _currentUser = currentUser;
        _receiptPreview = receiptPreview;
        _receiptConfirmation = receiptConfirmation;
    }

    /// <summary>Dry-run: analisa a estrutura da planilha e gera confirmações por categoria (estoque, fornecedores, finanças...) sem gravar nada.</summary>
    [HttpPost("preview")]
    [Authorize(Policy = IsmPolicies.RestaurantManagerOrAbove)]
    [RequestSizeLimit(50 * 1024 * 1024)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PreviewImport(IFormFile file, CancellationToken ct = default)
    {
        if (file == null || file.Length == 0)
            return BadRequest("Arquivo não enviado.");

        using var stream = file.OpenReadStream();
        var preview = await _orchestrator.PreviewFileAsync(stream, file.ContentType, file.FileName, ct);
        return Ok(preview);
    }

    /// <summary>Dry-run: extrai a tabela de uma foto/PDF escaneado (visão computacional) e gera confirmações por categoria.</summary>
    [HttpPost("photo-preview")]
    [Authorize(Policy = IsmPolicies.RestaurantManagerOrAbove)]
    [RequestSizeLimit(20 * 1024 * 1024)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PreviewPhotoImport(IFormFile file, CancellationToken ct = default)
    {
        if (file == null || file.Length == 0)
            return BadRequest("Arquivo não enviado.");

        var contentType = string.IsNullOrWhiteSpace(file.ContentType) ? "image/jpeg" : file.ContentType;
        using var stream = file.OpenReadStream();
        try
        {
            var preview = await _orchestrator.PreviewPhotoAsync(stream, contentType, file.FileName, ct);
            return Ok(preview);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return StatusCode(StatusCodes.Status504GatewayTimeout, "A análise da imagem excedeu o tempo limite. Tente uma imagem menor ou outro modelo.");
        }
    }

    /// <summary>Extrai recibo para uma prévia tipada. Não grava estoque nem histórico.</summary>
    [HttpPost("receipt-preview")]
    [Authorize(Policy = IsmPolicies.RestaurantManagerOrAbove)]
    [RequestSizeLimit(20 * 1024 * 1024)]
    [ProducesResponseType(typeof(ReceiptPreviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status504GatewayTimeout)]
    public async Task<IActionResult> PreviewReceipt(IFormFile file, CancellationToken ct = default)
    {
        if (file is null || file.Length == 0) return BadRequest("Arquivo não enviado.");
        if (file.Length > 12 * 1024 * 1024) return BadRequest("Imagem acima do limite de 12 MB.");
        await using var stream = file.OpenReadStream();
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, ct);
        try
        {
            return Ok(await _receiptPreview.AnalyzeAsync(buffer.ToArray(), file.FileName, file.ContentType, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return StatusCode(StatusCodes.Status504GatewayTimeout, "A análise da imagem excedeu o tempo limite. Tente uma imagem menor ou outro modelo.");
        }
    }

    /// <summary>Confirma uma prévia de compra após mapear cada linha. Aplica estoque e auditoria em uma transação.</summary>
    [HttpPost("receipt-confirm")]
    [Authorize(Policy = IsmPolicies.RestaurantManagerOrAbove)]
    [RequestSizeLimit(20 * 1024 * 1024)]
    [ProducesResponseType(typeof(ReceiptConfirmationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ConfirmReceipt(IFormFile file, [FromForm] string confirmation, CancellationToken ct = default)
    {
        if (file is null || file.Length == 0 || file.Length > 12 * 1024 * 1024)
            return BadRequest("Envie uma imagem de até 12 MB.");
        ReceiptConfirmationRequestDto? request;
        try
        {
            request = JsonSerializer.Deserialize<ReceiptConfirmationRequestDto>(confirmation,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException)
        {
            return BadRequest("Confirmação inválida.");
        }
        if (request is null) return BadRequest("Confirmação obrigatória.");

        await using var stream = file.OpenReadStream();
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, ct);
        try
        {
            return Ok(await _receiptConfirmation.ConfirmAsync(buffer.ToArray(), file.FileName, file.ContentType, request, ct));
        }
        catch (ReceiptConflictException ex)
        {
            return Conflict(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return StatusCode(StatusCodes.Status504GatewayTimeout, "A análise da imagem excedeu o tempo limite. Nenhum estoque foi alterado.");
        }
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
