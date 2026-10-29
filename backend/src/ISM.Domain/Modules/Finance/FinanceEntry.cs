using ISM.Domain.Entities;
namespace ISM.Domain.Modules.Finance;

public enum FinanceEntryType { Income, Expense }
public sealed class FinanceEntry : BaseEntity
{
    public int RestaurantId { get; set; }
    public string Description { get; set; } = string.Empty;
    public FinanceEntryType Type { get; set; }
    public decimal Amount { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? SettledAtUtc { get; set; }
}
