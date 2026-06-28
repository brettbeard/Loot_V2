using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;

using Loot_V2.Contracts.ViewModels;
using Loot_V2.Core.Contracts.Services;
using Loot_V2.Core.Models;
using Loot_V2.Messages;

namespace Loot_V2.ViewModels;

public partial class ImportsViewModel : ObservableRecipient, INavigationAware
{
    private readonly IMonthDataService _monthDataService;
    private readonly IOFXImportService _ofxImportService;

    public ObservableCollection<OFXImportTransaction> Source { get; } = new();

    [ObservableProperty]
    private bool hasMonth;

    [ObservableProperty]
    private bool hasOFXData;

    [ObservableProperty]
    private OFXImportTransaction? selectedTransaction;

    [ObservableProperty]
    private bool showHidden;

    [ObservableProperty]
    private bool showMatched = true;

    public ImportsViewModel(IMonthDataService monthDataService, IOFXImportService ofxImportService)
    {
        _monthDataService = monthDataService;
        _ofxImportService = ofxImportService;

        _monthDataService.DataChanged += OnDataChanged;
        _monthDataService.OFXDataLoaded += OnDataChanged;
    }

    public void OnNavigatedTo(object parameter)
    {
        RefreshSource();
    }

    public void OnNavigatedFrom() { }

    partial void OnShowHiddenChanged(bool value) => RefreshSource();
    partial void OnShowMatchedChanged(bool value) => RefreshSource();

    public void SetTransactionHidden(string ofxId, bool hidden)
    {
        _monthDataService.SetOFXTransactionHidden(ofxId, hidden);

        var item = Source.FirstOrDefault(t => t.Id == ofxId);
        if (item is null) return;

        if (hidden && !ShowHidden)
        {
            Source.Remove(item);
        }
        else
        {
            var idx = Source.IndexOf(item);
            Source.RemoveAt(idx);
            Source.Insert(idx, item);
        }
    }

    public async Task LoadOFXFileAsync(string filePath)
    {
        var (transactions, ledgerBalance) = await _ofxImportService.ParseAsync(filePath);
        _monthDataService.SetOFXData(transactions, ledgerBalance);
    }

    public void AddToTransactions(OFXImportTransaction tx, string customName)
    {
        _monthDataService.AddUnexpectedTransaction(tx, customName);

        WeakReferenceMessenger.Default.Send(new UnexpectedTransactionAddedMessage
        {
            OFXTransactionId = tx.Id
        });

        RefreshSource();
    }

    private void OnDataChanged(object? sender, EventArgs e)
    {
        HasMonth = _monthDataService.CurrentMonth is not null;
        RefreshSource();
    }

    private void RefreshSource()
    {
        Source.Clear();
        HasMonth = _monthDataService.CurrentMonth is not null;
        HasOFXData = _monthDataService.CurrentMonth?.OFXTransactions.Count > 0;

        if (_monthDataService.CurrentMonth is null) return;

        foreach (var tx in _monthDataService.CurrentMonth.OFXTransactions
            .Where(t => (ShowHidden || !t.IsHidden) && (ShowMatched || !t.IsMatched)))
            Source.Add(tx);
    }
}
