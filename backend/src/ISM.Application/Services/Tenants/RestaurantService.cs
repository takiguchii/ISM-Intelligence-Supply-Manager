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

        if (!TimeZoneInfo.TryFindSystemTimeZoneById(dto.TimeZone, out _))
            throw new ArgumentException("Fuso horário inválido.");
        restaurant.Name = dto.Name.Trim();
        restaurant.CNPJ = dto.CNPJ;
        restaurant.TimeZone = dto.TimeZone;
        restaurant.Currency = dto.Currency;
        restaurant.Language = dto.Language;
        restaurant.Address = dto.Address;
        restaurant.Phone = dto.Phone;
        restaurant.ContactEmail = dto.ContactEmail;
        restaurant.OpeningHours = dto.OpeningHours;
        restaurant.LogoDataUrl = dto.LogoDataUrl;
        restaurant.ServiceFeePercent = dto.ServiceFeePercent;
        restaurant.DeliveryFee = dto.DeliveryFee;
        restaurant.PackagingFee = dto.PackagingFee;

        restaurant.UpdatedAtUtc = DateTime.UtcNow;

        _restaurantRepository.Update(restaurant);
        await _restaurantRepository.SaveChangesAsync(cancellationToken);
        return true;
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
            CNPJ = restaurant.CNPJ,
            TimeZone = restaurant.TimeZone,
            Currency = restaurant.Currency,
            Language = restaurant.Language,
            Address = restaurant.Address,
            Phone = restaurant.Phone,
            ContactEmail = restaurant.ContactEmail,
            OpeningHours = restaurant.OpeningHours,
            LogoDataUrl = restaurant.LogoDataUrl,
            ServiceFeePercent = restaurant.ServiceFeePercent,
            DeliveryFee = restaurant.DeliveryFee,
            PackagingFee = restaurant.PackagingFee,
            PlanName = restaurant.Plan?.Name,
            MonthlyPrice = restaurant.Plan?.MonthlyPrice ?? 0,
            IsPlanActive = restaurant.IsPlanActive,
            TrialEndAtUtc = restaurant.TrialEndAtUtc
        };
    }
}