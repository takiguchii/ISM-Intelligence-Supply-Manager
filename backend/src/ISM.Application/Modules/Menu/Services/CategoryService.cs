using ISM.Application.Abstractions;
using ISM.Application.DTOs;
using ISM.Domain.Modules.Menu.Entities;
using ISM.Domain.Modules.Menu.Interfaces;

namespace ISM.Application.Modules.Menu.Services;

public sealed class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;

    public CategoryService(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<CategoryResponse?> GetCategoryByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepository.GetCategoryByIdAsync(id, cancellationToken);
        return category is null ? null : Map(category);
    }

    public async Task<IReadOnlyList<CategoryResponse>> GetAllCategoriesAsync(int? restaurantId = null, CancellationToken cancellationToken = default)
    {
        var categories = await _categoryRepository.GetAllCategoriesAsync(restaurantId, cancellationToken);
        return categories.Select(Map).ToArray();
    }

    public async Task<CategoryResponse> CreateCategoryAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var category = new Category
        {
            RestaurantId = request.RestaurantId,
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
        if (existing is null)
        {
            return null;
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

    public Task<bool> DeleteCategoryAsync(int id, CancellationToken cancellationToken = default)
        => _categoryRepository.DeleteCategoryAsync(id, cancellationToken);

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