using ISM.Application.DTOs.DataImport;
using ISM.Application.Interfaces.DataImport;
using ISM.Domain.Modules.DataImport;
using ISM.Domain.Interfaces;

namespace ISM.Application.Services.DataImport;

public sealed class ImportOrchestrator : IImportOrchestrator
{
    private readonly IEnumerable<IFileImporter> _importers;
    private readonly IImportAuditRepository _auditRepo;
    private readonly ICsvStructureAnalyzer _analyzer;
    private readonly IPhotoImportService _photoImportService;

    public ImportOrchestrator(
        IEnumerable<IFileImporter> importers,
        IImportAuditRepository auditRepo,
        ICsvStructureAnalyzer analyzer,
        IPhotoImportService photoImportService)
    {
        _importers = importers;
        _auditRepo = auditRepo;
        _analyzer = analyzer;
        _photoImportService = photoImportService;
    }

    /// <summary>Dry-run: analisa a planilha e retorna as confirmações por categoria sem gravar nada.</summary>
    public Task<ImportPreviewDto> PreviewFileAsync(
        Stream fileContent,
        string? contentType,
        string fileName,
        CancellationToken ct)
        => _analyzer.AnalyzeAsync(fileContent, fileName, contentType, ct);

    /// <summary>Dry-run: extrai a tabela de uma foto/PDF escaneado e gera as confirmações por categoria.</summary>
    public Task<ImportPreviewDto> PreviewPhotoAsync(
        Stream fileContent,
        string contentType,
        string fileName,
        CancellationToken ct)
        => _photoImportService.AnalyzePhotoAsync(fileContent, fileName, contentType, ct);

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