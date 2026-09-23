using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ISM.Domain.Entities;

namespace ISM.Domain.Modules.DataImport;

public sealed class ImportAudit
{
    [Key] public Guid ImportId { get; set; }

    public int RestaurantId { get; set; }
    [ForeignKey(nameof(RestaurantId))] public Restaurant? Restaurant { get; set; }

    [Required] public string DataSourceName { get; set; } = string.Empty;
    public DataSourceType DataSourceType { get; set; }
    public TargetImportEntity TargetEntity { get; set; }
    public UpsertStrategy UpsertStrategy { get; set; }

    [MaxLength(500)] public string? SourceOriginalFilename { get; set; }
    [MaxLength(500)] public string? SourceEndpointOrUrl { get; set; }
    [Required] [MaxLength(80)] public string SourceContentHashSha256 { get; set; } = string.Empty;

    public DateTime ReceivedAtUtc { get; set; }
    public int? ReceivedByUserId { get; set; }

    public int TotalRecordsInSource { get; set; }
    public int RecordsSucceeded { get; set; }
    public int RecordsFailed { get; set; }

    [MaxLength(4000)] public string? LineageSerializedJson { get; set; }
    public DateTime? FinishedAtUtc { get; set; }

    public ICollection<ImportErrorLog> Errors { get; set; } = [];
}