namespace ISM.Domain.Modules.DataImport;

public interface IImportAuditRepository
{
    Task<ImportAudit> StartImportAsync(ImportAudit audit, CancellationToken ct);
    Task FinishImportAsync(Guid importId, int succeeded, int failed, string? lineageJson, CancellationToken ct);
    Task AppendErrorsAsync(Guid importId, IEnumerable<ImportErrorLog> errors, CancellationToken ct);
    Task<IReadOnlyList<ImportAudit>> GetRecentByRestaurantAsync(int restaurantId, int limit, CancellationToken ct);
    Task<ImportAudit?> GetByIdAsync(Guid importId, int? restaurantId, CancellationToken ct);
}