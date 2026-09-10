using System.Text.Json;

namespace ISM.API.Middlewares;

public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (UnauthorizedAccessException exception)
        {
            _logger.LogWarning(exception, "Forbidden access attempt.");
            await WriteErrorResponseAsync(context, StatusCodes.Status403Forbidden, "Forbidden", "Você não tem permissão para executar esta operação.");
        }
        catch (InvalidOperationException exception)
        {
            _logger.LogWarning(exception, "Invalid operation.");
            await WriteErrorResponseAsync(context, StatusCodes.Status400BadRequest, "Bad Request", exception.Message);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled exception while processing request.");
            await WriteErrorResponseAsync(context, StatusCodes.Status500InternalServerError, "Unexpected server error", "Ocorreu um erro inesperado.");
        }
    }

    private static async Task WriteErrorResponseAsync(HttpContext context, int statusCode, string title, string detail)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var payload = new
        {
            title,
            detail,
            status = statusCode,
            traceId = context.TraceIdentifier
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}
