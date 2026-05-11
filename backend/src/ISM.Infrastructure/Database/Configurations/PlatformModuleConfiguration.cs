using ISM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ISM.Infrastructure.Database.Configurations;

public sealed class PlatformModuleConfiguration : IEntityTypeConfiguration<PlatformModule>
{
    public void Configure(EntityTypeBuilder<PlatformModule> builder)
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
    }
}
