using ISM.Domain.Entities;
using ISM.Domain.Interfaces;
using ISM.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace ISM.Infrastructure.Repositories.Tenants;

public sealed class RestaurantRepository : IRestaurantRepository
{
    private readonly IsmDbContext _dbContext;

    public RestaurantRepository(IsmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ISM.Domain.Entities.Restaurant?> GetRestaurantByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Restaurants
            .FirstOrDefaultAsync(restaurant => restaurant.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<ISM.Domain.Entities.Restaurant>> GetAllRestaurantsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Restaurants.ToListAsync(cancellationToken);
    }

    public async Task AddRestaurantAsync(ISM.Domain.Entities.Restaurant restaurant, CancellationToken cancellationToken = default)
    {
        await _dbContext.Restaurants.AddAsync(restaurant, cancellationToken);
    }

    public void Update(ISM.Domain.Entities.Restaurant restaurant)
    {
        _dbContext.Restaurants.Update(restaurant);
    }

    public void Delete(ISM.Domain.Entities.Restaurant restaurant)
    {
        _dbContext.Restaurants.Remove(restaurant);
    }

    public async Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SaveChangesAsync(cancellationToken) > 0;
    }

    public async Task<bool> CanConnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbContext.Database.CanConnectAsync(cancellationToken);
        }
        catch
        {
            return false;
        }
    }
}