using Loot_V2.Core.Models;

using Microsoft.UI.Xaml.Controls;

namespace Loot_V2.Views;

public sealed partial class MatchTransactionDialog : ContentDialog
{
    public class OFXDisplayItem
    {
        public OFXImportTransaction Source { get; set; } = null!;
        public string DateDisplay => Source.Date.ToString("MM/dd/yyyy");
        public string Name => Source.Name;
        public string TypeDisplay => Source.Amount > 0 ? "Income" : "Expense";
        public string AmountDisplay => Math.Abs(Source.Amount).ToString("C");
    }

    public OFXImportTransaction? SelectedTransaction { get; private set; }

    public MatchTransactionDialog(MonthTransaction expected, IList<OFXImportTransaction> rankedCandidates)
    {
        InitializeComponent();

        Title = $"Match: {expected.Name}";
        var expectedType = expected.IsCredit ? "Income" : "Expense";
        SubtitleText.Text = $"Expected {expected.Amount:C} ({expectedType}) around day {expected.Date.Day}. Select the matching bank transaction.";

        var displayItems = rankedCandidates
            .Select(t => new OFXDisplayItem { Source = t })
            .ToList();

        CandidatesGrid.ItemsSource = displayItems;

        if (displayItems.Count > 0)
        {
            CandidatesGrid.SelectedItem = displayItems[0];
            SelectedTransaction = displayItems[0].Source;
        }

        IsPrimaryButtonEnabled = displayItems.Count > 0;
    }

    private void OnGridSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CandidatesGrid.SelectedItem is OFXDisplayItem item)
        {
            SelectedTransaction = item.Source;
            IsPrimaryButtonEnabled = true;
        }
    }
}
