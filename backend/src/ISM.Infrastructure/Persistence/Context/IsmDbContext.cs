using ISM.Domain.Modules.System.Entities;
using Microsoft.EntityFrameworkCore;

namespace ISM.Infrastructure.Persistence.Context;

public sealed class IsmDbContext : DbContext
{
    public IsmDbContext(DbContextOptions<IsmDbContext> options)
        : base(options)
    {
    }

    public DbSet<PlatformModule> PlatformModules => Set<PlatformModule>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IsmDbContext).Assembly);
    }
}
