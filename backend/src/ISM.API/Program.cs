using ISM.API.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApiServices(builder.Configuration);

var app = builder.Build();

await app.ApplyMigrationsAsync();

app.UseApiPipeline();

app.Run();

public partial class Program;
