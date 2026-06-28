namespace Loot_V2.Core.Models;

public class OFXImportTransaction
{
    // OFX FITID
    public string Id { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public string Name { get; set; } = string.Empty;
    // Raw OFX amount: negative = debit (money out), positive = credit (money in)
    public decimal Amount { get; set; }
    public bool IsMatched { get; set; }
    public bool IsHidden { get; set; }
}
