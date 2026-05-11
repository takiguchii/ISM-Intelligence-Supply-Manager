using ISM.API.Middlewares;

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
}
