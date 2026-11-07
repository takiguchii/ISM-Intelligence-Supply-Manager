namespace ISM.Application.Interfaces.Restaurant;

public sealed record SubscriptionPlan(int Id, string Name, string? Description, decimal MonthlyPrice,
    int MaxUsers, int MaxDishes, int MaxProducts, int MaxCategories);
public sealed record SubscriptionInvoice(int Id, string PlanName, decimal Amount, DateTime CreatedAtUtc);
public sealed record DemoSubscription(int? PlanId, bool IsActive, DateTime? TrialEndAtUtc,
    IReadOnlyList<SubscriptionPlan> Plans, IReadOnlyList<SubscriptionInvoice> Invoices);

public interface IDemoSubscriptionService
{
    Task<DemoSubscription?> GetAsync(int restaurantId, CancellationToken ct = default);
    Task ChangePlanAsync(int restaurantId, int planId, CancellationToken ct = default);
}
