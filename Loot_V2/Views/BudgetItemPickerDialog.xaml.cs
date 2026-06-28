using Loot_V2.Core.Models;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Loot_V2.Views;

public sealed partial class BudgetItemPickerDialog : ContentDialog
{
    public class BudgetItemSelection
    {
        public BudgetItem Item { get; set; } = null!;
        public bool IsSelected { get; set; } = true;
        public double EditAmount { get; set; }
        public string TypeDisplay => Item.IsCredit ? "Income" : "Expense";
    }

    private readonly List<BudgetItemSelection> _selections;

    public IEnumerable<BudgetItem> SelectedItems =>
        _selections.Where(s => s.IsSelected).Select(s => new BudgetItem
        {
            Id = s.Item.Id,
            Name = s.Item.Name,
            Amount = double.IsNaN(s.EditAmount) ? 0m : (decimal)s.EditAmount,
            IsCredit = s.Item.IsCredit,
            DayOfMonth = s.Item.DayOfMonth
        });

    public int Year => YearCombo.SelectedItem is int y ? y : DateTime.Now.Year;
    public int Month => MonthCombo.SelectedIndex >= 0 ? MonthCombo.SelectedIndex + 1 : 1;

    public BudgetItemPickerDialog(IList<BudgetItem> items)
    {
        InitializeComponent();

        _selections = items
            .OrderBy(i => i.DayOfMonth)
            .Select(i => new BudgetItemSelection { Item = i, EditAmount = (double)i.Amount })
            .ToList();

        ItemsControl.ItemsSource = _selections;

        var now = DateTime.Now;
        var defaultMonth = now.Month == 12 ? 1 : now.Month + 1;
        var defaultYear  = now.Month == 12 ? now.Year + 1 : now.Year;

        var monthNames = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.MonthNames;
        for (var m = 1; m <= 12; m++)
            MonthCombo.Items.Add(monthNames[m - 1]);
        MonthCombo.SelectedIndex = defaultMonth - 1;

        for (var y = now.Year - 1; y <= now.Year + 5; y++)
            YearCombo.Items.Add(y);
        YearCombo.SelectedItem = defaultYear;
    }

    private void OnSelectAllClicked(object sender, RoutedEventArgs e) => SetAllSelected(true);

    private void OnSelectNoneClicked(object sender, RoutedEventArgs e) => SetAllSelected(false);

    private void SetAllSelected(bool selected)
    {
        foreach (var s in _selections)
            s.IsSelected = selected;
        ItemsControl.ItemsSource = null;
        ItemsControl.ItemsSource = _selections;
    }
}
