using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using ISM.Application.DTOs.DataImport;
using ISM.Application.Interfaces.DataImport;
using ISM.Domain.Entities;
using ISM.Domain.Interfaces;
using ISM.Domain.Modules.DataImport;

namespace ISM.Application.Services.DataImport;

public sealed class CsvSupplierImporter : IFileImporter
{
    public DataSourceType HandlesType => DataSourceType.Csv;
    public TargetImportEntity HandlesEntity => TargetImportEntity.Supplier;

    private readonly ISupplierRepository _supplierRepo;
    private readonly IImportAuditRepository _auditRepo;

    public CsvSupplierImporter(ISupplierRepository supplierRepo, IImportAuditRepository auditRepo)
    {
        _supplierRepo = supplierRepo;
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
            ReceivedByUserId = context.UserId
        };
        await _auditRepo.StartImportAsync(audit, ct);

        var (rows, parseErrors) = await ParseCsvRowsAsync(bufferedStream, ct);
        audit.TotalRecordsInSource = rows.Count;

        var errors = new List<ImportErrorLog>();
        errors.AddRange(parseErrors);

        var newOrUpdatedIds = new List<int>();
        var lineage = new Dictionary<int, int>();

        var existingByName = (await _supplierRepo.GetAllAsync(ct))
            .Where(f => f.RestaurantId == context.RestaurantId)
            .GroupBy(f => f.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var rowNumber = i + 2;
            try
            {
                var (name, category, description, email, phone) = NormalizeRow(row, rowNumber);
                var key = name.Trim();
                Supplier? entity = null;

                if (context.UpsertStrategy == UpsertStrategy.MergeByNameAndRestaurant &&
                    existingByName.TryGetValue(key, out var existing))
                {
                    entity = existing;
                    entity.Category = category;
                    entity.Description = description;
                    entity.Email = email;
                    entity.Phone = phone;
                    entity.UpdatedAtUtc = DateTime.UtcNow;
                    entity = await _supplierRepo.UpdateAsync(entity, ct);
                    existingByName[key] = entity;
                }
                else
                {
                    entity = new Supplier
                    {
                        RestaurantId = context.RestaurantId,
                        Name = name.Trim(),
                        Category = category,
                        Description = description,
                        Email = email,
                        Phone = phone,
                        IsActive = true,
                        CreatedAtUtc = DateTime.UtcNow
                    };
                    entity = await _supplierRepo.AddAsync(entity, ct);
                    existingByName.TryAdd(key, entity);
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
                    EntityKeyValue = Try(row, "Nome", "Name", "Fornecedor", "Supplier"),
                    ErrorMessage = ex.Message,
                    RawRowPayloadJson = JsonSerializer.Serialize(row),
                    CreatedAtUtc = DateTime.UtcNow
                });
            }
        }

        if (errors.Count > 0)
            await _auditRepo.AppendErrorsAsync(audit.ImportId, errors, ct);

        var lineageJson = lineage.Count == 0 ? null : JsonSerializer.Serialize(lineage);
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
        var headerLine = await reader.ReadLineAsync(ct);
        if (string.IsNullOrWhiteSpace(headerLine)) return (rows, errors);
        var columns = ParseCsvLine(headerLine);
        if (columns.Count == 0) return (rows, errors);

        string? line;
        var rowNumber = 2;
        while ((line = await reader.ReadLineAsync(ct)) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) { rowNumber++; continue; }
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
                    ErrorMessage = $"Falha parse linha: {ex.Message}",
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
        var cur = new StringBuilder();
        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"') { cur.Append('"'); i++; }
                    else inQuotes = false;
                }
                else cur.Append(c);
            }
            else
            {
                if (c == ',') { result.Add(cur.ToString().Trim()); cur.Clear(); }
                else if (c == '"') inQuotes = true;
                else cur.Append(c);
            }
        }
        result.Add(cur.ToString().Trim());
        return result;
    }

    private static (string name, string category, string? description, string email, string phone)
        NormalizeRow(Dictionary<string, string> row, int rowNumber)
    {
        string Get(params string[] keys)
        {
            foreach (var k in keys)
                if (row.TryGetValue(k, out var v) && !string.IsNullOrWhiteSpace(v))
                    return v.Trim();
            return string.Empty;
        }

        var name = Get("Nome", "Name", "Fornecedor", "Supplier");
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException($"Nome do fornecedor é obrigatório na linha {rowNumber}.");

        var category = Get("Categoria", "Category", "Ramo");
        if (string.IsNullOrWhiteSpace(category)) category = "Geral";

        var description = Get("Descricao", "Description", "Descrição", "Observacao");
        if (string.IsNullOrWhiteSpace(description)) description = null;

        var email = Get("Email", "E-mail", "emailContato");
        if (string.IsNullOrWhiteSpace(email)) email = "nao-informado@exemplo.com";
        else if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            throw new InvalidOperationException($"Email inválido '{email}' na linha {rowNumber}.");

        var phone = Get("Telefone", "Phone", "Tel", "Contato");
        if (string.IsNullOrWhiteSpace(phone)) phone = "(00) 00000-0000";

        return (name, category, description, email, phone);
    }

    private static string Try(Dictionary<string, string> row, params string[] keys)
    {
        foreach (var k in keys)
            if (row.TryGetValue(k, out var v) && !string.IsNullOrWhiteSpace(v))
                return v.Trim();
        return string.Empty;
    }
}