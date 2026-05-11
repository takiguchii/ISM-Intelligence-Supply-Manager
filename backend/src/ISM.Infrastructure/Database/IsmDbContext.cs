using ISM.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ISM.Infrastructure.Database;

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
