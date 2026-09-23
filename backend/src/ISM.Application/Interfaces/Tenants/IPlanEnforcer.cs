namespace ISM.Application.Interfaces;

public interface IPlanEnforcer
{
    Task AssertCanAddUserAsync(int restaurantId, CancellationToken ct = default);
    Task AssertCanAddCategoryAsync(int restaurantId, CancellationToken ct = default);
    Task AssertCanAddProductAsync(int restaurantId, CancellationToken ct = default);
    Task AssertCanAddDishAsync(int restaurantId, CancellationToken ct = default);
    Task<(bool IsActive, string? Message)> ValidateRestaurantAccessAsync(int? restaurantId, CancellationToken ct = default);
}
