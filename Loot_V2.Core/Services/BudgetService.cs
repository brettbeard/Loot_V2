using System.Xml.Linq;

using Loot_V2.Core.Contracts.Services;
using Loot_V2.Core.Models;

namespace Loot_V2.Core.Services;

public class BudgetService : IBudgetService
{
    private static readonly string FolderPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Loot_V2");

    private static readonly string FilePath = Path.Combine(FolderPath, "budget.xml");

    private List<BudgetItem>? _cache;

    public async Task<IList<BudgetItem>> GetItemsAsync()
    {
        if (_cache is null)
            _cache = await Task.Run(Load);
        return _cache;
    }

    public async Task AddItemAsync(BudgetItem item)
    {
        var items = await GetItemsAsync();
        items.Add(item);
        await Task.Run(SaveCache);
    }

    public async Task UpdateItemAsync(BudgetItem item)
    {
        var items = await GetItemsAsync();
        var idx = items.IndexOf(items.First(x => x.Id == item.Id));
        items[idx] = item;
        await Task.Run(SaveCache);
    }

    public async Task DeleteItemAsync(Guid id)
    {
        var items = await GetItemsAsync();
        var item = items.FirstOrDefault(x => x.Id == id);
        if (item is not null)
        {
            items.Remove(item);
            await Task.Run(SaveCache);
        }
    }

    private List<BudgetItem> Load()
    {
        if (!File.Exists(FilePath))
            return new List<BudgetItem>();

        var doc = XDocument.Load(FilePath);
        return doc.Root!.Elements("Item").Select(e => new BudgetItem
        {
            Id = Guid.Parse(e.Attribute("Id")!.Value),
            Name = e.Attribute("Name")!.Value,
            Amount = decimal.Parse(e.Attribute("Amount")?.Value ?? "0", System.Globalization.CultureInfo.InvariantCulture),
            IsCredit = bool.Parse(e.Attribute("IsCredit")?.Value ?? "false"),
            DayOfMonth = int.Parse(e.Attribute("DayOfMonth")!.Value)
        }).ToList();
    }

    private void SaveCache()
    {
        Directory.CreateDirectory(FolderPath);
        var doc = new XDocument(
            new XElement("Budget",
                _cache!.Select(item => new XElement("Item",
                    new XAttribute("Id", item.Id),
                    new XAttribute("Name", item.Name),
                    new XAttribute("Amount", item.Amount),
                    new XAttribute("IsCredit", item.IsCredit),
                    new XAttribute("DayOfMonth", item.DayOfMonth)))));
        doc.Save(FilePath);
    }
}
