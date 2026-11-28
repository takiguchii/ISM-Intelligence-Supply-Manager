using System.Globalization;
using ISM.Application.DTOs.Dashboard;
using ISM.Application.Interfaces.Dashboard;
using ISM.Application.Security;
using ISM.Domain.Interfaces;
using ISM.Domain.Modules.Orders;

namespace ISM.Application.Services.Dashboard;

public sealed class DashboardService(IOrderRepository orders, IProductRepository products, ICurrentUser currentUser) : IDashboardService
{
    private static readonly TimeZoneInfo LocalZone = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
    private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("pt-BR");

    public async Task<DashboardResponse> GetAsync(int? restaurantId, CancellationToken cancellationToken = default)
    {
        var tenant = currentUser.IsSuperAdmin ? restaurantId : currentUser.RestaurantId;
        if (tenant is not > 0) throw new InvalidOperationException("Selecione um restaurante para consultar os indicadores.");
        if (!currentUser.IsSuperAdmin && restaurantId.HasValue && restaurantId != currentUser.RestaurantId)
            throw new UnauthorizedAccessException("Restaurante não autorizado.");
        var allOrders = await orders.GetAllAsync(tenant.Value, cancellationToken);
        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, LocalZone).Date;
        DateTime LocalPaid(Order order) => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(order.PaidAtUtc!.Value, DateTimeKind.Utc), LocalZone);
        var paid = allOrders.Where(order => order.Status == OrderStatus.Paid && order.PaidAtUtc.HasValue)
            .Select(order => (Order: order, LocalTime: LocalPaid(order))).Where(entry => entry.LocalTime.Date <= now)
            .GroupBy(entry => entry.Order.PaidTabId ?? entry.Order.Id)
            .Select(group => new
            {
                LocalTime = group.Min(entry => entry.LocalTime),
                Orders = group.Select(entry => entry.Order).ToArray(),
                Revenue = group.SelectMany(entry => entry.Order.Items).Sum(item => item.UnitPrice * item.Quantity),
                Cost = group.SelectMany(entry => entry.Order.Items).Sum(item => item.UnitCost * item.Quantity)
            }).ToList();
        var week = paid.Where(entry => entry.LocalTime.Date >= now.AddDays(-6)).ToList();
        var revenue = week.Sum(entry => entry.Revenue);
        var cost = week.Sum(entry => entry.Cost);
        var margin = revenue > 0 ? (revenue - cost) / revenue * 100 : (decimal?)null;
        var stock = await products.GetAllAsync(cancellationToken);
        var criticalStock = stock.Where(product => product.RestaurantId == tenant.Value && product.IsActive && product.CurrentQuantity <= product.MinimumQuantity)
            .OrderBy(product => product.CurrentQuantity - product.MinimumQuantity)
            .Select(product => new DashboardCriticalProduct(product.Name, product.CurrentQuantity, product.MinimumQuantity, product.Unit)).ToArray();
        var criticalCount = criticalStock.Length;
        var kpis = new[] {
            new DashboardKpi("receita", "Receita de pedidos pagos", revenue.ToString("C", Culture), "—", "up", "Money", "Últimos 7 dias"),
            new DashboardKpi("margem", "Margem bruta dos pratos", margin.HasValue ? margin.Value.ToString("N1", Culture) + "%" : "—", "—", "up", "TrendCharts", "Receita menos custo dos pratos"),
            new DashboardKpi("pedidos", "Comandas pagas", week.Count.ToString(Culture), "—", "up", "Tickets", "Últimos 7 dias"),
            new DashboardKpi("estoque", "Itens em estoque crítico", criticalCount.ToString(Culture), "—", "down", "WarningFilled", "No nível mínimo ou abaixo")
        };
        var daily = Enumerable.Range(0, 7).Select(index => now.AddDays(index - 6)).Select(day => {
            var entries = week.Where(entry => entry.LocalTime.Date == day);
            return new RevenueCostPoint(day.ToString("dd/MM", Culture), entries.Sum(entry => entry.Revenue), entries.Sum(entry => entry.Cost));
        }).ToArray();
        var top = week.SelectMany(entry => entry.Orders).SelectMany(order => order.Items).GroupBy(item => new { item.DishId, item.DishName })
            .Select(group => new DashboardDish(group.Key.DishName, group.Sum(item => item.Quantity))).OrderByDescending(item => item.Pedidos).ThenBy(item => item.Name).Take(10).ToArray();
        var recent = paid.Where(entry => entry.LocalTime.Date >= now.AddDays(-27)).ToList();
        var weekdays = Enumerable.Range(0, 7).Select(index => (DayOfWeek)((index + 1) % 7)).Select(day =>
            new DashboardWeekday(Culture.DateTimeFormat.GetAbbreviatedDayName(day), recent.Count(entry => entry.LocalTime.DayOfWeek == day) / 4m)).ToArray();
        var peakPeriods = new[]
        {
            new DashboardPeakPeriod("Manhã", recent.Count(entry => entry.LocalTime.Hour is >= 6 and < 11) / 4m),
            new DashboardPeakPeriod("Almoço", recent.Count(entry => entry.LocalTime.Hour is >= 11 and < 15) / 4m),
            new DashboardPeakPeriod("Tarde", recent.Count(entry => entry.LocalTime.Hour is >= 15 and < 18) / 4m),
            new DashboardPeakPeriod("Jantar", recent.Count(entry => entry.LocalTime.Hour is >= 18 and < 23) / 4m),
            new DashboardPeakPeriod("Madrugada", recent.Count(entry => entry.LocalTime.Hour is >= 23 or < 6) / 4m)
        };
        var orderQueue = new[]
        {
            new DashboardOrderQueue("Recebidos", allOrders.Count(order => order.Status == OrderStatus.Submitted)),
            new DashboardOrderQueue("Em preparo", allOrders.Count(order => order.Status == OrderStatus.Preparing)),
            new DashboardOrderQueue("Prontos", allOrders.Count(order => order.Status == OrderStatus.Ready))
        };
        return new DashboardResponse(kpis, daily, top, weekdays, criticalStock, peakPeriods, orderQueue);
    }
}
