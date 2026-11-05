using ISM.Application.Interfaces.Restaurant;
using ISM.Domain.Entities;
using ISM.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace ISM.Infrastructure.Services;

public sealed class DemoSubscriptionService(IsmDbContext db) : IDemoSubscriptionService
{
    public async Task<DemoSubscription?> GetAsync(int restaurantId, CancellationToken ct = default)
    {
        var restaurant = await db.Restaurants.AsNoTracking().FirstOrDefaultAsync(r => r.Id == restaurantId, ct);
        if (restaurant == null) return null;
        var plans = await db.Plans.Where(p => p.IsActive).OrderBy(p => p.MonthlyPrice)
            .Select(p => new SubscriptionPlan(p.Id, p.Name, p.Description, p.MonthlyPrice,
                p.MaxUsers, p.MaxDishes, p.MaxProducts, p.MaxCategories)).ToListAsync(ct);
        var invoices = await db.Set<DemoInvoice>().Where(i => i.RestaurantId == restaurantId)
            .OrderByDescending(i => i.CreatedAtUtc).Take(100)
            .Select(i => new SubscriptionInvoice(i.Id, i.PlanName, i.Amount, i.CreatedAtUtc)).ToListAsync(ct);
        return new DemoSubscription(restaurant.PlanId, restaurant.IsPlanActive, restaurant.TrialEndAtUtc, plans, invoices);
    }

    public async Task ChangePlanAsync(int restaurantId, int planId, CancellationToken ct = default)
    {
        var restaurant = await db.Restaurants.FirstOrDefaultAsync(r => r.Id == restaurantId, ct)
            ?? throw new InvalidOperationException("Restaurante não encontrado.");
        var plan = await db.Plans.FirstOrDefaultAsync(p => p.Id == planId && p.IsActive, ct)
            ?? throw new InvalidOperationException("Plano indisponível.");
        if (restaurant.PlanId == plan.Id && restaurant.IsPlanActive) return;
        if (await db.Users.IgnoreQueryFilters().CountAsync(u => u.RestaurantId == restaurantId && u.IsActive, ct) > plan.MaxUsers ||
            await db.Dishes.IgnoreQueryFilters().CountAsync(d => d.RestaurantId == restaurantId, ct) > plan.MaxDishes ||
            await db.Products.IgnoreQueryFilters().CountAsync(p => p.RestaurantId == restaurantId, ct) > plan.MaxProducts ||
            await db.Categories.IgnoreQueryFilters().CountAsync(c => c.RestaurantId == restaurantId, ct) > plan.MaxCategories)
            throw new InvalidOperationException("O uso atual do restaurante excede os limites deste plano.");
        restaurant.PlanId = plan.Id;
        restaurant.IsPlanActive = true;
        restaurant.TrialEndAtUtc = null;
        restaurant.UpdatedAtUtc = DateTime.UtcNow;
        db.Set<DemoInvoice>().Add(new DemoInvoice
        {
            RestaurantId = restaurantId, PlanName = plan.Name, Amount = plan.MonthlyPrice, CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync(ct);
    }
}
