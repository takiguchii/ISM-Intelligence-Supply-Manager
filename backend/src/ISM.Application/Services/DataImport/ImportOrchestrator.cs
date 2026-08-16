using ISM.Application.DTOs.DataImport;
using ISM.Application.Interfaces.DataImport;
using ISM.Domain.Modules.DataImport;

namespace ISM.Application.Services.DataImport;

public sealed class ImportOrchestrator : IImportOrchestrator
{
    private readonly IEnumerable<IFileImporter> _importers;
    private readonly IImportAuditRepository _auditRepo;

    public ImportOrchestrator(IEnumerable<IFileImporter> importers, IImportAuditRepository auditRepo)
    {
        _importers = importers;
        _auditRepo = auditRepo;
    }

    public async Task<ImportResultDto> ExecuteFileImportAsync(
        Stream fileContent,
        string? contentType,
        string? fileName,
        ImportContext context,
        CancellationToken ct)
    {
        var importer = _importers.FirstOrDefault(i => i.CanHandle(contentType, fileName));
        if (importer is null)
            throw new NotSupportedException(
                $"Nenhum importador registrado para ContentType='{contentType}', arquivo='{fileName}'.");

        var result = await importer.ImportAsync(fileContent, context, ct);
        return result;
    }

    public Task<IReadOnlyList<ImportAuditDto>> GetHistoryAsync(int restaurantId, int limit, CancellationToken ct)
        => _auditRepo.GetRecentByRestaurantAsync(restaurantId, limit, ct)
            .ContinueWith(t => (IReadOnlyList<ImportAuditDto>)t.Result.Select(a => new ImportAuditDto(
                a.ImportId,
                a.DataSourceName,
                a.DataSourceType.ToString(),
                a.TargetEntity.ToString(),
                a.TotalRecordsInSource,
                a.RecordsSucceeded,
                a.RecordsFailed,
                a.ReceivedAtUtc,
                a.FinishedAtUtc
            )).ToArray(), ct);

    public async Task<ImportAuditDetailDto?> GetImportDetailAsync(Guid importId, int? restaurantId, CancellationToken ct)
    {
        var a = await _auditRepo.GetByIdAsync(importId, restaurantId, ct);
        if (a is null) return null;

        return new ImportAuditDetailDto(
            a.ImportId,
            a.DataSourceName,
            a.DataSourceType.ToString(),
            a.TargetEntity.ToString(),
            a.SourceOriginalFilename,
            a.TotalRecordsInSource,
            a.RecordsSucceeded,
            a.RecordsFailed,
            a.ReceivedAtUtc,
            a.FinishedAtUtc,
            a.Errors.Select(e => new ImportErrorDto
            {
                SourceRowNumber = e.SourceRowNumber,
                EntityKeyValue = e.EntityKeyValue,
                ErrorMessage = e.ErrorMessage
            }).ToArray());
    }
}