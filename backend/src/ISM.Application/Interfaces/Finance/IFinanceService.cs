using ISM.Application.DTOs.Finance;
namespace ISM.Application.Interfaces.Finance;
public interface IFinanceService
{
    Task<FinanceOverview> GetAsync(CancellationToken cancellationToken = default);
    Task<FinanceEntryResponse> CreateAsync(FinanceEntryRequest request, CancellationToken cancellationToken = default);
    Task<FinanceEntryResponse?> UpdateAsync(int id, FinanceEntryRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
