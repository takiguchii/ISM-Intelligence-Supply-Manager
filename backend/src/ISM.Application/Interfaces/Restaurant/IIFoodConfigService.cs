using ISM.Application.DTOs.Restaurant;

namespace ISM.Application.Interfaces.Restaurant;

public interface IIFoodConfigService
{
    Task<IFoodConfigDto> GetMyConfigAsync(int restaurantId, CancellationToken ct);
    Task UpdateMyConfigAsync(int restaurantId, UpdateIFoodConfigDto dto, CancellationToken ct);
    Task<TestIFoodConfigResultDto> TestMyConfigAsync(int  restaurantId, CancellationToken ct);
}