using System.Diagnostics;
using System.Text.Json;
using ISM.Application.Common;
using ISM.Application.Interfaces.Stock;
using ISM.Application.Security;
using ISM.Domain.Entities;
using ISM.Domain.Interfaces;
using ISM.Domain.Modules.Stock.Entities;
using ISM.Domain.Modules.Stock.Enums;
using ISM.Domain.Modules.System.Entities;
using ISM.Domain.Modules.System.Enums;
using RestaurantEntity = ISM.Domain.Entities.Restaurant;

namespace ISM.Application.Services.Stock;

public sealed class StockAgent : IStockAgent
{
    private static readonly StockMovementType[] AllowedNegativeMovementTypes =
    [
        StockMovementType.Import,
        StockMovementType.ManualIncrease,
        StockMovementType.ManualDecrease,
        StockMovementType.InventoryAudit
    ];

    private readonly IRestaurantRepository _restaurantRepository;
    private readonly IProductRepository _productRepository;
    private readonly IStockMovementRepository _stockMovementRepository;
    private readonly ISystemAlertRepository _systemAlertRepository;
    private readonly ICurrentUser _currentUser;

    public StockAgent(
        IRestaurantRepository restaurantRepository,
        IProductRepository productRepository,
        IStockMovementRepository stockMovementRepository,
        ISystemAlertRepository systemAlertRepository,
        ICurrentUser currentUser)
    {
        _restaurantRepository = restaurantRepository;
        _productRepository = productRepository;
        _stockMovementRepository = stockMovementRepository;
        _systemAlertRepository = systemAlertRepository;
        _currentUser = currentUser;
    }

    public async Task<AgentRunSummary> RunAsync(
        int? restaurantIdFilter = null,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        int restaurantsProcessed = 0;
        int entitiesEvaluated = 0;
        int alertsCreated = 0;
        int alertsSkippedByDedup = 0;

        var restaurants = await SelectRestaurantsAsync(restaurantIdFilter, cancellationToken);

        foreach (var restaurant in restaurants)
        {
            restaurantsProcessed++;
            var allProducts = await _productRepository.GetAllAsync(cancellationToken);
            var productsOfRestaurant = allProducts
                .Where(p => p.RestaurantId == restaurant.Id)
                .ToList();

            if (productsOfRestaurant.Count == 0)
                continue;

            var now = DateTime.UtcNow;
            var since30Days = now.AddDays(-30);
            var productIds = productsOfRestaurant.Select(p => p.Id).ToArray();
            var negativeDeltasByProduct = await _stockMovementRepository.SumNegativeDeltasLast30DaysAsync(
                restaurant.Id,
                productIds,
                since30Days,
                AllowedNegativeMovementTypes,
                cancellationToken);

            var productsToUpdateMovingAverage = new List<Product>(capacity: productsOfRestaurant.Count);

            foreach (var product in productsOfRestaurant)
            {
                entitiesEvaluated++;

                negativeDeltasByProduct.TryGetValue(product.Id, out var sumNegative30);
                decimal movingAverage = CalculateMovingAverage(product, sumNegative30);

                if (Math.Abs(movingAverage - product.MovingAverageConsumption) > 0.000001m ||
                    !product.LastConsumptionRecalculatedAtUtc.HasValue)
                {
                    product.MovingAverageConsumption = movingAverage;
                    product.LastConsumptionRecalculatedAtUtc = now;
                    productsToUpdateMovingAverage.Add(product);
                }

                double daysRemaining = movingAverage > 0m
                    ? (double)(product.CurrentQuantity / movingAverage)
                    : double.MaxValue;

                if (movingAverage > 0m && daysRemaining <= 2.0)
                {
                    var created = await TryCreateAlertAsync(
                        restaurant,
                        product,
                        SystemAlertType.StockRuptureRisk48h,
                        AlertSeverity.Danger,
                        title: $"Estoque acabando em ≤48h: {product.Name}",
                        message: $"Saldo atual {product.CurrentQuantity:N2}{product.Unit}. Consumo médio 30 dias: {movingAverage:N3}/dia. Faltam ~{daysRemaining:N1} dias.",
                        payload: new
                        {
                            ProductId = product.Id,
                            product.Name,
                            CurrentQuantity = product.CurrentQuantity,
                            MinimumQuantity = product.MinimumQuantity,
                            MovingAverageConsumption = movingAverage,
                            DaysRemaining = daysRemaining,
                            RecalculatedAtUtc = now
                        },
                        dedupWindow: TimeSpan.FromHours(24),
                        cancellationToken);

                    if (created) alertsCreated++; else alertsSkippedByDedup++;
                }

                if (product.CurrentQuantity < product.MinimumQuantity)
                {
                    var created = await TryCreateAlertAsync(
                        restaurant,
                        product,
                        SystemAlertType.StockBelowMinimum,
                        AlertSeverity.Warning,
                        title: $"Estoque abaixo do mínimo: {product.Name}",
                        message: $"Saldo atual {product.CurrentQuantity:N2}{product.Unit}. Mínimo definido: {product.MinimumQuantity:N2}{product.Unit}.",
                        payload: new
                        {
                            ProductId = product.Id,
                            product.Name,
                            CurrentQuantity = product.CurrentQuantity,
                            MinimumQuantity = product.MinimumQuantity,
                            ReorderPoint = product.ReorderPoint,
                            DetectedAtUtc = now
                        },
                        dedupWindow: TimeSpan.FromHours(24),
                        cancellationToken);

                    if (created) alertsCreated++; else alertsSkippedByDedup++;
                }
            }

            foreach (var p in productsToUpdateMovingAverage)
            {
                try
                {
                    await _productRepository.UpdateAsync(p, cancellationToken);
                }
                catch
                {
                }
            }
        }

        sw.Stop();
        return new AgentRunSummary(
            restaurantsProcessed,
            entitiesEvaluated,
            alertsCreated,
            alertsSkippedByDedup,
            sw.Elapsed,
            nameof(StockAgent));
    }

    private async Task<IReadOnlyList<RestaurantEntity>> SelectRestaurantsAsync(
        int? restaurantIdFilter,
        CancellationToken cancellationToken)
    {
        if (restaurantIdFilter.HasValue)
        {
            var single = await _restaurantRepository.GetRestaurantByIdAsync(restaurantIdFilter.Value, cancellationToken);
            return single is not null ? new[] { single } : Array.Empty<RestaurantEntity>();
        }

        var all = await _restaurantRepository.GetAllRestaurantsAsync(cancellationToken);
        if (_currentUser.IsSuperAdmin)
            return all;

        if (_currentUser.RestaurantId.HasValue)
        {
            var single = all.FirstOrDefault(r => r.Id == _currentUser.RestaurantId.Value);
            return single is not null ? new[] { single } : Array.Empty<RestaurantEntity>();
        }

        return Array.Empty<RestaurantEntity>();
    }

    private static decimal CalculateMovingAverage(Product product, decimal sumNegativeDeltas30Days)
    {
        decimal sumAbs = Math.Abs(sumNegativeDeltas30Days);
        if (sumAbs > 0m)
            return sumAbs / 30m;

        if (product.MinimumQuantity > 0m)
            return product.MinimumQuantity / 7m;

        return 0m;
    }

    private async Task<bool> TryCreateAlertAsync(
        RestaurantEntity restaurant,
        Product product,
        SystemAlertType alertType,
        AlertSeverity severity,
        string title,
        string message,
        object payload,
        TimeSpan dedupWindow,
        CancellationToken cancellationToken)
    {
        var exists = await _systemAlertRepository.ExistsSameTypeNonDismissedWithinAsync(
            restaurant.Id,
            alertType,
            referenceEntityType: "Product",
            referenceEntityId: product.Id,
            withinWindow: dedupWindow,
            cancellationToken: cancellationToken);

        if (exists)
            return false;

        var alert = new SystemAlert
        {
            RestaurantId = restaurant.Id,
            AlertType = alertType,
            Severity = severity,
            Title = title,
            Message = message,
            ReferenceEntityType = "Product",
            ReferenceEntityId = product.Id,
            PayloadSerializedJson = JsonSerializer.Serialize(payload),
            IsRead = false,
            IsDismissed = false,
            GeneratedAtUtc = DateTime.UtcNow
        };

        try
        {
            await _systemAlertRepository.CreateAsync(alert, cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
