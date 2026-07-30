using ISM.Domain.Entities;
using ISM.Domain.Interfaces;
using ISM.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace ISM.Infrastructure.Repositories;

public sealed class RestaurantRepository : IRestaurantRepository
{
    private readonly IsmDbContext _dbContext;

    public RestaurantRepository(IsmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Restaurant?> GetRestaurantByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Restaurants
            .FirstOrDefaultAsync(restaurant => restaurant.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Restaurant>> GetAllRestaurantsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Restaurants.ToListAsync(cancellationToken);
    }

    public async Task AddRestaurantAsync(Restaurant restaurant, CancellationToken cancellationToken = default)
    {
        await _dbContext.Restaurants.AddAsync(restaurant, cancellationToken);
    }

    public void Update(Restaurant restaurant)
    {
        _dbContext.Restaurants.Update(restaurant);
    }

    public void Delete(Restaurant restaurant)
    {
        _dbContext.Restaurants.Remove(restaurant);
    }

    public async Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SaveChangesAsync(cancellationToken) > 0;
    }
}