using ISM.Application.Interfaces;
using ISM.Application.DTOs;
using ISM.Application.Security;
using ISM.Domain.Modules.Menu.Entities;
using ISM.Domain.Modules.Menu.Interfaces;

namespace ISM.Application.Services;

public sealed class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IPlanEnforcer _planEnforcer;

    public CategoryService(
        ICategoryRepository categoryRepository,
        ICurrentUser currentUser,
        IPlanEnforcer planEnforcer)
    {
        _categoryRepository = categoryRepository;
        _currentUser = currentUser;
        _planEnforcer = planEnforcer;
    }

    public async Task<CategoryResponse?> GetCategoryByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepository.GetCategoryByIdAsync(id, cancellationToken);
        if (category is null) return null;

        if (!_currentUser.IsSuperAdmin && _currentUser.RestaurantId.HasValue)
        {
            if (category.RestaurantId != _currentUser.RestaurantId.Value)
                return null;
        }

        return Map(category);
    }

    public async Task<IReadOnlyList<CategoryResponse>> GetAllCategoriesAsync(int? restaurantId = null, CancellationToken cancellationToken = default)
    {
        var categories = await _categoryRepository.GetAllCategoriesAsync(restaurantId, cancellationToken);
        if (!_currentUser.IsSuperAdmin && _currentUser.RestaurantId.HasValue)
        {
            categories = categories.Where(c => c.RestaurantId == _currentUser.RestaurantId.Value).ToList();
        }
        return categories.Select(Map).ToArray();
    }

    public async Task<CategoryResponse> CreateCategoryAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var restaurantId = ResolveRestaurantId(request.RestaurantId);

        if (!_currentUser.IsSuperAdmin && !_currentUser.IsManagerOrAbove)
            throw new UnauthorizedAccessException("Apenas gerentes podem criar categorias.");

        await _planEnforcer.AssertCanAddCategoryAsync(restaurantId, cancellationToken);

        var category = new Category
        {
            RestaurantId = restaurantId,
            Name = request.Name.Trim(),
            IsActive = request.IsActive,
            DisplayOrder = request.DisplayOrder,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _categoryRepository.AddCategoryAsync(category, cancellationToken);
        return Map(category);
    }

    public async Task<CategoryResponse?> UpdateCategoryAsync(int id, UpdateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _categoryRepository.GetCategoryByIdAsync(id, cancellationToken);
        if (existing is null) return null;

        if (!_currentUser.IsSuperAdmin && _currentUser.RestaurantId.HasValue)
        {
            if (existing.RestaurantId != _currentUser.RestaurantId.Value)
                return null;
            if (!_currentUser.IsManagerOrAbove)
                throw new UnauthorizedAccessException("Apenas gerentes podem editar categorias.");
        }

        var updated = new Category
        {
            Id = id,
            RestaurantId = existing.RestaurantId,
            Name = request.Name.Trim(),
            IsActive = request.IsActive,
            DisplayOrder = request.DisplayOrder,
            CreatedAtUtc = existing.CreatedAtUtc,
            UpdatedAtUtc = DateTime.UtcNow
        };

        await _categoryRepository.UpdateCategoryAsync(updated, cancellationToken);
        return Map(updated);
    }

    public async Task<bool> DeleteCategoryAsync(int id, CancellationToken cancellationToken = default)
    {
        var existing = await _categoryRepository.GetCategoryByIdAsync(id, cancellationToken);
        if (existing is null) return false;

        if (!_currentUser.IsSuperAdmin && _currentUser.RestaurantId.HasValue)
        {
            if (existing.RestaurantId != _currentUser.RestaurantId.Value)
                return false;
            if (!_currentUser.IsManagerOrAbove)
                throw new UnauthorizedAccessException("Apenas gerentes podem deletar categorias.");
        }

        return await _categoryRepository.DeleteCategoryAsync(id, cancellationToken);
    }

    private int ResolveRestaurantId(int requestRestaurantId)
    {
        if (_currentUser.IsSuperAdmin)
        {
            if (requestRestaurantId <= 0)
                throw new InvalidOperationException("Super Admin deve informar o RestaurantId.");
            return requestRestaurantId;
        }

        if (!_currentUser.RestaurantId.HasValue)
            throw new UnauthorizedAccessException("Usuário não vinculado a restaurante.");

        if (requestRestaurantId > 0 && requestRestaurantId != _currentUser.RestaurantId.Value)
            throw new UnauthorizedAccessException("Você só pode criar categorias no seu restaurante.");

        return _currentUser.RestaurantId.Value;
    }

    private static CategoryResponse Map(Category category)
        => new(
            category.Id,
            category.RestaurantId,
            category.Name,
            category.IsActive,
            category.DisplayOrder,
            category.CreatedAtUtc,
            category.UpdatedAtUtc);
}
