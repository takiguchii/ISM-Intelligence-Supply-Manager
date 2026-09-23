using ISM.Application.Interfaces;
using ISM.Application.DTOs;
using ISM.Application.Security;
using ISM.Domain.Modules.Menu.Entities;
using ISM.Domain.Interfaces;

namespace ISM.Application.Services.Menu;

public sealed class DishService : IDishService
{
    private readonly IDishRepository _dishRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IProductRepository _productRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IPlanEnforcer _planEnforcer;

    public DishService(
        IDishRepository dishRepository,
        ICategoryRepository categoryRepository,
        IProductRepository productRepository,
        ICurrentUser currentUser,
        IPlanEnforcer planEnforcer)
    {
        _dishRepository = dishRepository;
        _categoryRepository = categoryRepository;
        _productRepository = productRepository;
        _currentUser = currentUser;
        _planEnforcer = planEnforcer;
    }

    public async Task<DishResponse?> GetDishByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var dish = await _dishRepository.GetDishByIdAsync(id, cancellationToken);
        if (dish is null) return null;

        if (!_currentUser.IsSuperAdmin && _currentUser.RestaurantId.HasValue)
        {
            if (dish.RestaurantId != _currentUser.RestaurantId.Value)
                return null;
        }

        return Map(dish);
    }

    public async Task<IReadOnlyList<DishResponse>> GetAllDishesAsync(
        int? restaurantId = null,
        int? categoryId = null,
        CancellationToken cancellationToken = default)
    {
        var targetRestaurantId = (!_currentUser.IsSuperAdmin && _currentUser.RestaurantId.HasValue)
            ? _currentUser.RestaurantId.Value
            : restaurantId;

        var dishes = await _dishRepository.GetAllDishesAsync(targetRestaurantId, categoryId, cancellationToken);
        return dishes.Select(Map).ToArray();
    }

    public async Task<DishResponse> CreateDishAsync(CreateDishRequest request, CancellationToken cancellationToken = default)
    {
        var restaurantId = ResolveRestaurantId(request.RestaurantId);

        if (!_currentUser.IsSuperAdmin && !_currentUser.IsManagerOrAbove)
            throw new UnauthorizedAccessException("Apenas gerentes podem criar pratos.");

        var category = await _categoryRepository.GetCategoryByIdAsync(request.CategoryId, cancellationToken)
            ?? throw new InvalidOperationException($"Categoria com ID {request.CategoryId} não existe.");

        if (category.RestaurantId != restaurantId)
            throw new InvalidOperationException("Categoria não pertence ao restaurante informado.");

        foreach (var ing in request.Ingredients)
        {
            var product = await _productRepository.GetByIdAsync(ing.ProductId, cancellationToken);
            if (product == null)
                throw new InvalidOperationException($"Produto com ID {ing.ProductId} não existe.");
            if (product.RestaurantId != restaurantId)
                throw new InvalidOperationException($"Produto '{product.Name}' não pertence ao restaurante.");
        }

        await _planEnforcer.AssertCanAddDishAsync(restaurantId, cancellationToken);

        var dish = new Dish
        {
            RestaurantId = restaurantId,
            CategoryId = request.CategoryId,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim() ?? string.Empty,
            Price = request.Price,
            Cost = request.Cost,
            IsActive = request.IsActive,
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
        var existing = await _dishRepository.GetDishByIdAsync(id, cancellationToken);
        if (existing is null) return null;

        if (!_currentUser.IsSuperAdmin && _currentUser.RestaurantId.HasValue)
        {
            if (existing.RestaurantId != _currentUser.RestaurantId.Value)
                return null;
            if (!_currentUser.IsManagerOrAbove)
                throw new UnauthorizedAccessException("Apenas gerentes podem editar pratos.");
        }

        var restaurantId = existing.RestaurantId;

        var category = await _categoryRepository.GetCategoryByIdAsync(request.CategoryId, cancellationToken)
            ?? throw new InvalidOperationException($"Categoria com ID {request.CategoryId} não existe.");
        if (category.RestaurantId != restaurantId)
            throw new InvalidOperationException("Categoria não pertence ao restaurante.");

        foreach (var ing in request.Ingredients)
        {
            var product = await _productRepository.GetByIdAsync(ing.ProductId, cancellationToken);
            if (product == null)
                throw new InvalidOperationException($"Produto com ID {ing.ProductId} não existe.");
            if (product.RestaurantId != restaurantId)
                throw new InvalidOperationException($"Produto de ID {ing.ProductId} não pertence ao restaurante.");
        }

        var dish = new Dish
        {
            RestaurantId = restaurantId,
            CategoryId = request.CategoryId,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim() ?? string.Empty,
            Price = request.Price,
            Cost = request.Cost,
            IsActive = request.IsActive,
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

    public async Task<bool> DeleteDishAsync(int id, CancellationToken cancellationToken = default)
    {
        var existing = await _dishRepository.GetDishByIdAsync(id, cancellationToken);
        if (existing is null) return false;

        if (!_currentUser.IsSuperAdmin && _currentUser.RestaurantId.HasValue)
        {
            if (existing.RestaurantId != _currentUser.RestaurantId.Value)
                return false;
            if (!_currentUser.IsManagerOrAbove)
                throw new UnauthorizedAccessException("Apenas gerentes podem deletar pratos.");
        }

        return await _dishRepository.DeleteDishAsync(id, cancellationToken);
    }

    private int ResolveRestaurantId(int requestRestaurantId)
    {
        if (_currentUser.IsSuperAdmin)
        {
            if (requestRestaurantId <= 0)
                return _currentUser.RestaurantId ?? 1;
            return requestRestaurantId;
        }

        if (!_currentUser.RestaurantId.HasValue)
            throw new UnauthorizedAccessException("Usuário não vinculado a restaurante.");

        if (requestRestaurantId > 0 && requestRestaurantId != _currentUser.RestaurantId.Value)
            throw new UnauthorizedAccessException("Você só pode criar pratos no seu restaurante.");

        return _currentUser.RestaurantId.Value;
    }

    private static DishResponse Map(Dish dish)
        => new(
            dish.Id,
            dish.RestaurantId,
            dish.CategoryId,
            dish.Name,
            dish.Description,
            dish.Price,
            dish.Cost,
            dish.IsActive,
            dish.Highlight,
            dish.UrlImage,
            dish.DisplayOrder,
            dish.CreatedAtUtc,
            dish.UpdatedAtUtc,
            dish.Ingredients
                .Select(ingredient => new DishIngredientResponse(ingredient.ProductId, ingredient.Quantity))
                .ToArray());
}
