namespace ISM.Application.DTOs.Dashboard;

public sealed record DashboardKpi(string Id, string Label, string Value, string Delta, string Trend, string Icon, string? Hint);
public sealed record RevenueCostPoint(string Day, decimal Receita, decimal Custo);
public sealed record DashboardDish(string Name, int Pedidos);
public sealed record DashboardWeekday(string Day, decimal Pedidos);
public sealed record DashboardCriticalProduct(string Name, decimal CurrentQuantity, decimal MinimumQuantity, string Unit);
public sealed record DashboardPeakPeriod(string Period, decimal Pedidos);
public sealed record DashboardOrderQueue(string Status, int Pedidos);
public sealed record DashboardResponse(IReadOnlyList<DashboardKpi> Kpis, IReadOnlyList<RevenueCostPoint> RevenueVsCost, IReadOnlyList<DashboardDish> TopDishes, IReadOnlyList<DashboardWeekday> WeekdayOrders, IReadOnlyList<DashboardCriticalProduct> CriticalStock, IReadOnlyList<DashboardPeakPeriod> PeakPeriods, IReadOnlyList<DashboardOrderQueue> OrderQueue);
