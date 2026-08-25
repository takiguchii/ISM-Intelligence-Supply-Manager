using System.Globalization;
using ISM.Application.Interfaces.DataImport;
using ISM.Domain.Modules.DataImport;
using ISM.Domain.Modules.Stock.Entities;
using ISM.Domain.Modules.Stock.Interfaces;

namespace ISM.Application.Services.DataImport;

/// <summary>Importador de produtos do estoque (planilha/CSV → Stock.Product).</summary>
public sealed class CsvProductImporter : CsvImporterBase
{
    public override TargetImportEntity HandlesEntity => TargetImportEntity.Product;

    private readonly IProductRepository _productRepo;
    private Dictionary<string, Product>? _existingByName;

    public CsvProductImporter(
        IProductRepository productRepo,
        IImportAuditRepository auditRepo,
        IImportFileReaderResolver fileReaders)
        : base(auditRepo, fileReaders)
        => _productRepo = productRepo;

    protected override async Task OnBeforeRowsAsync(ImportContext context, CancellationToken ct)
    {
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
        var norm = NormalizeRow(row, rowNumber);
        if (string.IsNullOrWhiteSpace(norm.Name))
            throw new InvalidOperationException("Nome do produto é obrigatório.");

        var key = norm.Name.Trim();
        var strategy = context.UpsertStrategy;

        Product entity;
        if (strategy == UpsertStrategy.MergeByNameAndRestaurant &&
            _existingByName!.TryGetValue(key, out var existing))
        {
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

        _existingByName![key] = entity;
        return entity.Id;
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
