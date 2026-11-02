using System.ComponentModel.DataAnnotations;
using ISM.Domain.Modules.Finance;
namespace ISM.Application.DTOs.Finance;
public sealed class FinanceEntryRequest
{
    [Required, StringLength(150, MinimumLength = 1)] public string Description { get; set; } = string.Empty;
    [StringLength(80)] public string Category { get; set; } = "Outros";
    [EnumDataType(typeof(FinanceEntryType))] public FinanceEntryType Type { get; set; }
    [Range(0.01, 99999999.99)] public decimal Amount { get; set; }
    [Required] public DateTime DueDate { get; set; }
    public bool IsSettled { get; set; }
}
public sealed record FinanceEntryResponse(int Id, string Description, string Category, FinanceEntryType Type, decimal Amount, DateTime DueDate, DateTime? SettledAtUtc);
public sealed record FinanceSummary(decimal SalesRevenue, decimal OtherIncome, decimal Expenses, decimal AccountsReceivable, decimal AccountsPayable);
public sealed record FinanceEntryPage(IReadOnlyList<FinanceEntryResponse> Items, int PageNumber, int PageSize, int TotalCount, int TotalPages, bool HasPreviousPage, bool HasNextPage);
public sealed record FinanceExpenseBreakdown(string Label, decimal Amount);
public sealed record FinanceTopSellingItem(string Name, int Quantity, decimal Revenue);
public sealed record FinanceCashFlowPoint(string Month, decimal Revenue, decimal Expenses);
public sealed record FinanceAnalytics(IReadOnlyList<FinanceExpenseBreakdown> ExpensesByCategory, IReadOnlyList<FinanceTopSellingItem> TopSellingItems, IReadOnlyList<FinanceCashFlowPoint> CashFlow);
public sealed record FinanceOverview(FinanceSummary Summary, FinanceEntryPage Entries, FinanceAnalytics Analytics);
