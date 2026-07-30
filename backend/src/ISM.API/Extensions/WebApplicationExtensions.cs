using ISM.API.Middlewares;
using ISM.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace ISM.API.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication UseApiPipeline(this WebApplication app)
    {
        app.UseMiddleware<GlobalExceptionMiddleware>();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseAuthorization();
        app.MapControllers();

        return app;
    }

    public static async Task ApplyMigrationsAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<IsmDbContext>();
            await context.Database.MigrateAsync();
            await DbSeeder.SeedAsync(context);
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "Ocorreu um erro durante a inicialização do banco de dados.");
        }
    }
}
