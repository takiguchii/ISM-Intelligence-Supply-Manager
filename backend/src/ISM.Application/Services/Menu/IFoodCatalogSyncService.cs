using ISM.Application.DTOs.Menu;
using ISM.Application.Interfaces.Integrations.IFood;
using ISM.Application.Interfaces.Menu;
using ISM.Domain.Interfaces;

namespace ISM.Application.Services.Menu;

public sealed class IFoodCatalogSyncService : IIFoodCatalogSyncService
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IDishRepository _dishRepository;
    private readonly IIFoodCatalogClient _catalogClient;

    public IFoodCatalogSyncService(ICategoryRepository categoryRepository, IDishRepository dishRepository, IIFoodCatalogClient catalogClient)
    {
        _categoryRepository = categoryRepository;
        _dishRepository = dishRepository;
        _catalogClient = catalogClient;
    }

    public async Task<IFoodCatalogSyncResultDto> SyncRestaurantCatalogAsync(int restaurantId, CancellationToken ct)
    {
        var categoriesSynced = 0;
        var categories = await _categoryRepository.GetAllCategoriesAsync(restaurantId, ct);
        var categoriesById = categories.ToDictionary(c => c.Id);

        foreach (var category in categories.Where(c => c.IFoodCategoryId is null))
        {
            var ifoodId = await _catalogClient.UpsertCategoryAsync(restaurantId, category.Name, ct);
            await _categoryRepository.UpdateIFoodCategoryIdAsync(category.Id, ifoodId, ct);
            categoriesById[category.Id].IFoodCategoryId = ifoodId;
            categoriesSynced++;
        }

        var itemsSynced = 0;
        var dishes = await _dishRepository.GetAllDishesAsync(restaurantId, cancellationToken: ct);
        
        foreach (var dish in dishes.Where(d => d.IFoodItemId is null && d.IsActive))
        {
            if (!categoriesById.TryGetValue(dish.CategoryId, out var category) || category.IFoodCategoryId is null)
                continue;
            
            var ifoodId = await _catalogClient.UpsertItemAsync(restaurantId, category.IFoodCategoryId, dish.Name, dish.Description, dish.Price, ct);
            await _dishRepository.UpdateIFoodItemIdAsync(dish.Id, ifoodId, ct);
            itemsSynced++;
        }

        return new IFoodCatalogSyncResultDto { CategoriesSynced = categoriesSynced, ItemsSynced = itemsSynced };
    }

    public async Task<IFoodCatalogStatusDto> GetStatusAsync(int restaurantId, CancellationToken ct)
    {
        var categories = await _categoryRepository.GetAllCategoriesAsync(restaurantId, ct);
        var dishes = await _dishRepository.GetAllDishesAsync(restaurantId, cancellationToken: ct);

        return new IFoodCatalogStatusDto
        {
            TotalCategories = categories.Count,
            CategoriesSynced = categories.Count(c => c.IFoodCategoryId is not null),
            TotalDishes = dishes.Count(d => d.IsActive),
            DishesSynced = dishes.Count(d => d.IsActive && d.IFoodItemId is not null)
        };
    } 
}