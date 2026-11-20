using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ISM.Application.DTOs.DataImport;
using ISM.Application.Interfaces.DataImport;
using ISM.Application.Security;
using ISM.Application.Services.DataImport;
using ISM.Domain.Modules.DataImport;
using ISM.Domain.Modules.Stock.Entities;
using ISM.Domain.Modules.Stock.Enums;
using ISM.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace ISM.Infrastructure.Services.DataImport;

public sealed class ReceiptConfirmationService(
    IsmDbContext db,
    ReceiptPreviewService previewService,
    ICurrentUser currentUser) : IReceiptConfirmationService
{
    public async Task<ReceiptConfirmationResultDto> ConfirmAsync(byte[] imageBytes, string fileName, string contentType,
        ReceiptConfirmationRequestDto request, CancellationToken ct)
    {
        if (!request.Confirm) throw new InvalidOperationException("Confirmação explícita é obrigatória.");
        var restaurantId = currentUser.RestaurantId ?? throw new UnauthorizedAccessException("Usuário sem restaurante.");
        if (!currentUser.IsManagerOrAbove) throw new UnauthorizedAccessException("Apenas gerentes podem confirmar recibos.");
        if (request.IdempotencyKey.Length is < 8 or > 80 || request.IdempotencyKey.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not ('-' or '_')))
            throw new InvalidOperationException("Chave de idempotência inválida.");

        var sha = Convert.ToHexString(SHA256.HashData(imageBytes)).ToLowerInvariant();
        if (!string.Equals(sha, request.ExpectedSha256, StringComparison.OrdinalIgnoreCase))
            throw new ReceiptConflictException("A imagem não corresponde à prévia revisada.");

        var mappings = request.ProductMappings.OrderBy(x => x.LineIndex).ToArray();
        var requestHash = Hash(JsonSerializer.Serialize(new { sha, mappings }));
        var existing = await db.ReceiptConfirmations.AsNoTracking()
            .FirstOrDefaultAsync(x => x.RestaurantId == restaurantId &&
                (x.IdempotencyKey == request.IdempotencyKey || x.ContentSha256 == sha), ct);
        if (existing is not null) return ResolveExisting(existing, request.IdempotencyKey, requestHash, sha);

        var preview = await previewService.AnalyzeAsync(imageBytes, fileName, contentType, ct);
        if (preview.Decision != "PREVIEW_REQUIRES_CONFIRMATION")
            throw new InvalidOperationException("A prévia exige revisão ou não permite entrada de estoque.");
        if (mappings.Length != preview.Items.Count || mappings.Select(x => x.LineIndex).Distinct().Count() != preview.Items.Count ||
            mappings.Where((x, i) => x.LineIndex != i).Any())
            throw new InvalidOperationException("Mapeie cada linha exatamente uma vez.");

        var documentIdentity = Hash($"{preview.Supplier!.Trim().ToUpperInvariant()}|{preview.DocumentNumber!.Trim().ToUpperInvariant()}|{preview.IssueDate:yyyy-MM-dd}");
        var byDocument = await db.ReceiptConfirmations.AsNoTracking()
            .FirstOrDefaultAsync(x => x.RestaurantId == restaurantId && x.DocumentIdentitySha256 == documentIdentity, ct);
        if (byDocument is not null) throw new ReceiptConflictException("Este documento já foi confirmado neste restaurante.");

        var strategy = db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
        db.ChangeTracker.Clear();
        var concurrentBefore = await db.ReceiptConfirmations.AsNoTracking()
            .FirstOrDefaultAsync(x => x.RestaurantId == restaurantId &&
                (x.IdempotencyKey == request.IdempotencyKey || x.ContentSha256 == sha || x.DocumentIdentitySha256 == documentIdentity), ct);
        if (concurrentBefore is not null) return ResolveExisting(concurrentBefore, request.IdempotencyKey, requestHash, sha);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var products = new Dictionary<int, Product>();
        for (var i = 0; i < mappings.Length; i++)
        {
            var mapping = mappings[i];
            var item = preview.Items[i];
            if (mapping.ProductId <= 0 || mapping.QuantityInStockUnit <= 0 || mapping.QuantityInStockUnit > 9999999.999m ||
                decimal.Round(mapping.QuantityInStockUnit, 3) != mapping.QuantityInStockUnit)
                throw new InvalidOperationException($"Quantidade de estoque inválida na linha {i + 1}.");
            if (!products.TryGetValue(mapping.ProductId, out var product))
            {
                product = await db.Products.SingleOrDefaultAsync(x => x.Id == mapping.ProductId && x.RestaurantId == restaurantId, ct)
                    ?? throw new InvalidOperationException($"Produto da linha {i + 1} não existe neste restaurante.");
                if (!product.IsActive) throw new InvalidOperationException($"Produto da linha {i + 1} está inativo.");
                products.Add(product.Id, product);
            }
            if (!string.Equals(item.Unit, product.Unit, StringComparison.OrdinalIgnoreCase) &&
                string.IsNullOrWhiteSpace(mapping.ConversionNote))
                throw new InvalidOperationException($"Informe a conversão de {item.Unit} para {product.Unit} na linha {i + 1}.");
            if (item.LineTotal is null || item.LineTotal < 0)
                throw new InvalidOperationException($"Total inválido na linha {i + 1}.");
        }

        var importId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        db.ReceiptConfirmations.Add(new ReceiptConfirmation
        {
            ImportId = importId,
            RestaurantId = restaurantId,
            ContentSha256 = sha,
            DocumentIdentitySha256 = documentIdentity,
            IdempotencyKey = request.IdempotencyKey,
            RequestSha256 = requestHash,
            ConfirmedByUserId = currentUser.UserId,
            ConfirmedAtUtc = now
        });
        db.ImportAudits.Add(new ImportAudit
        {
            ImportId = importId,
            RestaurantId = restaurantId,
            DataSourceName = $"Recibo confirmado - {preview.DocumentNumber}",
            DataSourceType = DataSourceType.ManualEntry,
            TargetEntity = TargetImportEntity.Product,
            UpsertStrategy = UpsertStrategy.MergeById,
            SourceOriginalFilename = Path.GetFileName(fileName),
            SourceContentHashSha256 = sha,
            ReceivedAtUtc = now,
            ReceivedByUserId = currentUser.UserId,
            TotalRecordsInSource = mappings.Length,
            RecordsSucceeded = mappings.Length,
            FinishedAtUtc = now,
            LineageSerializedJson = JsonSerializer.Serialize(new
            {
                preview.DocumentNumber, preview.Supplier, preview.IssueDate,
                Lines = mappings.Select((mapping, i) => new
                {
                    preview.Items[i].Description,
                    SourceQuantity = preview.Items[i].Quantity,
                    SourceUnit = preview.Items[i].Unit,
                    mapping.ProductId,
                    mapping.QuantityInStockUnit,
                    mapping.ConversionNote
                })
            })
        });

        var netCostFactor = preview.Subtotal is > 0m ? preview.Total!.Value / preview.Subtotal.Value : 1m;
        foreach (var group in mappings.GroupBy(x => x.ProductId))
        {
            var product = products[group.Key];
            var quantity = group.Sum(x => x.QuantityInStockUnit);
            var cost = group.Sum(x => preview.Items[x.LineIndex].LineTotal!.Value * netCostFactor);
            var newQuantity = product.CurrentQuantity + quantity;
            if (newQuantity > 9999999.999m) throw new InvalidOperationException("Saldo excede o limite do produto.");
            product.AverageCost = newQuantity == 0 ? product.AverageCost :
                decimal.Round((product.CurrentQuantity * product.AverageCost + cost) / newQuantity, 2, MidpointRounding.AwayFromZero);
            product.CurrentQuantity = newQuantity;
            product.UpdatedAtUtc = now;
        }
        foreach (var mapping in mappings)
        {
            var item = preview.Items[mapping.LineIndex];
            db.StockMovements.Add(new StockMovement
            {
                RestaurantId = restaurantId,
                ProductId = mapping.ProductId,
                MovementType = StockMovementType.Import,
                QuantityDelta = mapping.QuantityInStockUnit,
                UnitCostSnapshot = decimal.Round(item.LineTotal!.Value * netCostFactor / mapping.QuantityInStockUnit, 2, MidpointRounding.AwayFromZero),
                TriggeredByImportId = importId,
                TriggeredByUserId = currentUser.UserId,
                CreatedAtUtc = now
            });
        }

        try
        {
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is MySqlException { Number: 1062 })
        {
            await transaction.RollbackAsync(ct);
            db.ChangeTracker.Clear();
            var concurrent = await db.ReceiptConfirmations.AsNoTracking()
                .FirstOrDefaultAsync(x => x.RestaurantId == restaurantId &&
                    (x.IdempotencyKey == request.IdempotencyKey || x.ContentSha256 == sha || x.DocumentIdentitySha256 == documentIdentity), ct);
            if (concurrent is not null) return ResolveExisting(concurrent, request.IdempotencyKey, requestHash, sha);
            throw new ReceiptConflictException("Confirmação concorrente: tente consultar o histórico antes de reenviar.");
        }

        return new ReceiptConfirmationResultDto(importId, false, products.Count, sha);
        });
    }

    private static ReceiptConfirmationResultDto ResolveExisting(ReceiptConfirmation existing, string key, string requestHash, string sha)
    {
        if (existing.IdempotencyKey == key && existing.RequestSha256 == requestHash && existing.ContentSha256 == sha)
            return new ReceiptConfirmationResultDto(existing.ImportId, true, 0, sha);
        throw new ReceiptConflictException("Documento ou chave de idempotência já usado em outra confirmação.");
    }

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
