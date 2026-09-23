using ISM.Domain.Modules.DataImport;
using ISM.Domain.Interfaces;
using ISM.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace ISM.Infrastructure.Repositories.DataImport;

public sealed class ImportAuditRepository : IImportAuditRepository
{
    private readonly IsmDbContext _ctx;

    public ImportAuditRepository(IsmDbContext ctx) => _ctx = ctx;

    public async Task<ImportAudit> StartImportAsync(ImportAudit audit, CancellationToken ct)
    {
        await _ctx.ImportAudits.AddAsync(audit, ct);
        await _ctx.SaveChangesAsync(ct);
        return audit;
    }

    public async Task FinishImportAsync(Guid importId, int succeeded, int failed, string? lineageJson, CancellationToken ct)
    {
        var audit = await _ctx.ImportAudits.FirstOrDefaultAsync(a => a.ImportId == importId, ct);
        if (audit == null) return;

        audit.RecordsSucceeded = succeeded;
        audit.RecordsFailed = failed;
        audit.FinishedAtUtc = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(lineageJson))
            audit.LineageSerializedJson = lineageJson;

        await _ctx.SaveChangesAsync(ct);
    }

    public async Task AppendErrorsAsync(Guid importId, IEnumerable<ImportErrorLog> errors, CancellationToken ct)
    {
        var list = errors.Select(e =>
        {
            e.ImportId = importId;
            e.CreatedAtUtc = DateTime.UtcNow;
            return e;
        }).ToList();
        if (list.Count == 0) return;
        await _ctx.ImportErrorLogs.AddRangeAsync(list, ct);
        await _ctx.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<ImportAudit>> GetRecentByRestaurantAsync(int restaurantId, int limit, CancellationToken ct)
    {
        var q = _ctx.ImportAudits.AsNoTracking();
        if (restaurantId > 0)
            q = q.Where(a => a.RestaurantId == restaurantId);
        return await q.OrderByDescending(a => a.ReceivedAtUtc)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task<ImportAudit?> GetByIdAsync(Guid importId, int? restaurantId, CancellationToken ct)
    {
        var q = _ctx.ImportAudits.AsNoTracking()
            .Include(a => a.Errors)
            .AsQueryable();
        q = restaurantId.HasValue
            ? q.Where(a => a.RestaurantId == restaurantId.Value && a.ImportId == importId)
            : q.Where(a => a.ImportId == importId);
        return await q.FirstOrDefaultAsync(ct);
    }
}