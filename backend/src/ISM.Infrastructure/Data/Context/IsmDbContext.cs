using System.Linq.Expressions;
using System.Reflection;
using ISM.Application.Security;
using ISM.Domain.Entities;
using ISM.Domain.Modules.Stock.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ISM.Domain.Modules.Menu.Entities;

namespace ISM.Infrastructure.Data.Context;

public sealed class IsmDbContext : DbContext
{
    private readonly ICurrentUser? _currentUser;

    public IsmDbContext(DbContextOptions<IsmDbContext> options, ICurrentUser? currentUser = null)
        : base(options)
    {
        _currentUser = currentUser;
    }

    public DbSet<Fornecedor> Fornecedores => Set<Fornecedor>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Restaurant> Restaurants => Set<Restaurant>();
    public DbSet<Plano> Planos => Set<Plano>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Dish> Dishes => Set<Dish>();
    public DbSet<DishIngredient> DishIngredients => Set<DishIngredient>();
    public DbSet<User> Users => Set<User>();

    internal int? CurrentUserRestaurantId =>
        _currentUser != null && !_currentUser.IsSuperAdmin
            ? _currentUser.RestaurantId
            : null;

    internal bool ApplyTenantFilter => _currentUser != null && !_currentUser.IsSuperAdmin;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Plano>(builder =>
        {
            builder.ToTable("planos");
            builder.HasKey(p => p.Id);
            builder.Property(p => p.Nome).HasMaxLength(50).IsRequired();
            builder.Property(p => p.Descricao).HasMaxLength(200);
            builder.Property(p => p.PrecoMensal).HasColumnType("decimal(10,2)").IsRequired();
            builder.Property(p => p.MaxUsuarios).IsRequired();
            builder.Property(p => p.MaxPratos).IsRequired();
            builder.Property(p => p.MaxProdutos).IsRequired();
            builder.Property(p => p.MaxCategorias).IsRequired();
            builder.Property(p => p.Ativo).IsRequired();
            builder.Property(p => p.CreatedAtUtc).HasColumnType("datetime(6)").IsRequired();
            builder.Property(p => p.UpdatedAtUtc).HasColumnType("datetime(6)");
            builder.HasIndex(p => p.Nome).IsUnique();
        });

        modelBuilder.Entity<Fornecedor>(builder =>
        {
            builder.ToTable("fornecedores");
            builder.HasKey(f => f.Id);
            builder.Property(f => f.Nome).HasMaxLength(150).IsRequired();
            builder.Property(f => f.Categoria).HasMaxLength(100).IsRequired();
            builder.Property(f => f.Email).HasMaxLength(150).IsRequired();
            builder.Property(f => f.Telefone).HasMaxLength(20).IsRequired();
            builder.Property(f => f.CreatedAtUtc).HasColumnType("datetime(6)").IsRequired();
            builder.Property(f => f.UpdatedAtUtc).HasColumnType("datetime(6)");
        });

        modelBuilder.Entity<Product>(builder =>
        {
            builder.ToTable("products");
            builder.HasKey(p => p.Id);
            builder.Property(p => p.RestaurantId).IsRequired();
            builder.Property(p => p.Name).HasMaxLength(120).IsRequired();
            builder.Property(p => p.Unit).HasMaxLength(10).IsRequired();
            builder.Property(p => p.CurrentQuantity).HasColumnType("decimal(10,3)").IsRequired();
            builder.Property(p => p.MinimumQuantity).HasColumnType("decimal(10,3)").IsRequired();
            builder.Property(p => p.AverageCost).HasColumnType("decimal(10,2)").IsRequired();
            builder.Property(p => p.IsActive).IsRequired();
            builder.Property(p => p.CreatedAtUtc).HasColumnType("datetime(6)").IsRequired();
            builder.Property(p => p.UpdatedAtUtc).HasColumnType("datetime(6)");
            builder.HasIndex(p => new { p.RestaurantId, p.Name }).IsUnique();

            builder.HasOne(p => p.Restaurant)
                .WithMany(r => r.Products)
                .HasForeignKey(p => p.RestaurantId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.ApplyTenantQueryFilter(this);
        });

        modelBuilder.Entity<Restaurant>(builder =>
        {
            builder.ToTable("restaurants");
            builder.HasKey(r => r.Id);
            builder.Property(r => r.Name).HasMaxLength(120).IsRequired();
            builder.Property(r => r.CNPJ).HasMaxLength(14).IsRequired();
            builder.HasIndex(r => r.CNPJ).IsUnique();
            builder.Property(r => r.Created).HasColumnType("datetime(6)").IsRequired();
            builder.Property(r => r.PlanoId);
            builder.Property(r => r.TrialEndAtUtc).HasColumnType("datetime(6)");
            builder.Property(r => r.PlanoAtivo).IsRequired();

            builder.HasOne(r => r.Plano)
                .WithMany(p => p.Restaurantes)
                .HasForeignKey(r => r.PlanoId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Category>(builder =>
        {
            builder.ToTable("categories");
            builder.HasKey(c => c.Id);
            builder.Property(c => c.RestaurantId).IsRequired();
            builder.Property(c => c.Name).HasMaxLength(80).IsRequired();
            builder.Property(c => c.IsActive).IsRequired();
            builder.Property(c => c.DisplayOrder).IsRequired();
            builder.Property(c => c.CreatedAtUtc).HasColumnType("datetime(6)").IsRequired();
            builder.Property(c => c.UpdatedAtUtc).HasColumnType("datetime(6)");
            builder.HasIndex(c => new { c.RestaurantId, c.Name }).IsUnique();

            builder.HasOne(c => c.Restaurant)
                .WithMany(r => r.Categories)
                .HasForeignKey(c => c.RestaurantId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.ApplyTenantQueryFilter(this);
        });

        modelBuilder.Entity<Dish>(builder =>
        {
            builder.ToTable("dishes");
            builder.HasKey(d => d.Id);
            builder.Property(d => d.RestaurantId).IsRequired();
            builder.Property(d => d.CategoryId).IsRequired();
            builder.Property(d => d.Name).HasMaxLength(80).IsRequired();
            builder.Property(d => d.Description).HasMaxLength(255).IsRequired(false);
            builder.Property(d => d.UrlImage).HasMaxLength(255).IsRequired(false);
            builder.Property(d => d.Cost).HasColumnType("decimal(10,2)").IsRequired();
            builder.Property(d => d.Price).HasColumnType("decimal(10,2)").IsRequired();
            builder.Property(d => d.IsActive).IsRequired();
            builder.Property(d => d.Highlight).IsRequired();
            builder.Property(d => d.DisplayOrder).IsRequired();
            builder.Property(d => d.CreatedAtUtc).HasColumnType("datetime(6)").IsRequired();
            builder.Property(d => d.UpdatedAtUtc).HasColumnType("datetime(6)");
            builder.HasIndex(d => new { d.RestaurantId, d.Name }).IsUnique();
            builder.HasIndex(d => new { d.RestaurantId, d.CategoryId });

            builder.HasOne(d => d.Category)
                .WithMany(c => c.Dishes)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(d => d.Restaurant)
                .WithMany(r => r.Dishes)
                .HasForeignKey(d => d.RestaurantId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.ApplyTenantQueryFilter(this);
        });

        modelBuilder.Entity<DishIngredient>(builder =>
        {
            builder.ToTable("dishing_ingredients");
            builder.HasKey(di => new { di.DishId, di.ProductId });
            builder.Property(di => di.Quantity).HasColumnType("decimal(10,3)");

            builder.HasOne(di => di.Dish)
                .WithMany(d => d.Ingredients)
                .HasForeignKey(di => di.DishId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(di => di.Product)
                .WithMany()
                .HasForeignKey(di => di.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<User>(builder =>
        {
            builder.ToTable("users");
            builder.HasKey(u => u.Id);
            builder.Property(u => u.Name).HasMaxLength(150).IsRequired();
            builder.Property(u => u.Email).HasMaxLength(150).IsRequired();
            builder.HasIndex(u => u.Email).IsUnique();
            builder.Property(u => u.PasswordHash).HasMaxLength(512).IsRequired();
            builder.Property(u => u.Role).HasMaxLength(50).IsRequired();
            builder.Property(u => u.IsActive).IsRequired();
            builder.Property(u => u.CreatedAtUtc).HasColumnType("datetime(6)").IsRequired();
            builder.Property(u => u.UpdatedAtUtc).HasColumnType("datetime(6)");

            builder.HasOne(u => u.Restaurant)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RestaurantId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.ApplyTenantQueryFilter(this);
        });
    }
}

public static class TenantQueryFilterExtensions
{
    public static EntityTypeBuilder<TEntity> ApplyTenantQueryFilter<TEntity>(
        this EntityTypeBuilder<TEntity> builder,
        IsmDbContext dbContext)
        where TEntity : class
    {
        var param = Expression.Parameter(typeof(TEntity), "e");
        var restaurantIdProp = typeof(TEntity).GetProperty("RestaurantId",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (restaurantIdProp == null) return builder;

        var propertyAccess = Expression.Property(param, restaurantIdProp);
        var propertyNullable = Expression.Convert(propertyAccess, typeof(int?));

        var restaurantIdExpr = Expression.Convert(
            Expression.Property(Expression.Constant(dbContext), nameof(IsmDbContext.CurrentUserRestaurantId)),
            typeof(int?));
        var applyFilterExpr = Expression.Property(
            Expression.Constant(dbContext), nameof(IsmDbContext.ApplyTenantFilter));

        Expression condition = Expression.OrElse(
            Expression.Not(applyFilterExpr),
            Expression.Equal(propertyNullable, restaurantIdExpr));

        var lambda = Expression.Lambda<Func<TEntity, bool>>(condition, param);
        builder.HasQueryFilter(lambda);

        return builder;
    }
}
