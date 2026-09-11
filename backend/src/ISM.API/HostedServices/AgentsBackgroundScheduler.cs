using ISM.Application.Interfaces.Stock;
using ISM.Application.Interfaces.Suppliers;
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
        using var scope = _scopeFactory.CreateScope();
        var stockAgent = scope.ServiceProvider.GetRequiredService<IStockAgent>();
        var supplierAgent = scope.ServiceProvider.GetRequiredService<ISupplierAgent>();

        try
        {
            _logger.LogInformation("Iniciando tick StockAgent");
            var stockSummary = await stockAgent.RunAsync(null, ct);
            _logger.LogInformation(
                "StockAgent finalizado: Restaurantes={Restaurantes} Produtos={Produtos} Alertas={Alertas} SkipDedup={SkipDedup} Duração={DuracaoMs}ms",
                stockSummary.RestaurantsProcessed,
                stockSummary.EntitiesEvaluated,
                stockSummary.AlertsCreated,
                stockSummary.AlertsSkippedByDedup,
                stockSummary.Duration.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao executar tick StockAgent");
        }

        try
        {
            _logger.LogInformation("Iniciando tick SupplierAgent");
            var supplierSummary = await supplierAgent.RunAsync(null, ct);
            _logger.LogInformation(
                "SupplierAgent finalizado: Restaurantes={Restaurantes} Entidades={Entidades} Alertas={Alertas} SkipDedup={SkipDedup} Duração={DuracaoMs}ms",
                supplierSummary.RestaurantsProcessed,
                supplierSummary.EntitiesEvaluated,
                supplierSummary.AlertsCreated,
                supplierSummary.AlertsSkippedByDedup,
                supplierSummary.Duration.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao executar tick SupplierAgent");
        }
    }
}
