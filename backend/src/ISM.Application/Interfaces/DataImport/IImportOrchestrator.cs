using ISM.Application.DTOs.DataImport;
using ISM.Domain.Modules.DataImport;

namespace ISM.Application.Interfaces.DataImport;

public sealed record ImportContext(
    int RestaurantId,
    int? UserId,
    DataSourceType DataSourceType,
    TargetImportEntity TargetEntity,
    UpsertStrategy UpsertStrategy,
    string DataSourceName,
    string? OriginalFileName);

public interface IFileImporter
{
    DataSourceType HandlesType { get; }
    TargetImportEntity HandlesEntity { get; }
    bool CanHandle(string? contentType, string? fileName);

    Task<ImportResultDto> ImportAsync(
        Stream fileContent,
        ImportContext context,
        CancellationToken ct);
}

public interface IImportOrchestrator
{
    Task<ImportResultDto> ExecuteFileImportAsync(
        Stream fileContent,
        string? contentType,
        string? fileName,
        ImportContext context,
        CancellationToken ct);

    /// <summary>Dry-run: analisa a planilha e gera as confirmações por categoria sem gravar nada.</summary>
    Task<ImportPreviewDto> PreviewFileAsync(
        Stream fileContent,
        string? contentType,
        string fileName,
        CancellationToken ct);

    /// <summary>Dry-run: extrai a tabela de uma foto/PDF escaneado e gera as confirmações por categoria.</summary>
    Task<ImportPreviewDto> PreviewPhotoAsync(
        Stream fileContent,
        string contentType,
        string fileName,
        CancellationToken ct);

    Task<IReadOnlyList<ImportAuditDto>> GetHistoryAsync(int restaurantId, int limit, CancellationToken ct);
    Task<ImportAuditDetailDto?> GetImportDetailAsync(Guid importId, int? restaurantId, CancellationToken ct);
}

public sealed record ImportAuditDto(
    Guid ImportId,
    string DataSourceName,
    string DataSourceType,
    string TargetEntity,
    int TotalRecordsInSource,
    int RecordsSucceeded,
    int RecordsFailed,
    DateTime ReceivedAtUtc,
    DateTime? FinishedAtUtc);

public sealed record ImportAuditDetailDto(
    Guid ImportId,
    string DataSourceName,
    string DataSourceType,
    string TargetEntity,
    string? SourceOriginalFilename,
    int TotalRecordsInSource,
    int RecordsSucceeded,
    int RecordsFailed,
    DateTime ReceivedAtUtc,
    DateTime? FinishedAtUtc,
    IReadOnlyList<ImportErrorDto> Errors);