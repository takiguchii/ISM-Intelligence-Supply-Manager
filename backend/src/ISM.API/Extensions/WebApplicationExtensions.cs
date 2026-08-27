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

        app.UseCors("AllowFrontend");
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        return app;
    }

    public static async Task ApplyMigrationsAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<Program>>();
        try
        {
            var context = services.GetRequiredService<IsmDbContext>();

            if (app.Environment.IsDevelopment())
            {
                var applied = await context.Database.GetAppliedMigrationsAsync();
                var defined = context.Database.GetMigrations();
                var orphaned = applied.Except(defined).ToList();

                if (orphaned.Any())
                {
                    logger.LogWarning("Divergência de banco de dados detectada (banco possui migrations órfãs de outra branch: {Orphaned}). Recriando banco local de desenvolvimento...", string.Join(", ", orphaned));
                    await context.Database.EnsureDeletedAsync();
                    await context.Database.MigrateAsync();
                    await DbSeeder.SeedAsync(context);
                    logger.LogInformation("Banco de dados local recriado e populado com sucesso.");
                    return;
                }
            }

            await context.Database.MigrateAsync();
            await DbSeeder.SeedAsync(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ocorreu um erro durante a inicialização do banco de dados.");
        }
    }
}
