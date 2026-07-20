using ISM.Domain.Modules.Menu.Interfaces;
using ISM.Domain.Modules.Menu.Entities;
using ISM.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;


namespace ISM.Infrastructure.Repositories;

public sealed class CategoryRepository : ICategoryRepository
{
    private readonly IsmDbContext _dbContext;

    public CategoryRepository(IsmDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public async Task<Category?> GetCategoryByIdAsync(int id, CancellationToken cancellationToken = default)
        => await _dbContext.Categories
            .AsNoTracking() 
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<Category>> GetAllCategoriesAsync(int? restaurantId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Categories.AsNoTracking();

        if (restaurantId != null)
        {
            query = query.Where(category => category.RestaurantId == restaurantId);
        }
        
        return await query
            .OrderBy(category => category.DisplayOrder)
            .ThenBy(category => category.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Category> AddCategoryAsync(Category category, CancellationToken cancellationToken = default)
    {
        _dbContext.Categories.Add(category);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return category;
    }

    public async Task<Category> UpdateCategoryAsync(Category category, CancellationToken cancellationToken = default)
    {
        _dbContext.Categories.Update(category);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return category;
    }

    public async Task<bool> DeleteCategoryAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Categories
            .FirstOrDefaultAsync(category => category.Id == id, cancellationToken);

        if (entity == null)
        {
            return false;
        }

        _dbContext.Categories.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}