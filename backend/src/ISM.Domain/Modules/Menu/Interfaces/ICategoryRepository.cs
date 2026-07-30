using ISM.Domain.Modules.Menu.Entities;

namespace ISM.Domain.Modules.Menu.Interfaces;

public interface ICategoryRepository
{
    Task<Category?> GetCategoryByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Category>> GetAllCategoriesAsync(int? restaurantId = null, CancellationToken cancellationToken = default);
    Task<Category> AddCategoryAsync(Category category, CancellationToken cancellationToken = default);
    Task<Category> UpdateCategoryAsync(Category category, CancellationToken cancellationToken = default);
    Task<bool> DeleteCategoryAsync(int id, CancellationToken cancellationToken = default);
}