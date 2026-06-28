namespace Loot_V2.Models;

public class PlaidTransaction
{
    public string TransactionId { get; set; } = string.Empty;
    public string AccountId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    // Positive = money out (debit), negative = money in (credit) — Plaid's convention
    public decimal Amount { get; set; }
    public DateOnly Date { get; set; }
    public string? Category { get; set; }
    public bool Pending { get; set; }
}
