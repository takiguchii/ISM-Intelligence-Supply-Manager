using ISM.Application.DTOs.Finance;
namespace ISM.Application.Interfaces.Finance;
public interface IFinanceService
{
    Task<FinanceOverview> GetAsync(int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default);
    Task<FinanceEntryResponse> CreateAsync(FinanceEntryRequest request, CancellationToken cancellationToken = default);
    Task<FinanceEntryResponse?> UpdateAsync(int id, FinanceEntryRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
