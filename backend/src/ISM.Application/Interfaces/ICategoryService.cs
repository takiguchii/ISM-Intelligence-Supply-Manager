using ISM.Application.DTOs;

namespace ISM.Application.Interfaces;

public interface ICategoryService
{
    Task<CategoryResponse?> GetCategoryByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CategoryResponse>> GetAllCategoriesAsync(int? restaurantId = null, CancellationToken cancellationToken = default);
    Task<CategoryResponse> CreateCategoryAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default);
    Task<CategoryResponse?> UpdateCategoryAsync(int id, UpdateCategoryRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteCategoryAsync(int id, CancellationToken cancellationToken = default);
}