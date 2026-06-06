using ISM.Domain.Modules.Stock.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ISM.Infrastructure.Persistence.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
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
    }
}
