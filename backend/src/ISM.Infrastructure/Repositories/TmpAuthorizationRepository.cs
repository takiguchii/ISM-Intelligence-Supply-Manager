using ISM.Domain.Entities;
using ISM.Domain.Interfaces;
using ISM.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace ISM.Infrastructure.Repositories;

public sealed class TmpAuthorizationRepository : ITmpAuthorizationRepository
{
    private readonly IsmDbContext _dbContext;

    public TmpAuthorizationRepository(IsmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TmpAuthorization?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TmpAuthorizations
            .Include(t => t.CreatedByUser)
            .Include(t => t.RevokedByUser)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<TmpAuthorization?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TmpAuthorizations
            .IgnoreQueryFilters()
            .Include(t => t.Restaurant)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);
    }

    public async Task<IReadOnlyList<TmpAuthorization>> ListByRestaurantAsync(int restaurantId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TmpAuthorizations
            .Include(t => t.CreatedByUser)
            .Include(t => t.RevokedByUser)
            .Where(t => t.RestaurantId == restaurantId)
            .OrderByDescending(t => t.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<TmpAuthorization> CreateAsync(TmpAuthorization entity, CancellationToken cancellationToken = default)
    {
        await _dbContext.TmpAuthorizations.AddAsync(entity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task<bool> UpdateAsync(TmpAuthorization entity, CancellationToken cancellationToken = default)
    {
        entity.UpdatedAtUtc = DateTime.UtcNow;
        _dbContext.TmpAuthorizations.Update(entity);
        return await _dbContext.SaveChangesAsync(cancellationToken) > 0;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        if (entity == null) return false;
        _dbContext.TmpAuthorizations.Remove(entity);
        return await _dbContext.SaveChangesAsync(cancellationToken) > 0;
    }
}
