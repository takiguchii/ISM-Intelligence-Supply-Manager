using ISM.Application.DTOs.Finance;
using ISM.Application.Interfaces.Finance;
using ISM.Application.Security;
using ISM.Domain.Interfaces;
using ISM.Domain.Modules.Finance;
using ISM.Domain.Modules.Orders;
namespace ISM.Application.Services.Finance;

public sealed class FinanceService(IFinanceEntryRepository entries, IOrderRepository orders, ICurrentUser currentUser) : IFinanceService
{
    private static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
    private int RestaurantId => currentUser.RestaurantId is > 0 ? currentUser.RestaurantId.Value : throw new UnauthorizedAccessException("É necessário estar vinculado a um restaurante.");
    public async Task<FinanceOverview> GetAsync(CancellationToken cancellationToken = default)
    {
        var tenant = RestaurantId;
        var all = await entries.GetAllAsync(tenant, cancellationToken);
        var allOrders = await orders.GetAllAsync(tenant, cancellationToken);
        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Zone);
        bool IsThisMonth(DateTime utc) { var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zone); return local.Year == now.Year && local.Month == now.Month && local <= now; }
        var sales = allOrders.Where(order => order.Status == OrderStatus.Paid && order.PaidAtUtc.HasValue && IsThisMonth(order.PaidAtUtc.Value)).Sum(order => order.Items.Sum(item => item.UnitPrice * item.Quantity));
        decimal Sum(FinanceEntryType type, bool settled) => all.Where(entry => entry.Type == type && (settled ? entry.SettledAtUtc.HasValue && IsThisMonth(entry.SettledAtUtc.Value) : !entry.SettledAtUtc.HasValue)).Sum(entry => entry.Amount);
        return new FinanceOverview(new FinanceSummary(sales, Sum(FinanceEntryType.Income, true), Sum(FinanceEntryType.Expense, true), Sum(FinanceEntryType.Income, false), Sum(FinanceEntryType.Expense, false)), all.Select(Map).ToArray());
    }
    public async Task<FinanceEntryResponse> CreateAsync(FinanceEntryRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request);
        var entry = new FinanceEntry { RestaurantId = RestaurantId };
        Apply(entry, request);
        await entries.AddAsync(entry, cancellationToken);
        return Map(entry);
    }
    public async Task<FinanceEntryResponse?> UpdateAsync(int id, FinanceEntryRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request);
        var entry = await entries.GetByIdAsync(RestaurantId, id, cancellationToken);
        if (entry is null) return null;
        Apply(entry, request);
        entry.UpdatedAtUtc = DateTime.UtcNow;
        await entries.SaveAsync(cancellationToken);
        return Map(entry);
    }
    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entry = await entries.GetByIdAsync(RestaurantId, id, cancellationToken);
        if (entry is null) return false;
        await entries.DeleteAsync(entry, cancellationToken);
        return true;
    }
    private static void Validate(FinanceEntryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Description) || request.Description.Trim().Length > 150 || !Enum.IsDefined(request.Type) || request.Amount <= 0 || request.Amount > 99999999.99m || request.DueDate == default)
            throw new InvalidOperationException("Informe descrição, tipo, valor e vencimento válidos.");
    }
    private static void Apply(FinanceEntry entry, FinanceEntryRequest request)
    {
        entry.Description = request.Description.Trim(); entry.Type = request.Type; entry.Amount = request.Amount; entry.DueDate = request.DueDate.Date;
        entry.SettledAtUtc = request.IsSettled ? entry.SettledAtUtc ?? DateTime.UtcNow : null;
    }
    private static FinanceEntryResponse Map(FinanceEntry entry) => new(entry.Id, entry.Description, entry.Type, entry.Amount, entry.DueDate, entry.SettledAtUtc);
}
