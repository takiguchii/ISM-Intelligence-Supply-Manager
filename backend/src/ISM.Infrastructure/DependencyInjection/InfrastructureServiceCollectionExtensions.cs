using ISM.Infrastructure.Data.Context;
using ISM.Infrastructure.Data.Options;
using ISM.Domain.Interfaces;
using ISM.Infrastructure.Repositories.Menu;
using ISM.Infrastructure.Repositories.Stock;
using ISM.Infrastructure.Repositories.Suppliers;
using ISM.Infrastructure.Repositories.Tenants;
using ISM.Infrastructure.Repositories.Users;
using ISM.Infrastructure.Repositories.DataImport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ISM.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        DatabaseOptions databaseOptions)
    {
        ArgumentNullException.ThrowIfNull(databaseOptions);

        services.AddDbContext<IsmDbContext>(options =>
        {
            if (databaseOptions.Provider.Equals("InMemory", StringComparison.OrdinalIgnoreCase))
            {
                options.UseInMemoryDatabase("ism-development-tests");
                return;
            }

            options.UseMySql(
                databaseOptions.ConnectionString,
                new MySqlServerVersion(new Version(8, 4, 0)),
                mySqlOptions =>
                {
                    mySqlOptions.MigrationsAssembly(typeof(IsmDbContext).Assembly.FullName);
                    mySqlOptions.EnableRetryOnFailure(3);
                });
        });

        services.AddScoped<ISupplierRepository, SupplierRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IDishRepository, DishRepository>();
        services.AddScoped<IRestaurantRepository, RestaurantRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IImportAuditRepository, ImportAuditRepository>();
        services.AddScoped<ITmpAuthorizationRepository, TmpAuthorizationRepository>();

        return services;
    }
}
