using ISM.Infrastructure.Data.Context;
using ISM.Infrastructure.Data.Options;
using ISM.Domain.Interfaces;
using ISM.Infrastructure.Repositories;
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

        services.AddScoped<IFornecedorRepository, FornecedorRepository>();

        return services;
    }
}
