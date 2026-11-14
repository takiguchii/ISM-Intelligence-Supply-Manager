using System.Diagnostics;
using ISM.Application.DTOs.Restaurant;
using ISM.Application.Interfaces.Integrations.IFood;
using ISM.Application.Interfaces.Restaurant;
using ISM.Domain.Entities;
using ISM.Domain.Interfaces;

namespace ISM.Application.Services.Restaurant;

public sealed class IFoodConfigService : IIFoodConfigService
{
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly IIFoodAuthClient _authClient;

    public IFoodConfigService(IRestaurantRepository restaurantRepository, IIFoodAuthClient authClient)
    {
        _restaurantRepository = restaurantRepository;
        _authClient = authClient;
    }

    public async Task<IFoodConfigDto> GetMyConfigAsync(int restaurantId, CancellationToken ct)
    {
        var restaurant = await GetOrThrowAsync(restaurantId, ct);
        var hasCredentials = !string.IsNullOrWhiteSpace(restaurant.IFoodClientId)
            && !string.IsNullOrWhiteSpace(restaurant.IFoodClientSecret);

        return new IFoodConfigDto
        {
            HasCredentials = hasCredentials,
            MerchantId = restaurant.IFoodMerchantId,
            ClientIdLast4 = hasCredentials && restaurant.IFoodClientId!.Length >= 4
                ? restaurant.IFoodClientId[^4..]
                : null,
        };
    }

    public async Task UpdateMyConfigAsync(int restaurantId, UpdateIFoodConfigDto dto, CancellationToken ct)
    {
        var restaurant = await GetOrThrowAsync(restaurantId, ct);

        if (dto.ClearCredentials)
        {
            restaurant.IFoodClientId = null;
            restaurant.IFoodClientSecret = null;
            restaurant.IFoodMerchantId = null;
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(dto.ClientId)) restaurant.IFoodClientId = dto.ClientId.Trim();
            if (!string.IsNullOrWhiteSpace(dto.ClientSecret)) restaurant.IFoodClientSecret = dto.ClientSecret.Trim();
            if (!string.IsNullOrWhiteSpace(dto.MerchantId)) restaurant.IFoodMerchantId = dto.MerchantId.Trim();
        }
        
        _authClient.InvalidateCachedToken(restaurantId); //atualiza o token
        restaurant.UpdatedAtUtc =  DateTime.UtcNow;
        _restaurantRepository.Update(restaurant);
        await _restaurantRepository.SaveChangesAsync(ct);
    }

    public async Task<TestIFoodConfigResultDto> TestMyConfigAsync(int restaurantId, CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            _authClient.InvalidateCachedToken(restaurantId);
            await _authClient.GetAccessTokenAsync(restaurantId, ct);
            return new TestIFoodConfigResultDto
                { Success = true, TokenObtained = true, LatencyMs = stopwatch.ElapsedMilliseconds };

        }
        catch (Exception ex)
        {
            return new TestIFoodConfigResultDto {Success = false, TokenObtained = false, ErrorMessage = ex.Message, LatencyMs = stopwatch.ElapsedMilliseconds };
        }
    }
    
    private async Task<ISM.Domain.Entities.Restaurant> GetOrThrowAsync(int restaurantId, CancellationToken ct) =>
        await _restaurantRepository.GetRestaurantByIdAsync(restaurantId, ct)
            ?? throw new InvalidOperationException($"Restaurante {restaurantId} não encontrado.");
}