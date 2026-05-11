using ISM.Domain.Entities;
using ISM.Domain.Interfaces;
using ISM.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace ISM.Infrastructure.Repositories;

public sealed class PlatformModuleRepository : IPlatformModuleRepository
{
    private readonly IsmDbContext _dbContext;

    public PlatformModuleRepository(IsmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> CanConnectAsync(CancellationToken cancellationToken = default)
        => _dbContext.Database.CanConnectAsync(cancellationToken);

    public Task<int> CountEnabledAsync(CancellationToken cancellationToken = default)
        => _dbContext.PlatformModules
            .Where(module => module.IsEnabled)
            .CountAsync(cancellationToken);

    public async Task<IReadOnlyList<PlatformModule>> GetEnabledOrderedAsync(CancellationToken cancellationToken = default)
        => await _dbContext.PlatformModules
            .Where(module => module.IsEnabled)
            .OrderBy(module => module.SortOrder)
            .Select(module => new PlatformModule
            {
                Id = module.Id,
                Name = module.Name,
                Slug = module.Slug,
                Description = module.Description,
                IsEnabled = module.IsEnabled,
                SortOrder = module.SortOrder,
                CreatedAtUtc = module.CreatedAtUtc,
                UpdatedAtUtc = module.UpdatedAtUtc
            })
            .ToListAsync(cancellationToken);

    public Task<PlatformModule?> GetFirstEnabledAsync(CancellationToken cancellationToken = default)
        => _dbContext.PlatformModules
            .Where(module => module.IsEnabled)
            .OrderBy(module => module.SortOrder)
            .FirstOrDefaultAsync(cancellationToken);
}
