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
            .Select(order => (Order: order, LocalTime: LocalPaid(order))).Where(entry => entry.LocalTime.Date <= now).ToList();
        var week = paid.Where(entry => entry.LocalTime.Date >= now.AddDays(-6)).ToList();
        var revenue = week.Sum(entry => entry.Order.Items.Sum(item => item.UnitPrice * item.Quantity));
        var cost = week.Sum(entry => entry.Order.Items.Sum(item => item.UnitCost * item.Quantity));
        var margin = revenue > 0 ? (revenue - cost) / revenue * 100 : (decimal?)null;
        var stock = await products.GetAllAsync(cancellationToken);
        var criticalCount = stock.Count(product => product.RestaurantId == tenant.Value && product.IsActive && product.CurrentQuantity <= product.MinimumQuantity);
        var kpis = new[] {
            new DashboardKpi("receita", "Receita de pedidos pagos", revenue.ToString("C", Culture), "—", "up", "Money", "Últimos 7 dias"),
            new DashboardKpi("margem", "Margem bruta dos pratos", margin.HasValue ? margin.Value.ToString("N1", Culture) + "%" : "—", "—", "up", "TrendCharts", "Receita menos custo dos pratos"),
            new DashboardKpi("pedidos", "Pedidos pagos", week.Count.ToString(Culture), "—", "up", "Tickets", "Últimos 7 dias"),
            new DashboardKpi("estoque", "Itens em estoque crítico", criticalCount.ToString(Culture), "—", "down", "WarningFilled", "No nível mínimo ou abaixo")
        };
        var daily = Enumerable.Range(0, 7).Select(index => now.AddDays(index - 6)).Select(day => {
            var entries = week.Where(entry => entry.LocalTime.Date == day);
            return new RevenueCostPoint(day.ToString("dd/MM", Culture), entries.Sum(entry => entry.Order.Items.Sum(item => item.UnitPrice * item.Quantity)), entries.Sum(entry => entry.Order.Items.Sum(item => item.UnitCost * item.Quantity)));
        }).ToArray();
        var top = week.SelectMany(entry => entry.Order.Items).GroupBy(item => new { item.DishId, item.DishName })
            .Select(group => new DashboardDish(group.Key.DishName, group.Sum(item => item.Quantity))).OrderByDescending(item => item.Pedidos).ThenBy(item => item.Name).Take(5).ToArray();
        var recent = paid.Where(entry => entry.LocalTime.Date >= now.AddDays(-27)).ToList();
        var weekdays = Enumerable.Range(0, 7).Select(index => (DayOfWeek)((index + 1) % 7)).Select(day =>
            new DashboardWeekday(Culture.DateTimeFormat.GetAbbreviatedDayName(day), recent.Count(entry => entry.LocalTime.DayOfWeek == day) / 4m)).ToArray();
        return new DashboardResponse(kpis, daily, top, weekdays);
    }
}
