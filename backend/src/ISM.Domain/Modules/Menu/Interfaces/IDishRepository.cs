using ISM.Domain.Modules.Menu.Entities;

namespace ISM.Domain.Modules.Menu.Interfaces;

public interface IDishRepository
{
    Task<Dish?> GetDishByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Dish>> GetAllDishesAsync(int? restaurantId = null, int? categoryId = null, CancellationToken cancellationToken = default);
    Task<Dish> AddDishAsync(Dish dish, CancellationToken cancellationToken = default);
    Task<Dish?> UpdateDishAsync(int id, Dish dish, IReadOnlyCollection<DishIngredient> ingredients, CancellationToken cancellationToken = default);
    Task<bool> DeleteDishAsync(int id, CancellationToken cancellationToken = default);
}