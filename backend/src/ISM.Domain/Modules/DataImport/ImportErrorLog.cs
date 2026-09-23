using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ISM.Domain.Modules.DataImport;

public sealed class ImportErrorLog
{
    [Key] public long Id { get; set; }

    public Guid ImportId { get; set; }
    [ForeignKey(nameof(ImportId))] public ImportAudit? Import { get; set; }

    public int SourceRowNumber { get; set; }
    [MaxLength(255)] public string? EntityKeyValue { get; set; }

    [Required] [MaxLength(500)] public string ErrorMessage { get; set; } = string.Empty;
    [MaxLength(4000)] public string? RawRowPayloadJson { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}