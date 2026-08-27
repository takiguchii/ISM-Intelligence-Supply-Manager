using System.ComponentModel.DataAnnotations;

namespace ISM.Application.DTOs.DataImport;

public sealed class ImportResultDto
{
    public Guid ImportId { get; set; }

    [Required] public string DataSourceName { get; set; } = string.Empty;
    public string TargetEntity { get; set; } = string.Empty;
    public int TotalRecordsInSource { get; set; }
    public int RecordsSucceeded { get; set; }
    public int RecordsFailed { get; set; }

    public DateTime ReceivedAtUtc { get; set; }
    public DateTime? FinishedAtUtc { get; set; }

    public IReadOnlyList<int> NewOrUpdatedEntityIds { get; set; } = [];
    public IReadOnlyList<ImportErrorDto> Errors { get; set; } = [];
}

public sealed class ImportErrorDto
{
    public int SourceRowNumber { get; set; }
    public string? EntityKeyValue { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}