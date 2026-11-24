using ISM.Application.DTOs.Menu;

namespace ISM.Application.Interfaces.Menu;

public interface IIFoodCatalogSyncService
{
    Task<IFoodCatalogSyncResultDto> SyncRestaurantCatalogAsync(int restaurantId, CancellationToken ct);
    Task<IFoodCatalogStatusDto> GetStatusAsync(int restaurantId, CancellationToken ct);
}