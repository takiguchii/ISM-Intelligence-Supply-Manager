using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ISM.Application.DTOs.DataImport;
using ISM.Application.Interfaces.DataImport;
using ISM.Domain.Modules.DataImport;

namespace ISM.Application.Services.DataImport;

/// <summary>
/// Classe base para importers CSV: centraliza o ciclo de vida de auditoria
/// (hash SHA256, start/append errors/finish), o parsing compartilhado via
/// CsvParser e o loop de linhas com tratamento de erro por linha.
/// As subclasses implementam apenas a lógica de domínio (upsert por linha).
/// </summary>
public abstract class CsvImporterBase : IFileImporter
{
    private readonly IImportAuditRepository _auditRepo;

    protected CsvImporterBase(IImportAuditRepository auditRepo)
        => _auditRepo = auditRepo;

    public DataSourceType HandlesType => DataSourceType.Csv;
    public abstract TargetImportEntity HandlesEntity { get; }
    public bool CanHandle(string? contentType, string? fileName) => CsvParser.IsCsv(contentType, fileName);

    public async Task<ImportResultDto> ImportAsync(
        Stream fileContent,
        ImportContext context,
        CancellationToken ct)
    {
        // Buffer + hash do arquivo original (auditabilidade)
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

        var (rows, parseErrors) = await CsvParser.ReadRowsAsync(bufferedStream, ct);
        audit.TotalRecordsInSource = rows.Count;

        var errors = new List<ImportErrorLog>();
        errors.AddRange(parseErrors);

        var newOrUpdatedIds = new List<int>();
        var lineage = new Dictionary<int, int>();

        await OnBeforeRowsAsync(context, ct);

        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var rowNumber = i + 2;
            try
            {
                var entityId = await ImportRowAsync(row, context, rowNumber, ct);
                newOrUpdatedIds.Add(entityId);
                lineage[rowNumber] = entityId;
                audit.RecordsSucceeded++;
            }
            catch (Exception ex)
            {
                audit.RecordsFailed++;
                errors.Add(new ImportErrorLog
                {
                    ImportId = audit.ImportId,
                    SourceRowNumber = rowNumber,
                    EntityKeyValue = GetRowKeyValue(row),
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

    /// <summary>Hook executado antes do loop de linhas (ex: carregar entidades existentes).</summary>
    protected virtual Task OnBeforeRowsAsync(ImportContext context, CancellationToken ct) => Task.CompletedTask;

    /// <summary>Processa uma linha e retorna o Id da entidade criada/atualizada. Lança exceção em erro de validação.</summary>
    protected abstract Task<int> ImportRowAsync(
        Dictionary<string, string> row,
        ImportContext context,
        int rowNumber,
        CancellationToken ct);

    /// <summary>Valor-chave da linha para o log de erros (ex: nome). Retorna null quando ausente.</summary>
    protected abstract string? GetRowKeyValue(Dictionary<string, string> row);

    private static string ComputeSha256(byte[] bytes)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(bytes);
        var sb = new StringBuilder();
        foreach (var b in hash) sb.Append(b.ToString("x2"));
        return sb.ToString();
    }
}
