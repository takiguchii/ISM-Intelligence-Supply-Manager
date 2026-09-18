using ISM.Application.DTOs;
using ISM.Application.Interfaces;
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
            
            decimal cmv = (newCost / dish.Price) * 100;
            decimal target = dish.TargetMarginPercent ?? _pricingOptions.DefaultTargetMarginPercent;
            decimal previousCmv = (dish.Cost / dish.Price) * 100;

            if (cmv > (100 - target))
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
            
            dish.Cost = newCost;
        }
        return alerts;
    }
}