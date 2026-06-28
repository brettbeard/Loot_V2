namespace Loot_V2.Core.Models;

public class MonthTransaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateOnly Date { get; set; }
    public string Name { get; set; } = string.Empty;
    // Absolute value — always positive; direction determined by IsCredit
    public decimal Amount { get; set; }
    public bool IsCredit { get; set; }
    public decimal SignedAmount => IsStartingBalance ? Amount : (IsCredit ? Amount : -Amount);
    public TransactionStatus Status { get; set; }
    public string? OFXTransactionId { get; set; }
    public decimal RunningBalance { get; set; }
    public bool IsStartingBalance { get; set; }
}
