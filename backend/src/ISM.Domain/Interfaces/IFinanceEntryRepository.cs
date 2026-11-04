using ISM.Domain.Modules.Finance;
namespace ISM.Domain.Interfaces;
public interface IFinanceEntryRepository
{
    Task<IReadOnlyList<FinanceEntry>> GetAllAsync(int restaurantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FinanceEntry>> GetPageAsync(int restaurantId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<int> CountAsync(int restaurantId, CancellationToken cancellationToken = default);
    Task<FinanceEntry?> GetByIdAsync(int restaurantId, int id, CancellationToken cancellationToken = default);
    Task AddAsync(FinanceEntry entry, CancellationToken cancellationToken = default);
    Task SaveAsync(CancellationToken cancellationToken = default);
    Task DeleteAsync(FinanceEntry entry, CancellationToken cancellationToken = default);
}
