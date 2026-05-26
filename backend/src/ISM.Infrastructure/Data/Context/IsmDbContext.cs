using ISM.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ISM.Infrastructure.Data.Context;

public sealed class IsmDbContext : DbContext
{
    public IsmDbContext(DbContextOptions<IsmDbContext> options)
        : base(options)
    {
    }

    public DbSet<PlatformModule> PlatformModules => Set<PlatformModule>();
    public DbSet<Fornecedor> Fornecedores => Set<Fornecedor>();

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

        modelBuilder.Entity<Fornecedor>(builder =>
        {
            builder.ToTable("fornecedores");

            builder.HasKey(f => f.Id);

            builder.Property(f => f.Nome)
                .HasMaxLength(150)
                .IsRequired();

            builder.Property(f => f.Categoria)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(f => f.Email)
                .HasMaxLength(150)
                .IsRequired();

            builder.Property(f => f.Telefone)
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(f => f.CreatedAtUtc)
                .HasColumnType("datetime(6)")
                .IsRequired();

            builder.Property(f => f.UpdatedAtUtc)
                .HasColumnType("datetime(6)");
        });
    }
}
