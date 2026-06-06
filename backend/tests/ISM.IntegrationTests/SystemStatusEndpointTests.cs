using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using ISM.Application.DTOs;
using ISM.Infrastructure.Data.Context;
using ISM.Infrastructure.Data.Options;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ISM.IntegrationTests;

public sealed class SystemStatusEndpointTests : IClassFixture<IsmApiFactory>
{
    private readonly IsmApiFactory _factory;

    public SystemStatusEndpointTests(IsmApiFactory factory)
    {
        _factory = factory;

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IsmDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.EnsureCreated();
    }

    [Fact]
    public async Task GetStatus_ShouldReturnOk_WithSeededModules()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/system/status");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<SystemStatusResponse>();
        payload.Should().NotBeNull();
        payload!.DatabaseConnected.Should().BeTrue();
        payload.EnabledModuleCount.Should().BeGreaterThan(0);
    }
}

public sealed class IsmApiFactory : WebApplicationFactory<Program>
{
    private const string InMemoryDatabaseName = "ism-integration-tests";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Provider"] = "InMemory",
                ["ConnectionStrings:DefaultConnection"] = "Server=unused;Database=unused;"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(DatabaseOptions));
            services.RemoveAll(typeof(DbContextOptions<IsmDbContext>));

            services.AddSingleton(new DatabaseOptions
            {
                Provider = "InMemory",
                ConnectionString = "Server=unused;Database=unused;"
            });

            services.AddDbContext<IsmDbContext>(options =>
                options.UseInMemoryDatabase(InMemoryDatabaseName));
        });
    }
}
