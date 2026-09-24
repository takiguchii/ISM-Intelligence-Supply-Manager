using System.Diagnostics;
using System.Text.Json;
using ISM.Application.Common;
using ISM.Application.DTOs;
using ISM.Application.Interfaces;
using ISM.Application.Interfaces.Menu;
using ISM.Application.Options;
using ISM.Application.Security;
using ISM.Domain.Entities;
using ISM.Domain.Interfaces;
using ISM.Domain.Modules.Menu.Entities;
using ISM.Domain.Modules.System.Entities;
using ISM.Domain.Modules.System.Enums;
using Microsoft.Extensions.Options;
using RestaurantEntity = ISM.Domain.Entities.Restaurant;

namespace ISM.Application.Services.Menu;

public class PricingCmvService : IPricingCmvService, IPricingAgent
{
    private readonly IDishRepository _dishRepository;
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly ISystemAlertRepository _systemAlertRepository;
    private readonly ICurrentUser _currentUser;
    private readonly PricingOptions _pricingOptions;

    public PricingCmvService(
        IDishRepository dishRepository,
        IRestaurantRepository restaurantRepository,
        ISystemAlertRepository systemAlertRepository,
        ICurrentUser currentUser,
        IOptions<PricingOptions> pricingOptions)
    {
        _dishRepository = dishRepository;
        _restaurantRepository = restaurantRepository;
        _systemAlertRepository = systemAlertRepository;
        _currentUser = currentUser;
        _pricingOptions = pricingOptions.Value;
    }

    public async Task<IReadOnlyList<MarginAlertResponse>> RecalculateForProductAsync(
        int productId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<Dish> dishes = await _dishRepository.GetDishesByProductIdAsync(productId, cancellationToken);
        var alerts = new List<MarginAlertResponse>();

        foreach (var dish in dishes)
        {
            var alert = await ProcessDishCostAndAlertAsync(dish, cancellationToken);
            if (alert != null)
            {
                alerts.Add(alert);
            }
        }

        return alerts;
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
            var dishes = await _dishRepository.GetAllDishesAsync(restaurant.Id, null, cancellationToken);

            foreach (var dish in dishes)
            {
                entitiesEvaluated++;
                var alert = await ProcessDishCostAndAlertAsync(dish, cancellationToken);

                if (alert != null)
                {
                    bool created = await TryPersistAlertAsync(dish, alert, cancellationToken);
                    if (created)
                    {
                        alertsCreated++;
                    }
                    else
                    {
                        alertsSkippedByDedup++;
                    }
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
            nameof(PricingCmvService));
    }

    private async Task<MarginAlertResponse?> ProcessDishCostAndAlertAsync(
        Dish dish,
        CancellationToken cancellationToken)
    {
        if (dish.Price <= 0) return null;

        decimal newCost = 0;
        foreach (var ingredient in dish.Ingredients)
        {
            if (ingredient.Product != null)
            {
                newCost += ingredient.Quantity * ingredient.Product.AverageCost;
            }
        }

        decimal cmv = (newCost / dish.Price) * 100;
        decimal target = dish.TargetMarginPercent ?? _pricingOptions.DefaultTargetMarginPercent;
        decimal previousCmv = dish.Price > 0 ? (dish.Cost / dish.Price) * 100 : 0;

        MarginAlertResponse? result = null;

        if (cmv > (100 - target))
        {
            decimal suggestedPrice = (1 - (target / 100)) > 0
                ? newCost / (1 - (target / 100))
                : newCost * 1.5m;

            string message = $"O prato {dish.Name} teve o CMV alterado para {cmv:F0}%. Sugestão de novo preço: R${suggestedPrice:F2} para reestabelecer a margem de {target:F0}%.";

            result = new MarginAlertResponse(
                dish.Id,
                dish.Name,
                PreviousCmvPercent: previousCmv,
                CurrentCmvPercent: cmv,
                TargetMarginPercent: target,
                SuggestedPrice: suggestedPrice,
                Message: message);

            await TryPersistAlertAsync(dish, result, cancellationToken);
        }

        if (Math.Abs(dish.Cost - newCost) > 0.0001m)
        {
            dish.Cost = newCost;
            try
            {
                await _dishRepository.UpdateDishAsync(dish.Id, dish, dish.Ingredients.ToList(), cancellationToken);
            }
            catch
            {
                // Ignora falha de rastreamento EF se já atualizado
            }
        }

        return result;
    }

    private async Task<bool> TryPersistAlertAsync(
        Dish dish,
        MarginAlertResponse alertResponse,
        CancellationToken cancellationToken)
    {
        var exists = await _systemAlertRepository.ExistsSameTypeNonDismissedWithinAsync(
            dish.RestaurantId,
            SystemAlertType.DishCmvDefasagem,
            referenceEntityType: "Dish",
            referenceEntityId: dish.Id,
            withinWindow: TimeSpan.FromHours(24),
            cancellationToken: cancellationToken);

        if (exists) return false;

        var severity = alertResponse.CurrentCmvPercent > (alertResponse.TargetMarginPercent + 15)
            ? AlertSeverity.Danger
            : AlertSeverity.Warning;

        var alert = new SystemAlert
        {
            RestaurantId = dish.RestaurantId,
            AlertType = SystemAlertType.DishCmvDefasagem,
            Severity = severity,
            Title = $"Defasagem de CMV: {dish.Name}",
            Message = alertResponse.Message,
            ReferenceEntityType = "Dish",
            ReferenceEntityId = dish.Id,
            PayloadSerializedJson = JsonSerializer.Serialize(alertResponse),
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
}