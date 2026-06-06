using ISM.Domain.Entities;
using ISM.Domain.Modules.Stock.Entities;
using Microsoft.EntityFrameworkCore;

namespace ISM.Infrastructure.Data.Context;

public sealed class IsmDbContext : DbContext
{
    public IsmDbContext(DbContextOptions<IsmDbContext> options)
        : base(options)
    {
    }

    public DbSet<PlatformModule> PlatformModules => Set<PlatformModule>();
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PlatformModule>(builder =>
        {
            builder.ToTable("platform_modules");

            builder.HasKey(module => module.Id);

            builder.Property(module => module.Name)
                .HasMaxLength(120)
                .IsRequired();

            builder.Property(module => module.Slug)
                .HasMaxLength(120)
                .IsRequired();

            builder.Property(module => module.Description)
                .HasMaxLength(256)
                .IsRequired();

            builder.Property(module => module.CreatedAtUtc)
                .HasColumnType("datetime(6)")
                .IsRequired();

            builder.Property(module => module.UpdatedAtUtc)
                .HasColumnType("datetime(6)");

            builder.HasIndex(module => module.Slug)
                .IsUnique();

            builder.HasData(
                new PlatformModule
                {
                    Id = 1,
                    Name = "Foundation",
                    Slug = "foundation",
                    Description = "Core architecture, Docker and collaborative foundation.",
                    IsEnabled = true,
                    SortOrder = 1,
                    CreatedAtUtc = new DateTime(2026, 5, 10, 0, 0, 0, DateTimeKind.Utc)
                },
                new PlatformModule
                {
                    Id = 2,
                    Name = "Analytics",
                    Slug = "analytics",
                    Description = "Future analytics and intelligence capabilities.",
                    IsEnabled = true,
                    SortOrder = 2,
                    CreatedAtUtc = new DateTime(2026, 5, 10, 0, 0, 0, DateTimeKind.Utc)
                });
        });

        modelBuilder.Entity<Product>(builder =>
        {
            builder.ToTable("products");

            builder.HasKey(product => product.Id);

            builder.Property(product => product.Name)
                .HasMaxLength(120)
                .IsRequired();

            builder.Property(product => product.Unit)
                .HasMaxLength(10)
                .IsRequired();

            builder.Property(product => product.CurrentQuantity)
                .HasColumnType("decimal(10,3)")
                .IsRequired();

            builder.Property(product => product.MinimumQuantity)
                .HasColumnType("decimal(10,3)")
                .IsRequired();

            builder.Property(product => product.AverageCost)
                .HasColumnType("decimal(10,2)")
                .IsRequired();

            builder.Property(product => product.CreatedAtUtc)
                .HasColumnType("datetime(6)")
                .IsRequired();

            builder.Property(product => product.UpdatedAtUtc)
                .HasColumnType("datetime(6)");

            builder.HasIndex(product => product.Name);
        });
    }
}
