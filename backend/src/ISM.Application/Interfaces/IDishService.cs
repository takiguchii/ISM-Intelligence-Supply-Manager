using ISM.Application.DTOs;

namespace ISM.Application.Interfaces;

public interface IDishService
{
    Task<DishResponse?> GetDishByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DishResponse>> GetAllDishesAsync(
        int? restaurantId = null,
        int? categoryId = null,
        CancellationToken cancellationToken = default);
    Task<DishResponse> CreateDishAsync(CreateDishRequest request, CancellationToken cancellationToken = default);
    Task<DishResponse?> UpdateDishAsync(int id, UpdateDishRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteDishAsync(int id, CancellationToken cancellationToken = default);
}