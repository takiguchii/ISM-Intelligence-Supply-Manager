using ISM.Application.DTOs;
using ISM.Application.Interfaces;
using ISM.Application.Interfaces.Menu;
using ISM.Application.Options;
using ISM.Domain.Interfaces;
using ISM.Domain.Modules.Menu.Entities;
using Microsoft.Extensions.Options;

namespace ISM.Application.Services.Menu;

public class PricingCmvService : IPricingCmvService
{
    private readonly IDishRepository _dishRepository;
    private readonly PricingOptions _pricingOptions;

    public PricingCmvService(IDishRepository dishRepository, IOptions<PricingOptions> pricingOptions)
    {
        _dishRepository = dishRepository;
        _pricingOptions = pricingOptions.Value;
    }

    public async Task<IReadOnlyList<MarginAlertResponse>> RecalculateForProductAsync(int productId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<Dish> dishes = await _dishRepository.GetDishesByProductIdAsync(productId, cancellationToken);
        List<MarginAlertResponse> alerts = new List<MarginAlertResponse>();
        
        foreach (Dish dish in dishes)
        {
            decimal newCost = 0;
            
            foreach (DishIngredient ingredient in dish.Ingredients)
            {
                newCost += ingredient.Quantity * ingredient.Product!.AverageCost;
            }
            
            if (dish.Price > 0)
            {
                decimal cmv = (newCost / dish.Price) * 100;
                decimal target = dish.TargetMarginPercent ?? _pricingOptions.DefaultTargetMarginPercent;
                decimal previousCmv = (dish.Cost / dish.Price) * 100;

                if (cmv > (100 - target) && target < 100)
                {
                    decimal suggestedPrice = newCost / (1 - target/100);
                    string message = $"O prato {dish.Name} teve o CMV para {cmv:F0}%. Sugestão de novo preço: R${suggestedPrice:F2} para reestabelecer a margem de {target:F0}";
                    alerts.Add(new MarginAlertResponse(
                        dish.Id,
                        dish.Name,
                        PreviousCmvPercent: previousCmv,
                        CurrentCmvPercent: cmv,
                        TargetMarginPercent: target,
                        SuggestedPrice: suggestedPrice,
                        Message: message));
                }
            }

            await _dishRepository.UpdateDishCostAsync(dish.Id, newCost, cancellationToken);
        }
        return alerts;
    }
    public async Task<DishPricingResponse?> GetPricingForDishAsync(int dishId,
        CancellationToken cancellationToken = default)
    {
        Dish? dish = await _dishRepository.GetDishByIdAsync(dishId, cancellationToken);
        if (dish is null)
        {
            return null;
        }

        decimal currentCost = 0;
        foreach (DishIngredient ingredient in dish.Ingredients)
        {
            currentCost += ingredient.Quantity * ingredient.Product!.AverageCost;
        }

        decimal target = dish.TargetMarginPercent ?? _pricingOptions.DefaultTargetMarginPercent;
        MarginAlertResponse? alert = null;

        if (dish.Price > 0)
        {
            decimal cmv = (currentCost / dish.Price) * 100;

            if (cmv > (100 - target) && target < 100)
            {
                decimal suggestedPrice = currentCost / (1 - target/100);
                string message = $"O prato {dish.Name} teve o CMV para {cmv:F0}%. Sugestão de novo preço:" +
                                 $"R${suggestedPrice:F2} para reestabelecer a margem de {target:F0}%";
                alert = new MarginAlertResponse(
                    dish.Id,
                    dish.Name,
                    PreviousCmvPercent: (dish.Cost / dish.Price) * 100,
                    CurrentCmvPercent: cmv,
                    TargetMarginPercent: target,
                    SuggestedPrice: suggestedPrice,
                    Message: message);
            }

            return new DishPricingResponse(dish.Id, dish.Name, currentCost, cmv, target, alert);
        }
        return new DishPricingResponse(dish.Id, dish.Name, currentCost, 0, target, null);
    }
}