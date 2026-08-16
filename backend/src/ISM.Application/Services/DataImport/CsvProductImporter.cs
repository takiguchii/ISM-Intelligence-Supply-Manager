using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ISM.Application.DTOs.DataImport;
using ISM.Application.Interfaces.DataImport;
using ISM.Domain.Modules.DataImport;
using ISM.Domain.Modules.Stock.Entities;
using ISM.Domain.Modules.Stock.Interfaces;

namespace ISM.Application.Services.DataImport;

public sealed class CsvProductImporter : IFileImporter
{
    public DataSourceType HandlesType => DataSourceType.Csv;
    public TargetImportEntity HandlesEntity => TargetImportEntity.Product;

    private readonly IProductRepository _productRepo;
    private readonly IImportAuditRepository _auditRepo;

    public CsvProductImporter(IProductRepository productRepo, IImportAuditRepository auditRepo)
    {
        _productRepo = productRepo;
        _auditRepo = auditRepo;
    }

    public bool CanHandle(string? contentType, string? fileName)
    {
        var isCsv =
            string.Equals(contentType, "text/csv", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(contentType, "application/csv", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(contentType, "application/vnd.ms-excel", StringComparison.OrdinalIgnoreCase) ||
            (fileName ?? string.Empty).EndsWith(".csv", StringComparison.OrdinalIgnoreCase);
        return isCsv;
    }

    public async Task<ImportResultDto> ImportAsync(
        Stream fileContent,
        ImportContext context,
        CancellationToken ct)
    {
        fileContent.Position = 0;
        using var bufferedStream = new MemoryStream();
        await fileContent.CopyToAsync(bufferedStream, ct);
        var sha256 = ComputeSha256(bufferedStream.ToArray());
        bufferedStream.Position = 0;

        var audit = new ImportAudit
        {
            ImportId = Guid.NewGuid(),
            RestaurantId = context.RestaurantId,
            DataSourceName = context.DataSourceName,
            DataSourceType = context.DataSourceType,
            TargetEntity = context.TargetEntity,
            UpsertStrategy = context.UpsertStrategy,
            SourceOriginalFilename = context.OriginalFileName,
            SourceContentHashSha256 = sha256,
            ReceivedAtUtc = DateTime.UtcNow,
            ReceivedByUserId = context.UserId,
            TotalRecordsInSource = 0,
            RecordsSucceeded = 0,
            RecordsFailed = 0
        };
        await _auditRepo.StartImportAsync(audit, ct);

        var (rows, parseErrors) = await ParseCsvRowsAsync(bufferedStream, ct);
        audit.TotalRecordsInSource = rows.Count;

        var errors = new List<ImportErrorLog>();
        errors.AddRange(parseErrors);

        var newOrUpdatedIds = new List<int>();
        var lineage = new Dictionary<int, int>();

        var existingProductsByName = (await _productRepo.GetAllAsync(ct))
            .Where(p => p.RestaurantId == context.RestaurantId)
            .GroupBy(p => p.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var rowNumber = i + 2;
            try
            {
                var norm = NormalizeRow(row, rowNumber);
                if (string.IsNullOrWhiteSpace(norm.Name))
                    throw new InvalidOperationException("Nome do produto é obrigatório.");

                var key = norm.Name.Trim();
                var strategy = context.UpsertStrategy;

                Product? entity = null;
                if (strategy == UpsertStrategy.MergeByNameAndRestaurant &&
                    existingProductsByName.TryGetValue(key, out var existing))
                {
                    entity = existing;
                    entity.Unit = norm.Unit;
                    entity.CurrentQuantity = norm.Qtd;
                    entity.MinimumQuantity = norm.Min > 0 ? norm.Min : entity.MinimumQuantity;
                    entity.AverageCost = norm.CustoMedio > 0 ? norm.CustoMedio : entity.AverageCost;
                    entity.UpdatedAtUtc = DateTime.UtcNow;
                    await _productRepo.UpdateAsync(entity, ct);
                    existingProductsByName[key] = entity;
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
                    existingProductsByName.TryAdd(key, entity);
                }

                newOrUpdatedIds.Add(entity.Id);
                lineage[rowNumber] = entity.Id;
                audit.RecordsSucceeded++;
            }
            catch (Exception ex)
            {
                audit.RecordsFailed++;
                errors.Add(new ImportErrorLog
                {
                    ImportId = audit.ImportId,
                    SourceRowNumber = rowNumber,
                    EntityKeyValue = row.TryGetValue("Nome", out var n) ? n :
                                     row.TryGetValue("Name", out var ne) ? ne : null,
                    ErrorMessage = ex.Message,
                    RawRowPayloadJson = JsonSerializer.Serialize(row),
                    CreatedAtUtc = DateTime.UtcNow
                });
            }
        }

        var lineageJson = lineage.Count == 0 ? null : JsonSerializer.Serialize(lineage);
        if (errors.Count > 0)
            await _auditRepo.AppendErrorsAsync(audit.ImportId, errors, ct);

        await _auditRepo.FinishImportAsync(audit.ImportId, audit.RecordsSucceeded, audit.RecordsFailed, lineageJson, ct);

        return new ImportResultDto
        {
            ImportId = audit.ImportId,
            DataSourceName = audit.DataSourceName,
            TargetEntity = audit.TargetEntity.ToString(),
            TotalRecordsInSource = audit.TotalRecordsInSource,
            RecordsSucceeded = audit.RecordsSucceeded,
            RecordsFailed = audit.RecordsFailed,
            ReceivedAtUtc = audit.ReceivedAtUtc,
            FinishedAtUtc = DateTime.UtcNow,
            NewOrUpdatedEntityIds = newOrUpdatedIds,
            Errors = errors.Select(e => new ImportErrorDto
            {
                SourceRowNumber = e.SourceRowNumber,
                EntityKeyValue = e.EntityKeyValue,
                ErrorMessage = e.ErrorMessage
            }).ToArray()
        };
    }

    private static string ComputeSha256(byte[] bytes)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(bytes);
        var sb = new StringBuilder();
        foreach (var b in hash) sb.Append(b.ToString("x2"));
        return sb.ToString();
    }

    private static async Task<(List<Dictionary<string, string>> rows, List<ImportErrorLog> errors)> ParseCsvRowsAsync(
        Stream stream, CancellationToken ct)
    {
        var rows = new List<Dictionary<string, string>>();
        var errors = new List<ImportErrorLog>();
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var headerLine = (await reader.ReadLineAsync(ct).ConfigureAwait(false));
        if (string.IsNullOrWhiteSpace(headerLine))
            return (rows, errors);

        var columns = ParseCsvLine(headerLine);
        if (columns.Count == 0)
            return (rows, errors);

        var rowNumber = 2;
        string? line;
        while ((line = await reader.ReadLineAsync(ct).ConfigureAwait(false)) != null)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                rowNumber++;
                continue;
            }
            try
            {
                var values = ParseCsvLine(line);
                var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                for (var i = 0; i < columns.Count; i++)
                    dict[columns[i]] = i < values.Count ? values[i] : string.Empty;
                rows.Add(dict);
            }
            catch (Exception ex)
            {
                errors.Add(new ImportErrorLog
                {
                    SourceRowNumber = rowNumber,
                    ErrorMessage = $"Falha ao parsear linha: {ex.Message}",
                    RawRowPayloadJson = line,
                    CreatedAtUtc = DateTime.UtcNow
                });
            }
            rowNumber++;
        }
        return (rows, errors);
    }

    private static List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        var inQuotes = false;
        var current = new StringBuilder();
        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else inQuotes = false;
                }
                else current.Append(c);
            }
            else
            {
                if (c == ',')
                {
                    result.Add(current.ToString().Trim());
                    current.Clear();
                }
                else if (c == '"') inQuotes = true;
                else current.Append(c);
            }
        }
        result.Add(current.ToString().Trim());
        return result;
    }

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