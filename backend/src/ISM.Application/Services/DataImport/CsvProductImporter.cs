using System.Globalization;
using ISM.Application.Common;
using ISM.Application.Interfaces.DataImport;
using ISM.Domain.Modules.DataImport;
using ISM.Domain.Modules.Stock.Entities;
using ISM.Domain.Modules.Stock.Enums;
using ISM.Domain.Modules.Suppliers.Entities;
using ISM.Domain.Interfaces;

namespace ISM.Application.Services.DataImport;

/// <summary>Importador de produtos do estoque (planilha/CSV/XML NF-e → Stock.Product).</summary>
public sealed class CsvProductImporter : CsvImporterBase
{
    public override TargetImportEntity HandlesEntity => TargetImportEntity.Product;

    private readonly IProductRepository _productRepo;
    private readonly IStockMovementRepository _stockMovementRepo;
    private readonly ISupplierProductPriceHistoryRepository _supplierPriceHistoryRepo;
    private readonly ISupplierRepository? _supplierRepo;

    private Dictionary<string, Product>? _existingByName;
    private readonly Dictionary<int, (decimal OldQty, decimal NewQty, decimal? UnitCostSnapshot)> _qtyDeltasByProductId = [];
    private ImportContext? _currentContext;

    public CsvProductImporter(
        IProductRepository productRepo,
        IStockMovementRepository stockMovementRepo,
        ISupplierProductPriceHistoryRepository supplierPriceHistoryRepo,
        IImportAuditRepository auditRepo,
        IImportFileReaderResolver fileReaders,
        ISupplierRepository? supplierRepo = null)
        : base(auditRepo, fileReaders)
    {
        _productRepo = productRepo;
        _stockMovementRepo = stockMovementRepo;
        _supplierPriceHistoryRepo = supplierPriceHistoryRepo;
        _supplierRepo = supplierRepo;
    }

    protected override async Task OnAfterFileReadAsync(
        ImportContext context,
        Guid importId,
        ImportFileContent content,
        CancellationToken ct)
    {
        _currentContext = context;

        // NF-e XML: gravar 1 linha de SupplierProductPriceHistory por item
        if (content.ExtendedRows is { Count: > 0 } &&
            content.Metadata?.ContainsKey("origin") == true &&
            content.Metadata["origin"] == "nfe_xml")
        {
            try
            {
                string? supplierCnpj = null;
                string? supplierRawName = null;
                if (content.Metadata.TryGetValue("nfe.supplier_cnpj", out var cnpjVal))
                    supplierCnpj = cnpjVal;
                if (content.Metadata.TryGetValue("nfe.supplier_raw_name", out var rawNameVal))
                    supplierRawName = rawNameVal;

                // Tentar match de fornecedor já cadastrado por CNPJ (sem tenant filter implícito, restaurante explícito)
                int? matchedSupplierId = null;
                if (_supplierRepo != null && !string.IsNullOrWhiteSpace(supplierCnpj))
                {
                    var allSuppliers = await _supplierRepo.GetAllAsync(ct);
                    var matchByCnpjOrName = allSuppliers
                        .FirstOrDefault(s =>
                            s.RestaurantId == context.RestaurantId &&
                            (!string.IsNullOrWhiteSpace(supplierCnpj) &&
                             string.Equals(supplierCnpj, s.Name, StringComparison.Ordinal) ? false : // Supplier não tem CNPJ hoje, match por nome normalizado
                             StringHelper.NormalizeName(supplierRawName) == StringHelper.NormalizeName(s.Name)));
                    if (matchByCnpjOrName != null)
                        matchedSupplierId = matchByCnpjOrName.Id;
                }

                var historyEntries = new List<SupplierProductPriceHistory>(content.ExtendedRows.Count);
                foreach (var row in content.ExtendedRows)
                {
                    string ValStr(string key) =>
                        row.TryGetValue(key, out var v) && v != null
                            ? Convert.ToString(v, CultureInfo.InvariantCulture) ?? string.Empty
                            : string.Empty;
                    decimal ValDec(string key, decimal fallback = 0m)
                    {
                        if (row.TryGetValue(key, out var v) && v != null)
                        {
                            try { return Convert.ToDecimal(v, CultureInfo.InvariantCulture); }
                            catch { return fallback; }
                        }
                        return fallback;
                    }
                    DateTime ValDt(string key)
                    {
                        if (row.TryGetValue(key, out var v) && v != null)
                        {
                            try { return Convert.ToDateTime(v, CultureInfo.InvariantCulture).ToUniversalTime(); }
                            catch { return DateTime.UtcNow; }
                        }
                        return DateTime.UtcNow;
                    }

                    var unitPrice = ValDec("unit_price");
                    var qty = ValDec("quantity_purchased");
                    if (unitPrice <= 0 || qty <= 0) continue;

                    var prodRaw = ValStr("product_raw_name");
                    var unit = ValStr("unit");
                    if (string.IsNullOrWhiteSpace(prodRaw)) continue;

                    var nfeKeyOrImportId = ValStr("nfe_access_key_or_import_id");
                    if (string.IsNullOrWhiteSpace(nfeKeyOrImportId))
                        nfeKeyOrImportId = importId.ToString();

                    historyEntries.Add(new SupplierProductPriceHistory
                    {
                        RestaurantId = context.RestaurantId,
                        SupplierId = matchedSupplierId,
                        SupplierRawName = string.IsNullOrWhiteSpace(supplierRawName)
                            ? (ValStr("supplier_raw_name") ?? "Fornecedor não identificado")
                            : supplierRawName,
                        ProductRawName = prodRaw,
                        Unit = string.IsNullOrWhiteSpace(unit) ? "un" : unit,
                        UnitPrice = unitPrice,
                        QuantityPurchased = qty,
                        NFeAccessKeyOrImportId = nfeKeyOrImportId,
                        PurchasedAtUtc = ValDt("purchased_at_utc"),
                    });
                }

                if (historyEntries.Count > 0)
                    await _supplierPriceHistoryRepo.BulkAddAsync(historyEntries, ct);
            }
            catch
            {
                // NÃO quebra a importação por falha de histórico complementar
            }
        }
    }

    protected override async Task OnBeforeRowsAsync(ImportContext context, CancellationToken ct)
    {
        _currentContext = context;
        _qtyDeltasByProductId.Clear();
        _existingByName = (await _productRepo.GetAllAsync(ct))
            .Where(p => p.RestaurantId == context.RestaurantId)
            .GroupBy(p => p.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
    }

    protected override async Task<int> ImportRowAsync(
        Dictionary<string, string> row,
        ImportContext context,
        int rowNumber,
        CancellationToken ct)
    {
        _currentContext = context;
        var norm = NormalizeRow(row, rowNumber);
        if (string.IsNullOrWhiteSpace(norm.Name))
            throw new InvalidOperationException("Nome do produto é obrigatório.");

        var key = norm.Name.Trim();
        var strategy = context.UpsertStrategy;

        Product entity;
        decimal oldQty = 0m;
        if (strategy == UpsertStrategy.MergeByNameAndRestaurant &&
            _existingByName!.TryGetValue(key, out var existing))
        {
            oldQty = existing.CurrentQuantity;
            existing.Unit = norm.Unit;
            existing.CurrentQuantity = norm.Qtd;
            existing.MinimumQuantity = norm.Min > 0 ? norm.Min : existing.MinimumQuantity;
            existing.AverageCost = norm.CustoMedio > 0 ? norm.CustoMedio : existing.AverageCost;
            existing.UpdatedAtUtc = DateTime.UtcNow;
            await _productRepo.UpdateAsync(existing, ct);
            entity = existing;
        }
        else
        {
            entity = new Product
            {
                RestaurantId = context.RestaurantId,
                Name = norm.Name.Trim(),
                Unit = norm.Unit,
                CurrentQuantity = norm.Qtd,
                MinimumQuantity = norm.Min,
                AverageCost = norm.CustoMedio,
                CreatedAtUtc = DateTime.UtcNow
            };
            entity = await _productRepo.AddAsync(entity, ct);
        }

        var delta = entity.CurrentQuantity - oldQty;
        if (delta != 0m)
        {
            _qtyDeltasByProductId[entity.Id] = (
                OldQty: oldQty,
                NewQty: entity.CurrentQuantity,
                UnitCostSnapshot: entity.AverageCost > 0 ? entity.AverageCost : null);
        }

        _existingByName![key] = entity;
        return entity.Id;
    }

    protected override async Task OnAfterRowsAsync(
        ImportContext context,
        Guid importId,
        IReadOnlyList<int> newOrUpdatedIds,
        IReadOnlyDictionary<int, int> lineageByRow,
        CancellationToken ct)
    {
        if (_qtyDeltasByProductId.Count == 0) return;

        try
        {
            var movements = new List<StockMovement>(_qtyDeltasByProductId.Count);
            foreach (var kvp in _qtyDeltasByProductId)
            {
                var productId = kvp.Key;
                var (old, @new, unitCost) = kvp.Value;
                var delta = @new - old;
                if (delta == 0m) continue;

                var movementType = delta > 0
                    ? StockMovementType.Import
                    : StockMovementType.ManualDecrease;

                movements.Add(new StockMovement
                {
                    RestaurantId = context.RestaurantId,
                    ProductId = productId,
                    MovementType = movementType,
                    QuantityDelta = delta,
                    UnitCostSnapshot = unitCost,
                    TriggeredByImportId = importId,
                    TriggeredByUserId = context.UserId,
                });
            }

            if (movements.Count > 0)
                await _stockMovementRepo.BulkAddAsync(movements, ct);
        }
        catch
        {
            // NÃO quebra a importação por falha de movimento complementar
        }
        finally
        {
            _qtyDeltasByProductId.Clear();
        }
    }

    protected override string? GetRowKeyValue(Dictionary<string, string> row) =>
        row.TryGetValue("Nome", out var n) ? n :
        row.TryGetValue("Name", out var ne) ? ne : null;

    private static (string Name, string Unit, decimal Qtd, decimal Min, decimal CustoMedio)
        NormalizeRow(Dictionary<string, string> row, int rowNumber)
    {
        string Get(params string[] keys)
        {
            foreach (var k in keys)
                if (row.TryGetValue(k, out var v) && !string.IsNullOrWhiteSpace(v))
                    return v.Trim();
            return string.Empty;
        }

        var name = Get("Nome", "Name", "Produto", "Prod ");
        var unit = Get("Unidade", "Unit", "Un", "UM");
        if (string.IsNullOrWhiteSpace(unit)) unit = "un";

        decimal ParseDecimal(string v, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(v)) return 0m;
            var cleaned = v
                .Replace("R$", "", StringComparison.Ordinal)
                .Replace(" ", "", StringComparison.Ordinal);
            if (decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.GetCultureInfo("pt-BR"), out var dec))
                return dec;
            if (decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out dec))
                return dec;
            throw new InvalidOperationException(
                $"Campo '{fieldName}' valor inválido: '{v}' na linha {rowNumber}");
        }

        var qtd = ParseDecimal(Get("QuantidadeAtual", "CurrentQuantity", "Qtd Atual", "Estoque", "EstoqueAtual"), "QuantidadeAtual");
        var min = ParseDecimal(Get("QuantidadeMinima", "MinimumQuantity", "Qtd Mínima", "EstoqueMinimo", "Min"), "QuantidadeMinima");
        var custo = ParseDecimal(Get("CustoMedio", "AverageCost", "Custo Médio", "PrecoCusto", "Custo"), "CustoMedio");

        return (name, unit, qtd, min, custo);
    }
}
