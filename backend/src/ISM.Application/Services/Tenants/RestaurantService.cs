using ISM.Application.DTOs;
using ISM.Application.Interfaces;
using ISM.Domain.Interfaces;
using RestaurantEntity = ISM.Domain.Entities.Restaurant;

namespace ISM.Application.Services.Tenants;

public sealed class RestaurantService : IRestaurantService
{
    private readonly IRestaurantRepository _restaurantRepository;

    public RestaurantService(IRestaurantRepository restaurantRepository)
    {
        _restaurantRepository = restaurantRepository;
    }

    public async Task<RestaurantDto?> GetRestaurantByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var restaurant = await _restaurantRepository.GetRestaurantByIdAsync(id, cancellationToken);
        if (restaurant == null)
            return null;

        return MapToDto(restaurant);
    }

    public async Task<IReadOnlyList<RestaurantDto>> GetAllRestaurantsAsync(CancellationToken cancellationToken = default)
    {
        var restaurants = await _restaurantRepository.GetAllRestaurantsAsync(cancellationToken);
        return restaurants.Select(MapToDto).ToList();
    }

    public async Task<RestaurantDto> CreateRestaurantAsync(RestaurantDto dto, int? planId = null, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var restaurant = new RestaurantEntity
        {
            Name = dto.Name,
            CNPJ = dto.CNPJ,
            Created = now,
            CreatedAtUtc = now,
            PlanId = planId ?? 1,
            IsPlanActive = true,
            TrialEndAtUtc = now.AddDays(14)
        };

        await _restaurantRepository.AddRestaurantAsync(restaurant, cancellationToken);
        await _restaurantRepository.SaveChangesAsync(cancellationToken);

        return MapToDto(restaurant);
    }

    public async Task<bool> UpdateAsync(int id, RestaurantDto dto, CancellationToken cancellationToken = default)
    {
        var restaurant = await _restaurantRepository.GetRestaurantByIdAsync(id, cancellationToken);
        if (restaurant == null)
            return false;

        restaurant.Name = dto.Name;
        restaurant.CNPJ = dto.CNPJ;
        restaurant.UpdatedAtUtc = DateTime.UtcNow;

        _restaurantRepository.Update(restaurant);
        return await _restaurantRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var restaurant = await _restaurantRepository.GetRestaurantByIdAsync(id, cancellationToken);
        if (restaurant == null)
            return false;

        _restaurantRepository.Delete(restaurant);
        return await _restaurantRepository.SaveChangesAsync(cancellationToken);
    }

    private static RestaurantDto MapToDto(RestaurantEntity restaurant)
    {
        return new RestaurantDto
        {
            Id = restaurant.Id,
            Name = restaurant.Name,
            CNPJ = restaurant.CNPJ
        };
    }
}