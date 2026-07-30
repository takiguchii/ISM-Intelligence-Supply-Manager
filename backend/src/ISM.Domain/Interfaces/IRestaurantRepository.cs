using ISM.Domain.Entities;

namespace ISM.Domain.Interfaces;

public interface IRestaurantRepository
{
    Task<Restaurant?> GetRestaurantByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Restaurant>> GetAllRestaurantsAsync(CancellationToken cancellationToken = default);
    Task AddRestaurantAsync(Restaurant restaurant, CancellationToken cancellationToken = default);
    void Update(Restaurant restaurant);
    void Delete(Restaurant restaurant);
    Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default);
}