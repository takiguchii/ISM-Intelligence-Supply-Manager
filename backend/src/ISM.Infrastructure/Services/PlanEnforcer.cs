using ISM.Application.Interfaces;
using ISM.Domain.Entities;
using ISM.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace ISM.Infrastructure.Services;

public sealed class PlanEnforcer : IPlanEnforcer
{
    private readonly IsmDbContext _db;

    public PlanEnforcer(IsmDbContext db)
    {
        _db = db;
    }

    public async Task AssertCanAddUserAsync(int restaurantId, CancellationToken ct)
    {
        var (plan, _) = await GetPlanAsync(restaurantId, ct);
        var count = await _db.Users.CountAsync(u => u.RestaurantId == restaurantId, ct);
        if (count >= plan.MaxUsers)
            throw new InvalidOperationException(
                $"Limite de usuários do plano {plan.Name} atingido ({plan.MaxUsers}). Faça upgrade para adicionar mais.");
    }

    public async Task AssertCanAddCategoryAsync(int restaurantId, CancellationToken ct)
    {
        var (plan, _) = await GetPlanAsync(restaurantId, ct);
        var count = await _db.Categories.CountAsync(c => c.RestaurantId == restaurantId, ct);
        if (count >= plan.MaxCategories)
            throw new InvalidOperationException(
                $"Limite de categorias do plano {plan.Name} atingido ({plan.MaxCategories}).");
    }

    public async Task AssertCanAddProductAsync(int restaurantId, CancellationToken ct)
    {
        var (plan, _) = await GetPlanAsync(restaurantId, ct);
        var count = await _db.Products.CountAsync(p => p.RestaurantId == restaurantId, ct);
        if (count >= plan.MaxProducts)
            throw new InvalidOperationException(
                $"Limite de produtos do plano {plan.Name} atingido ({plan.MaxProducts}).");
    }

    public async Task AssertCanAddDishAsync(int restaurantId, CancellationToken ct)
    {
        var (plan, _) = await GetPlanAsync(restaurantId, ct);
        var count = await _db.Dishes.CountAsync(d => d.RestaurantId == restaurantId, ct);
        if (count >= plan.MaxDishes)
            throw new InvalidOperationException(
                $"Limite de pratos do plano {plan.Name} atingido ({plan.MaxDishes}).");
    }

    public async Task<(bool IsActive, string? Message)> ValidateRestaurantAccessAsync(int? restaurantId, CancellationToken ct)
    {
        if (!restaurantId.HasValue) return (true, null);

        var restaurant = await _db.Restaurants
            .Include(r => r.Plan)
            .FirstOrDefaultAsync(r => r.Id == restaurantId.Value, ct);

        if (restaurant == null) return (false, "Restaurante não encontrado.");
        if (!restaurant.IsPlanActive) return (false, "Assinatura do restaurante está suspensa.");
        if (restaurant.TrialEndAtUtc.HasValue && restaurant.TrialEndAtUtc.Value < DateTime.UtcNow && restaurant.Plan?.MonthlyPrice == 0)
            return (false, "Período trial expirou. Ative sua assinatura para continuar.");

        return (true, null);
    }

    private async Task<(Plan Plan, Restaurant Restaurant)> GetPlanAsync(int restaurantId, CancellationToken ct)
    {
        var restaurant = await _db.Restaurants
            .Include(r => r.Plan)
            .FirstOrDefaultAsync(r => r.Id == restaurantId, ct);

        if (restaurant == null)
            throw new InvalidOperationException($"Restaurante com ID {restaurantId} não existe.");

        if (!restaurant.IsPlanActive)
            throw new InvalidOperationException("Assinatura do restaurante está suspensa.");

        var plan = restaurant.Plan ?? new Plan
        {
            Name = "Free",
            MaxUsers = 2,
            MaxDishes = 10,
            MaxProducts = 20,
            MaxCategories = 3,
            MonthlyPrice = 0,
            IsActive = true
        };

        return (plan, restaurant);
    }
}
