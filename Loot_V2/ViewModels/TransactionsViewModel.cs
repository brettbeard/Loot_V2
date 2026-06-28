using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;

using Loot_V2.Contracts.ViewModels;
using Loot_V2.Core.Contracts.Services;
using Loot_V2.Core.Models;
using Loot_V2.Messages;

namespace Loot_V2.ViewModels;

public partial class TransactionsViewModel : ObservableRecipient, INavigationAware
{
    private readonly IMonthDataService _monthDataService;
    private readonly IBudgetService _budgetService;

    public ObservableCollection<MonthTransaction> Source { get; } = new();

    [ObservableProperty]
    private bool isDirty;

    [ObservableProperty]
    private bool hasMonth;

    [ObservableProperty]
    private string statusText = string.Empty;

    [ObservableProperty]
    private decimal currentBalance;

    public TransactionsViewModel(IMonthDataService monthDataService, IBudgetService budgetService)
    {
        _monthDataService = monthDataService;
        _budgetService = budgetService;

        _monthDataService.DataChanged += OnDataChanged;
        _monthDataService.OFXDataLoaded += OnOFXDataLoaded;
    }

    public void OnNavigatedTo(object parameter)
    {
        RefreshSource();
        UpdateTitle();
    }

    public void OnNavigatedFrom() { }

    public Task<IList<BudgetItem>> GetBudgetItemsAsync() => _budgetService.GetItemsAsync();

    public void StartNewMonth(int year, int month, IEnumerable<BudgetItem> selectedItems)
    {
        _monthDataService.NewMonth(year, month, selectedItems);
        UpdateTitle();
    }

    public MonthData? OpenMonth(string filePath)
    {
        var data = _monthDataService.Open(filePath);
        RefreshSource();
        UpdateTitle();
        return data;
    }

    public void SaveMonth(string filePath)
    {
        _monthDataService.Save(filePath);
        UpdateTitle();
    }

    public bool IsDirtyCheck() => _monthDataService.IsDirty;

    public string? CurrentFilePath => _monthDataService.CurrentFilePath;

    public MonthData? CurrentMonth => _monthDataService.CurrentMonth;

    public IList<OFXImportTransaction> GetUnmatchedOFXTransactions()
    {
        return _monthDataService.CurrentMonth?.OFXTransactions
            .Where(o => !o.IsMatched && !o.IsHidden)
            .ToList() ?? new List<OFXImportTransaction>();
    }

    public void UpdateTransaction(MonthTransaction transaction)
    {
        _monthDataService.UpdateTransaction(transaction);
    }

    public void DeleteTransaction(Guid id)
    {
        _monthDataService.DeleteTransaction(id);
    }

    public void MatchTransaction(Guid transactionId, OFXImportTransaction ofxTransaction)
    {
        _monthDataService.MatchTransaction(transactionId, ofxTransaction);
        WeakReferenceMessenger.Default.Send(new TransactionMatchedMessage
        {
            TransactionId = transactionId,
            OFXTransactionId = ofxTransaction.Id
        });
    }

    private void OnDataChanged(object? sender, EventArgs e)
    {
        IsDirty = _monthDataService.IsDirty;
        HasMonth = _monthDataService.CurrentMonth is not null;
        RefreshSource();
        UpdateTitle();
    }

    private void OnOFXDataLoaded(object? sender, EventArgs e) { }

    private void RefreshSource()
    {
        Source.Clear();
        if (_monthDataService.CurrentMonth is null)
        {
            HasMonth = false;
            CurrentBalance = 0m;
            return;
        }
        HasMonth = true;

        var sorted = _monthDataService.CurrentMonth.Transactions
            .OrderBy(t => t.IsStartingBalance ? 0 : 1)
            .ThenBy(t => t.Date)
            .ToList();

        foreach (var tx in sorted)
            Source.Add(new MonthTransaction
            {
                Id = tx.Id,
                Date = tx.Date,
                Name = tx.Name,
                Amount = tx.Amount,
                IsCredit = tx.IsCredit,
                Status = tx.Status,
                OFXTransactionId = tx.OFXTransactionId,
                RunningBalance = tx.RunningBalance,
                IsStartingBalance = tx.IsStartingBalance
            });

        CurrentBalance = Source.Count > 0 ? Source[^1].RunningBalance : 0m;
        IsDirty = _monthDataService.IsDirty;
        StatusText = _monthDataService.IsDirty ? "Unsaved changes" : string.Empty;
    }

    private void UpdateTitle()
    {
        string fileName = _monthDataService.CurrentFilePath is not null
            ? Path.GetFileName(_monthDataService.CurrentFilePath)
            : "(unsaved)";

        string dirty = _monthDataService.IsDirty ? " *" : string.Empty;

        string title = _monthDataService.CurrentMonth is not null
            ? $"Loot V2 — {fileName}{dirty}"
            : "Loot V2";

        WeakReferenceMessenger.Default.Send(new TitleChangedMessage(title));
    }
}
