namespace Loot_V2.Core.Models;

public class MonthData
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal StartingBalance { get; set; }
    public List<MonthTransaction> Transactions { get; set; } = new();
    public List<OFXImportTransaction> OFXTransactions { get; set; } = new();
}
