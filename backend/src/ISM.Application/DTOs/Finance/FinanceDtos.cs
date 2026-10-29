using System.ComponentModel.DataAnnotations;
using ISM.Domain.Modules.Finance;
namespace ISM.Application.DTOs.Finance;
public sealed class FinanceEntryRequest
{
    [Required, StringLength(150, MinimumLength = 1)] public string Description { get; set; } = string.Empty;
    [EnumDataType(typeof(FinanceEntryType))] public FinanceEntryType Type { get; set; }
    [Range(0.01, 99999999.99)] public decimal Amount { get; set; }
    [Required] public DateTime DueDate { get; set; }
    public bool IsSettled { get; set; }
}
public sealed record FinanceEntryResponse(int Id, string Description, FinanceEntryType Type, decimal Amount, DateTime DueDate, DateTime? SettledAtUtc);
public sealed record FinanceSummary(decimal SalesRevenue, decimal OtherIncome, decimal Expenses, decimal AccountsReceivable, decimal AccountsPayable);
public sealed record FinanceOverview(FinanceSummary Summary, IReadOnlyList<FinanceEntryResponse> Entries);
