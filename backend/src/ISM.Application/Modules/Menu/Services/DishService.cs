using ISM.Application.Abstractions;
using ISM.Application.DTOs;
using ISM.Domain.Modules.Menu.Entities;
using ISM.Domain.Modules.Menu.Interfaces;

namespace ISM.Application.Modules.Menu.Services;

public sealed class DishService : IDishService
{
    private readonly IDishRepository _dishRepository;

    public DishService(IDishRepository dishRepository)
    {
        _dishRepository = dishRepository;
    }

    public async Task<DishResponse?> GetDishByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var dish = await _dishRepository.GetDishByIdAsync(id, cancellationToken);
        return dish is null ? null : Map(dish);
    }

    public async Task<IReadOnlyList<DishResponse>> GetAllDishesAsync(
        int? restaurantId = null,
        int? categoryId = null,
        CancellationToken cancellationToken = default)
    {
        var dishes = await _dishRepository.GetAllDishesAsync(restaurantId, categoryId, cancellationToken);
        return dishes.Select(Map).ToArray();
    }

    public async Task<DishResponse> CreateDishAsync(CreateDishRequest request, CancellationToken cancellationToken = default)
    {
        var dish = new Dish
        {
            RestaurantId = request.RestaurantId,
            CategoryId = request.CategoryId,
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            Price = request.Price,
            Cost = request.Cost,
            IsAtive = request.IsAtive,
            Highlight = request.Highlight,
            UrlImage = request.UrlImage,
            DisplayOrder = request.DisplayOrder,
            CreatedAtUtc = DateTime.UtcNow,
            Ingredients = request.Ingredients
                .Select(ingredient => new DishIngredient
                {
                    ProductId = ingredient.ProductId,
                    Quantity = ingredient.Quantity
                })
                .ToList()
        };

        await _dishRepository.AddDishAsync(dish, cancellationToken);
        return Map(dish);
    }

    public async Task<DishResponse?> UpdateDishAsync(int id, UpdateDishRequest request, CancellationToken cancellationToken = default)
    {
        var dish = new Dish
        {
            RestaurantId = request.RestaurantId,
            CategoryId = request.CategoryId,
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            Price = request.Price,
            Cost = request.Cost,
            IsAtive = request.IsAtive,
            Highlight = request.Highlight,
            UrlImage = request.UrlImage,
            DisplayOrder = request.DisplayOrder
        };

        var ingredients = request.Ingredients
            .Select(ingredient => new DishIngredient
            {
                ProductId = ingredient.ProductId,
                Quantity = ingredient.Quantity
            })
            .ToList();

        var updated = await _dishRepository.UpdateDishAsync(id, dish, ingredients, cancellationToken);
        return updated is null ? null : Map(updated);
    }

    public Task<bool> DeleteDishAsync(int id, CancellationToken cancellationToken = default)
        => _dishRepository.DeleteDishAsync(id, cancellationToken);

    private static DishResponse Map(Dish dish)
        => new(
            dish.Id,
            dish.RestaurantId,
            dish.CategoryId,
            dish.Name,
            dish.Description,
            dish.Price,
            dish.Cost,
            dish.IsAtive,
            dish.Highlight,
            dish.UrlImage,
            dish.DisplayOrder,
            dish.CreatedAtUtc,
            dish.UpdatedAtUtc,
            dish.Ingredients
                .Select(ingredient => new DishIngredientResponse(ingredient.ProductId, ingredient.Quantity))
                .ToArray());
}