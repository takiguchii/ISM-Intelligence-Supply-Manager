namespace ISM.Application.DTOs.Dashboard;

public sealed record DashboardKpi(string Id, string Label, string Value, string Delta, string Trend, string Icon, string? Hint);
public sealed record RevenueCostPoint(string Day, decimal Receita, decimal Custo);
public sealed record DashboardDish(string Name, int Pedidos);
public sealed record DashboardWeekday(string Day, decimal Pedidos);
public sealed record DashboardResponse(IReadOnlyList<DashboardKpi> Kpis, IReadOnlyList<RevenueCostPoint> RevenueVsCost, IReadOnlyList<DashboardDish> TopDishes, IReadOnlyList<DashboardWeekday> WeekdayOrders);
