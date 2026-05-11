using ISM.Application.Services;
using ISM.Application.Services;
using ISM.Infrastructure.Database;
using ISM.Infrastructure.DependencyInjection;

namespace ISM.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        var databaseOptions = new DatabaseOptions();
        configuration.GetSection(DatabaseOptions.SectionName).Bind(databaseOptions);
        databaseOptions.ConnectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? databaseOptions.ConnectionString;

        services.AddSingleton(databaseOptions);
        services.AddInfrastructure(databaseOptions);
        services.AddScoped<ISystemStatusService, SystemStatusService>();

        return services;
    }
}
