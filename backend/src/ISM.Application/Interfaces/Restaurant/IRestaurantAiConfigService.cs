using ISM.Application.DTOs.Restaurant;

namespace ISM.Application.Interfaces.Restaurant;

public interface IRestaurantAiConfigService
{
    Task<RestaurantAiPhotoConfigDto> GetByRestaurantIdAsync(int restaurantId, CancellationToken ct);
    Task UpdateByRestaurantIdAsync(int restaurantId, UpdateRestaurantAiPhotoConfigDto dto, CancellationToken ct);
    Task<TestRestaurantAiPhotoConfigResultDto> TestByRestaurantIdAsync(int restaurantId, TestRestaurantAiPhotoConfigRequestDto? req, CancellationToken ct);

    Task<RestaurantAiPhotoConfigDto> GetMyRestaurantAsync(int currentRestaurantId, CancellationToken ct) =>
        GetByRestaurantIdAsync(currentRestaurantId, ct);

    Task UpdateMyRestaurantAsync(int currentRestaurantId, UpdateRestaurantAiPhotoConfigDto dto, CancellationToken ct) =>
        UpdateByRestaurantIdAsync(currentRestaurantId, dto, ct);

    Task<TestRestaurantAiPhotoConfigResultDto> TestMyRestaurantAsync(int currentRestaurantId, TestRestaurantAiPhotoConfigRequestDto? req, CancellationToken ct) =>
        TestByRestaurantIdAsync(currentRestaurantId, req, ct);
}
