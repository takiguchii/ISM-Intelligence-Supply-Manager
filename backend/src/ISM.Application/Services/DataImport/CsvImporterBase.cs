using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ISM.Application.DTOs.DataImport;
using ISM.Application.Interfaces.DataImport;
using ISM.Domain.Modules.DataImport;
using ISM.Domain.Interfaces;

namespace ISM.Application.Services.DataImport;

/// <summary>
/// Classe base para importers de arquivo: centraliza o ciclo de vida de auditoria
/// (hash SHA256, start/append errors/finish), a normalização de qualquer formato
/// suportado via IImportFileReaderResolver e o loop de linhas com tratamento de erro.
/// As subclasses implementam apenas a lógica de domínio (upsert por linha).
/// </summary>
public abstract class CsvImporterBase : IFileImporter
{
    private static readonly string[] SupportedExtensions = [".csv", ".xlsx", ".xlsm", ".xml", ".json", ".txt"];

    public static bool IsSupportedFormat(string? contentType, string? fileName)
        => SupportedExtensions.Contains(Path.GetExtension(fileName ?? ""), StringComparer.OrdinalIgnoreCase)
           || CsvParser.IsCsv(contentType, fileName);

    private readonly IImportAuditRepository _auditRepo;
    private readonly IImportFileReaderResolver _fileReaders;

    protected ImportFileContent? LastReadContent { get; private set; }

    protected CsvImporterBase(IImportAuditRepository auditRepo, IImportFileReaderResolver fileReaders)
        => (_auditRepo, _fileReaders) = (auditRepo, fileReaders);

    public DataSourceType HandlesType => DataSourceType.Csv;
    public abstract TargetImportEntity HandlesEntity { get; }
    public bool CanHandle(string? contentType, string? fileName) => IsSupportedFormat(contentType, fileName);

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

        // Qualquer formato suportado vira linhas normalizadas antes do loop de domínio
        var content = await _fileReaders.ReadAsync(
            bufferedStream, context.OriginalFileName ?? string.Empty, contentType: null, ct);

        LastReadContent = content;

        try
        {
            await OnAfterFileReadAsync(context, audit.ImportId, content, ct);
        }
        catch (Exception)
        {
            // Erro no hook pós-leitura NÃO quebra a importação principal
            // (ex.: falha ao gravar histórico de preços do fornecedor)
        }

        audit.TotalRecordsInSource = content.Rows.Count;

        var errors = new List<ImportErrorLog>();
        errors.AddRange(content.Errors);
        var rows = content.Rows;

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

        try
        {
            await OnAfterRowsAsync(context, audit.ImportId, newOrUpdatedIds, lineage, ct);
        }
        catch (Exception)
        {
            // Erro no hook pós-processamento NÃO quebra a importação principal
            // (ex.: falha ao gravar movimentos de estoque)
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

    /// <summary>
    /// Hook executado logo após a leitura do arquivo, antes do loop de linhas.
    /// Ideal para extrair metadados globais (ex.: fornecedor da NF-e) e gravar
    /// históricos complementares SEM depender do resultado do upsert.
    /// </summary>
    protected virtual Task OnAfterFileReadAsync(
        ImportContext context,
        Guid importId,
        ImportFileContent content,
        CancellationToken ct)
        => Task.CompletedTask;

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

    /// <summary>
    /// Hook executado após o loop de linhas, com a lista de ids criados/atualizados.
    /// Ideal para gravar lote complementar (ex.: movimentos de estoque).
    /// </summary>
    protected virtual Task OnAfterRowsAsync(
        ImportContext context,
        Guid importId,
        IReadOnlyList<int> newOrUpdatedIds,
        IReadOnlyDictionary<int, int> lineageByRow,
        CancellationToken ct)
        => Task.CompletedTask;

    private static string ComputeSha256(byte[] bytes)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(bytes);
        var sb = new StringBuilder();
        foreach (var b in hash) sb.Append(b.ToString("x2"));
        return sb.ToString();
    }
}
