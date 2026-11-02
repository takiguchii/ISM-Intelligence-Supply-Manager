using ISM.Domain.Interfaces;
using ISM.Domain.Modules.Finance;
using ISM.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;
namespace ISM.Infrastructure.Repositories.Finance;
public sealed class FinanceEntryRepository(IsmDbContext db) : IFinanceEntryRepository
{
    public async Task<IReadOnlyList<FinanceEntry>> GetAllAsync(int restaurantId, CancellationToken cancellationToken = default) =>
        await db.FinanceEntries.AsNoTracking().Where(entry => entry.RestaurantId == restaurantId).OrderByDescending(entry => entry.DueDate).ToListAsync(cancellationToken);
    public async Task<IReadOnlyList<FinanceEntry>> GetPageAsync(int restaurantId, int pageNumber, int pageSize, CancellationToken cancellationToken = default) =>
        await db.FinanceEntries.AsNoTracking().Where(entry => entry.RestaurantId == restaurantId)
            .OrderByDescending(entry => entry.DueDate).ThenByDescending(entry => entry.Id)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
    public Task<int> CountAsync(int restaurantId, CancellationToken cancellationToken = default) =>
        db.FinanceEntries.AsNoTracking().CountAsync(entry => entry.RestaurantId == restaurantId, cancellationToken);
    public Task<FinanceEntry?> GetByIdAsync(int restaurantId, int id, CancellationToken cancellationToken = default) =>
        db.FinanceEntries.FirstOrDefaultAsync(entry => entry.RestaurantId == restaurantId && entry.Id == id, cancellationToken);
    public async Task AddAsync(FinanceEntry entry, CancellationToken cancellationToken = default) { db.FinanceEntries.Add(entry); await db.SaveChangesAsync(cancellationToken); }
    public async Task SaveAsync(CancellationToken cancellationToken = default) => await db.SaveChangesAsync(cancellationToken);
    public async Task DeleteAsync(FinanceEntry entry, CancellationToken cancellationToken = default) { db.FinanceEntries.Remove(entry); await db.SaveChangesAsync(cancellationToken); }
}
