using ISM.Application.DTOs;

namespace ISM.Application.Interfaces;

public interface IRestaurantService
{
    Task<RestaurantDto?> GetRestaurantByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RestaurantDto>> GetAllRestaurantsAsync(CancellationToken cancellationToken = default);
    Task<RestaurantDto> CreateRestaurantAsync(RestaurantDto dto, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(int id, RestaurantDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}