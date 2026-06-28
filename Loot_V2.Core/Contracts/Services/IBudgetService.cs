using Loot_V2.Core.Models;

namespace Loot_V2.Core.Contracts.Services;

public interface IBudgetService
{
    Task<IList<BudgetItem>> GetItemsAsync();
    Task AddItemAsync(BudgetItem item);
    Task UpdateItemAsync(BudgetItem item);
    Task DeleteItemAsync(Guid id);
}
