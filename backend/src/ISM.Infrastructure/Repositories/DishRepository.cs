using ISM.Domain.Modules.Menu.Entities;
using ISM.Domain.Modules.Menu.Interfaces;
using ISM.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace ISM.Infrastructure.Repositories;

public sealed class DishRepository : IDishRepository
{
    private readonly IsmDbContext _dbContext; 

    public DishRepository(IsmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Dish?> GetDishByIdAsync(int id, CancellationToken cancellationToken = default)
        => await _dbContext.Dishes 
            .AsNoTracking()
            .Include(dish => dish.Ingredients)
            .FirstOrDefaultAsync(dish => dish.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<Dish>> GetAllDishesAsync(int? restaurantId = null, int? categoryId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Dishes
            .AsNoTracking()
            .Include(dish => dish.Ingredients)
            .AsQueryable(); 

        if (restaurantId != null)
        {
            query = query.Where(dish => dish.RestaurantId == restaurantId);
        }

        if (categoryId != null)
        {
            query = query.Where(dish => dish.CategoryId == categoryId);
        }
        
        return await query
            .OrderBy(dish => dish.DisplayOrder)
            .ThenBy(dish => dish.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Dish> AddDishAsync(Dish dish, CancellationToken cancellationToken = default)
    {
        _dbContext.Dishes.Add(dish);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return dish;
    }

    public async Task<Dish?> UpdateDishAsync(int id, Dish dish, IReadOnlyCollection<DishIngredient> ingredients,
        CancellationToken cancellationToken = default)
    {
        
        var existing = await _dbContext.Dishes
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        if (existing == null)
        {
            return null;
        }

        existing.Name = dish.Name;
        existing.RestaurantId = dish.RestaurantId;
        existing.CategoryId = dish.CategoryId;
        existing.Description = dish.Description;
        existing.Price = dish.Price;
        existing.Cost = dish.Cost;
        existing.IsAtive = dish.IsAtive;
        existing.Highlight = dish.Highlight;
        existing.UrlImage = dish.UrlImage;
        existing.DisplayOrder = dish.DisplayOrder;
        existing.UpdatedAtUtc = DateTime.UtcNow;

        
        await _dbContext.DishIngredients
            .Where(ingredient => ingredient.DishId == id)
            .ExecuteDeleteAsync(cancellationToken);

        foreach (var ingredient in ingredients) 
        {
            ingredient.DishId = id;
            existing.Ingredients.Add(ingredient); 
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public async Task<bool> DeleteDishAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Dishes.FirstOrDefaultAsync(dish => dish.Id == id, cancellationToken);

        if (entity is null)
        {
            return false;
        }

        _dbContext.Dishes.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}