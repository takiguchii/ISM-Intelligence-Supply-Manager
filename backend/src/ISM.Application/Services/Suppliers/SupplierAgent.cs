using System.Diagnostics;
using System.Text.Json;
using ISM.Application.Common;
using ISM.Application.Interfaces.Suppliers;
using ISM.Application.Security;
using ISM.Domain.Entities;
using ISM.Domain.Interfaces;
using ISM.Domain.Modules.Suppliers.Entities;
using ISM.Domain.Modules.System.Entities;
using ISM.Domain.Modules.System.Enums;
using RestaurantEntity = ISM.Domain.Entities.Restaurant;

namespace ISM.Application.Services.Suppliers;

public sealed class SupplierAgent : ISupplierAgent
{
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly ISupplierProductPriceHistoryRepository _supplierPriceHistoryRepository;
    private readonly ISystemAlertRepository _systemAlertRepository;
    private readonly ICurrentUser _currentUser;

    public SupplierAgent(
        IRestaurantRepository restaurantRepository,
        ISupplierProductPriceHistoryRepository supplierPriceHistoryRepository,
        ISystemAlertRepository systemAlertRepository,
        ICurrentUser currentUser)
    {
        _restaurantRepository = restaurantRepository;
        _supplierPriceHistoryRepository = supplierPriceHistoryRepository;
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
        var now = DateTime.UtcNow;
        var since60Days = now.AddDays(-60);

        foreach (var restaurant in restaurants)
        {
            restaurantsProcessed++;

            var allHistory = await _supplierPriceHistoryRepository.GetForRestaurantSinceAsync(
                restaurant.Id,
                since60Days,
                cancellationToken);

            if (allHistory.Count == 0)
                continue;

            var (spikeAlerts, altAlerts, evaluated) = AnalyzeHistory(restaurant, allHistory, now);
            entitiesEvaluated += evaluated;

            foreach (var alertInfo in spikeAlerts)
            {
                var created = await TryCreateAlertAsync(
                    restaurant,
                    alertInfo.AlertType,
                    alertInfo.Severity,
                    alertInfo.Title,
                    alertInfo.Message,
                    alertInfo.Payload,
                    dedupWindow: TimeSpan.FromHours(48),
                    deduplicationKey: alertInfo.DedupKey,
                    referenceEntityType: alertInfo.RefEntityType,
                    referenceEntityId: alertInfo.RefEntityId,
                    cancellationToken);

                if (created) alertsCreated++; else alertsSkippedByDedup++;
            }

            foreach (var alertInfo in altAlerts)
            {
                var created = await TryCreateAlertAsync(
                    restaurant,
                    alertInfo.AlertType,
                    alertInfo.Severity,
                    alertInfo.Title,
                    alertInfo.Message,
                    alertInfo.Payload,
                    dedupWindow: TimeSpan.FromHours(48),
                    deduplicationKey: alertInfo.DedupKey,
                    referenceEntityType: alertInfo.RefEntityType,
                    referenceEntityId: alertInfo.RefEntityId,
                    cancellationToken);

                if (created) alertsCreated++; else alertsSkippedByDedup++;
            }
        }

        sw.Stop();
        return new AgentRunSummary(
            restaurantsProcessed,
            entitiesEvaluated,
            alertsCreated,
            alertsSkippedByDedup,
            sw.Elapsed,
            nameof(SupplierAgent));
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

    private record PendingAlertInfo(
        SystemAlertType AlertType,
        AlertSeverity Severity,
        string Title,
        string Message,
        object Payload,
        string DedupKey,
        string RefEntityType,
        int? RefEntityId);

    private (List<PendingAlertInfo> SpikeAlerts, List<PendingAlertInfo> AltAlerts, int Evaluated) AnalyzeHistory(
        RestaurantEntity restaurant,
        IReadOnlyList<SupplierProductPriceHistory> allHistory,
        DateTime now)
    {
        var spikes = new List<PendingAlertInfo>(capacity: 16);
        var alternatives = new List<PendingAlertInfo>(capacity: 16);
        int evaluated = 0;

        var since30Days = now.AddDays(-30);
        var since7Days = now.AddDays(-7);

        var normalized = allHistory.Select(h => new
        {
            History = h,
            ProductNormalized = StringHelper.NormalizeName(h.ProductRawName),
            SupplierNormalized = StringHelper.NormalizeName(h.SupplierRawName)
        })
        .Where(x => x.ProductNormalized.Length > 0)
        .ToList();

        var byProduct = normalized.GroupBy(x => x.ProductNormalized);

        foreach (var productGroup in byProduct)
        {
            var productName = productGroup.Key;

            var bySupplier = productGroup.GroupBy(x => x.SupplierNormalized);

            var supplierStats = new List<(
                string SupplierNormalized,
                string SupplierDisplayName,
                int? SupplierId,
                decimal Avg30Weighted,
                decimal Latest7dPrice,
                DateTime? Latest7dAt,
                int Count30d)>();

            foreach (var supplierGroup in bySupplier)
            {
                evaluated++;
                var supplierDisplayName = supplierGroup
                    .OrderByDescending(x => x.History.PurchasedAtUtc)
                    .FirstOrDefault()?.History.SupplierRawName ?? supplierGroup.Key;
                var supplierId = supplierGroup
                    .Select(x => x.History.SupplierId)
                    .FirstOrDefault(sid => sid.HasValue);

                var rows30d = supplierGroup
                    .Where(x => x.History.PurchasedAtUtc >= since30Days && x.History.QuantityPurchased > 0m)
                    .ToList();

                decimal avg30Weighted = 0m;
                int count30d = rows30d.Count;
                if (rows30d.Count > 0)
                {
                    decimal weightedSum = rows30d.Sum(x => x.History.UnitPrice * x.History.QuantityPurchased);
                    decimal totalQty = rows30d.Sum(x => x.History.QuantityPurchased);
                    if (totalQty > 0m)
                        avg30Weighted = weightedSum / totalQty;
                }

                var latest7d = supplierGroup
                    .Where(x => x.History.PurchasedAtUtc >= since7Days && x.History.UnitPrice > 0m)
                    .OrderByDescending(x => x.History.PurchasedAtUtc)
                    .FirstOrDefault();

                decimal latest7dPrice = latest7d?.History.UnitPrice ?? 0m;
                DateTime? latest7dAt = latest7d?.History.PurchasedAtUtc;

                supplierStats.Add((
                    supplierGroup.Key,
                    supplierDisplayName,
                    supplierId,
                    avg30Weighted,
                    latest7dPrice,
                    latest7dAt,
                    count30d));

                if (avg30Weighted > 0m && latest7dPrice > 0m &&
                    latest7dPrice >= avg30Weighted * 1.15m && count30d >= 2)
                {
                    decimal pct = (latest7dPrice - avg30Weighted) / avg30Weighted * 100m;
                    var severity = pct >= 25m ? AlertSeverity.Danger : AlertSeverity.Warning;
                    var payload = new
                    {
                        ProductNormalized = productName,
                        SupplierNormalized = supplierGroup.Key,
                        SupplierDisplayName = supplierDisplayName,
                        SupplierId = supplierId,
                        Average30dWeighted = avg30Weighted,
                        LatestPrice7d = latest7dPrice,
                        PercentageIncrease = pct,
                        LatestPurchaseAtUtc = latest7dAt
                    };

                    spikes.Add(new PendingAlertInfo(
                        SystemAlertType.SupplierPriceSpike15Percent,
                        severity,
                        $"Aumento de preço: {productName} ({supplierDisplayName})",
                        $"Preço atual {latest7dPrice:C} ficou {pct:N1}% acima da média 30 dias ({avg30Weighted:C}).",
                        payload,
                        DedupKey: $"spike_{restaurant.Id}_{productName}_{supplierGroup.Key}",
                        RefEntityType: "SupplierProductPrice",
                        RefEntityId: supplierId));
                }
            }

            if (supplierStats.Count >= 2)
            {
                for (int i = 0; i < supplierStats.Count; i++)
                {
                    var supA = supplierStats[i];
                    if (supA.Latest7dPrice <= 0m) continue;

                    for (int j = 0; j < supplierStats.Count; j++)
                    {
                        if (i == j) continue;
                        var supB = supplierStats[j];
                        if (supB.Latest7dPrice <= 0m) continue;

                        if (supB.Latest7dPrice <= supA.Latest7dPrice * 0.90m)
                        {
                            decimal savingPct = (supA.Latest7dPrice - supB.Latest7dPrice) / supA.Latest7dPrice * 100m;
                            var payload = new
                            {
                                ProductNormalized = productName,
                                SupplierANormalized = supA.SupplierNormalized,
                                SupplierADisplayName = supA.SupplierDisplayName,
                                SupplierAId = supA.SupplierId,
                                SupplierALatestPrice = supA.Latest7dPrice,
                                SupplierBNormalized = supB.SupplierNormalized,
                                SupplierBDisplayName = supB.SupplierDisplayName,
                                SupplierBId = supB.SupplierId,
                                SupplierBLatestPrice = supB.Latest7dPrice,
                                PercentageSaving = savingPct
                            };

                            alternatives.Add(new PendingAlertInfo(
                                SystemAlertType.SupplierBetterAlternative,
                                AlertSeverity.Info,
                                $"Preço melhor: {productName} com {supB.SupplierDisplayName}",
                                $"{supB.SupplierDisplayName} oferece por {supB.Latest7dPrice:C} — economia de {savingPct:N1}% sobre {supA.SupplierDisplayName} ({supA.Latest7dPrice:C}).",
                                payload,
                                DedupKey: $"alt_{restaurant.Id}_{productName}_{supA.SupplierNormalized}_x_{supB.SupplierNormalized}",
                                RefEntityType: "SupplierProductAlternative",
                                RefEntityId: supB.SupplierId));
                        }
                    }
                }
            }
        }

        return (spikes, alternatives, evaluated);
    }

    private async Task<bool> TryCreateAlertAsync(
        RestaurantEntity restaurant,
        SystemAlertType alertType,
        AlertSeverity severity,
        string title,
        string message,
        object payload,
        TimeSpan dedupWindow,
        string deduplicationKey,
        string referenceEntityType,
        int? referenceEntityId,
        CancellationToken cancellationToken)
    {
        var exists = await _systemAlertRepository.ExistsSameTypeNonDismissedWithinAsync(
            restaurant.Id,
            alertType,
            referenceEntityType,
            referenceEntityId,
            dedupWindow,
            deduplicationKey,
            cancellationToken);

        if (exists)
            return false;

        var alert = new SystemAlert
        {
            RestaurantId = restaurant.Id,
            AlertType = alertType,
            Severity = severity,
            Title = title.Length > 150 ? title[..150] : title,
            Message = message.Length > 400 ? message[..400] : message,
            ReferenceEntityType = referenceEntityType,
            ReferenceEntityId = referenceEntityId,
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
