namespace Loot_V2.Core.Models;

public class BudgetItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool IsCredit { get; set; }
    public int DayOfMonth { get; set; } = 1;
}
