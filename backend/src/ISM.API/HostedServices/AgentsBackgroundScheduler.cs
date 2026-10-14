using ISM.Application.Interfaces.Menu;
using ISM.Application.Interfaces.Stock;
using ISM.Application.Interfaces.Suppliers;
using ISM.Application.Security;
using ISM.Domain.Interfaces;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ISM.API.HostedServices;

public sealed class AgentsBackgroundScheduler : BackgroundService
{
    private readonly ILogger<AgentsBackgroundScheduler> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public AgentsBackgroundScheduler(
        ILogger<AgentsBackgroundScheduler> logger,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));

        try
        {
            await RunTickAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro fatal no tick inicial dos agentes");
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!await timer.WaitForNextTickAsync(stoppingToken))
                    break;

                await RunTickAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro fatal ao aguardar próximo tick dos agentes");
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }

    private async Task RunTickAsync(CancellationToken ct)
    {
        using var discoveryScope = _scopeFactory.CreateScope();
        var restaurants = await discoveryScope.ServiceProvider
            .GetRequiredService<IRestaurantRepository>()
            .GetAllRestaurantsAsync(ct);

        foreach (var restaurant in restaurants)
        {
            using var scope = _scopeFactory.CreateScope();
            var tenantContext = scope.ServiceProvider.GetRequiredService<IBackgroundTenantContext>();
            tenantContext.SetRestaurant(restaurant.Id);

            try
            {
                await scope.ServiceProvider.GetRequiredService<IStockAgent>().RunAsync(restaurant.Id, ct);
                await scope.ServiceProvider.GetRequiredService<ISupplierAgent>().RunAsync(restaurant.Id, ct);
                await scope.ServiceProvider.GetRequiredService<IPricingAgent>().RunAsync(restaurant.Id, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao executar agentes para restaurante {RestaurantId}", restaurant.Id);
            }
            finally
            {
                tenantContext.Clear();
            }
        }
    }
}
