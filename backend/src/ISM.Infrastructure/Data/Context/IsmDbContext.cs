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

    public DbSet<Fornecedor> Fornecedores => Set<Fornecedor>();
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
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
