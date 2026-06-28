using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Loot_V2.Contracts.ViewModels;
using Loot_V2.Core.Contracts.Services;
using Loot_V2.Core.Models;

namespace Loot_V2.ViewModels;

public partial class BudgetViewModel : ObservableRecipient, INavigationAware
{
    private readonly IBudgetService _budgetService;
    private readonly IMonthDataService _monthDataService;

    public ObservableCollection<BudgetItem> Source { get; } = new();

    [ObservableProperty]
    private BudgetItem? selectedItem;

    [ObservableProperty]
    private decimal monthlyTotal;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteItemCommand))]
    private bool hasSelectedItem;

    public bool HasMonth => _monthDataService.CurrentMonth is not null;

    public BudgetViewModel(IBudgetService budgetService, IMonthDataService monthDataService)
    {
        _budgetService = budgetService;
        _monthDataService = monthDataService;
    }

    public async void OnNavigatedTo(object parameter)
    {
        await LoadItemsAsync();
    }

    public void OnNavigatedFrom() { }

    partial void OnSelectedItemChanged(BudgetItem? value)
    {
        HasSelectedItem = value is not null;
    }

    public async Task AddItemAsync(BudgetItem item)
    {
        await _budgetService.AddItemAsync(item);
        Source.Add(item);
        RecalcTotal();
    }

    public async Task UpdateItemAsync(BudgetItem item)
    {
        await _budgetService.UpdateItemAsync(item);
        var existing = Source.FirstOrDefault(x => x.Id == item.Id);
        if (existing is null) return;
        var idx = Source.IndexOf(existing);
        Source.RemoveAt(idx);
        Source.Insert(idx, item);
        RecalcTotal();
    }

    [RelayCommand(CanExecute = nameof(HasSelectedItem))]
    private async Task DeleteItemAsync()
    {
        if (SelectedItem is null) return;
        await _budgetService.DeleteItemAsync(SelectedItem.Id);
        Source.Remove(SelectedItem);
        SelectedItem = null;
        RecalcTotal();
    }

    public void AddToCurrentMonth(BudgetItem item)
    {
        var month = _monthDataService.CurrentMonth;
        if (month is null) return;

        var day = Math.Min(item.DayOfMonth, DateTime.DaysInMonth(month.Year, month.Month));
        _monthDataService.AddTransaction(new MonthTransaction
        {
            Date = new DateOnly(month.Year, month.Month, day),
            Name = item.Name,
            Amount = item.Amount,
            IsCredit = item.IsCredit,
            Status = TransactionStatus.Expected
        });
    }

    private async Task LoadItemsAsync()
    {
        Source.Clear();
        var items = await _budgetService.GetItemsAsync();
        foreach (var item in items.OrderBy(i => i.DayOfMonth))
            Source.Add(item);
        RecalcTotal();
    }

    private void RecalcTotal()
    {
        MonthlyTotal = Source.Sum(i => i.IsCredit ? i.Amount : -i.Amount);
    }
}
