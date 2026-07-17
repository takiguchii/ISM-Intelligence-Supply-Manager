using ISM.Domain.Entities;
using ISM.Domain.Modules.Stock.Entities;
using Microsoft.EntityFrameworkCore;
using ISM.Domain.Modules.Menu.Entities;


namespace ISM.Infrastructure.Data.Context;

public sealed class IsmDbContext : DbContext
{
    public IsmDbContext(DbContextOptions<IsmDbContext> options)
        : base(options)
    {
    }

    public DbSet<Fornecedor> Fornecedores => Set<Fornecedor>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Restaurant> Restaurants => Set<Restaurant>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Dish> Dishes => Set<Dish>();
    public DbSet<DishIngredient> DishIngredients => Set<DishIngredient>();
    

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

        modelBuilder.Entity<Restaurant>(builder =>
        {
            builder.ToTable("restaurants");
                
            builder.HasKey(restaurant => restaurant.Id);
                
            builder.Property(restaurant => restaurant.Name)
                .HasMaxLength(120)
                .IsRequired();

            builder.Property(restaurant => restaurant.CNPJ)
                .HasMaxLength(14)
                .IsRequired();

            builder.HasIndex(restaurant => restaurant.CNPJ)
                .IsUnique();
            
            builder.Property(restaurant => restaurant.Created)
                .HasColumnType("datetime(6)")
                .IsRequired();
        });

        modelBuilder.Entity<Category>(builder =>
        {
            builder.ToTable("categories");

            builder.HasKey(category => category.Id);

            builder.Property(category => category.RestaurantId)
                .IsRequired();

            builder.Property(category => category.Name)
                .HasMaxLength(80)
                .IsRequired();
            
            builder.Property(category => category.IsActive)
                .IsRequired();
            
            builder.Property(category => category.DisplayOrder)
                .IsRequired();
            
            builder.Property(category => category.CreatedAtUtc)
                .HasColumnType("datetime(6)")
                .IsRequired();
            
            builder.Property(category => category.UpdatedAtUtc)
                .HasColumnType("datetime(6)");
            
            builder.HasIndex(category => new {category.RestaurantId, category.Name}) 
                .IsUnique();

            builder.HasOne(category => category.Restaurant)
                .WithMany(restaurant => restaurant.Categories)
                .HasForeignKey(category => category.RestaurantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Dish>(builder =>
        {   
            builder.ToTable("dishes");
            
            builder.HasKey(dish => dish.Id);
            
            builder.Property(dish => dish.RestaurantId)
                .IsRequired();

            builder.Property(dish => dish.CategoryId)
                .IsRequired();

            builder.Property(dish => dish.Name)
                .HasMaxLength(80)
                .IsRequired();

            builder.Property(dish => dish.Description)
                .HasMaxLength(255)
                .IsRequired();
            
            builder.Property(dish => dish.UrlImage)
                .HasMaxLength(255)
                .IsRequired();
            
            builder.Property(dish => dish.Cost)
                .HasColumnType("decimal(10,2)")
                .IsRequired();

            builder.Property(dish => dish.Price)
                .HasColumnType("decimal(10,2)")
                .IsRequired();

            builder.Property(dish => dish.IsAtive)
                .IsRequired();

            builder.Property(dish => dish.Highlight)
                .IsRequired();
            
            builder.Property(dish => dish.DisplayOrder)
                .IsRequired();
            
            builder.Property(dish => dish.CreatedAtUtc)
                .HasColumnType("datetime(6)")
                .IsRequired();

            builder.Property(dish => dish.UpdatedAtUtc)
                .HasColumnType("datetime(6)");

            builder.HasIndex(dish => new { dish.RestaurantId, dish.Name }) 
                .IsUnique();

            builder.HasIndex(dish => new { dish.RestaurantId, dish.CategoryId }); 
            
            builder.HasOne(dish => dish.Category)
                .WithMany(category => category.Dishes)
                .HasForeignKey(dish => dish.CategoryId)
                .OnDelete(DeleteBehavior.Cascade); 
            
            builder.HasOne(dish => dish.Restaurant)
                .WithMany(restaurant => restaurant.Dishes)
                .HasForeignKey(dish => dish.RestaurantId)
                .OnDelete(DeleteBehavior.Cascade); 

        });

        modelBuilder.Entity<DishIngredient>(builder =>
        {
            builder.ToTable("dishing_ingredients");

            builder.HasKey(ingredient => new { ingredient.DishId, ingredient.ProductId });

            builder.Property(ingredient => ingredient.Quantity)
                .HasColumnType("decimal(10,3)");

            builder.HasOne(ingredient => ingredient.Dish)
                .WithMany(dish => dish.Ingredients)
                .HasForeignKey(ingredient => ingredient.DishId)
                .OnDelete(DeleteBehavior.Cascade);
            
            builder.HasOne(ingredient => ingredient.Product)
                .WithMany()
                .HasForeignKey(ingredient => ingredient.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
