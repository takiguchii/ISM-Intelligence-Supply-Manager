using ISM.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ISM.Infrastructure.Data.Context;

public sealed class IsmDbContext : DbContext
{
    public IsmDbContext(DbContextOptions<IsmDbContext> options)
        : base(options)
    {
    }

    public DbSet<Fornecedor> Fornecedores => Set<Fornecedor>();

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
    }
}
