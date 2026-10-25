using ISM.Application.DTOs.Dashboard;
namespace ISM.Application.Interfaces.Dashboard;
public interface IDashboardService
{
    Task<DashboardResponse> GetAsync(int? restaurantId, CancellationToken cancellationToken = default);
}
