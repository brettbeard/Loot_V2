using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;

using Loot_V2.Contracts.ViewModels;
using Loot_V2.Core.Contracts.Services;
using Loot_V2.Core.Models;

namespace Loot_V2.ViewModels;

public partial class MainViewModel : ObservableRecipient, INavigationAware
{
    private readonly ISampleDataService _sampleDataService;

    public ObservableCollection<SampleOrder> Source { get; } = new ObservableCollection<SampleOrder>();

    public MainViewModel(ISampleDataService sampleDataService)
    {
        _sampleDataService = sampleDataService;
    }

    public async void OnNavigatedTo(object parameter)
    {
        Source.Clear();

        // TODO: Replace with real data.
        var data = await _sampleDataService.GetGridDataAsync();

        foreach (var item in data)
        {
            Source.Add(item);
        }
    }

    public void OnNavigatedFrom()
    {
    }
}
