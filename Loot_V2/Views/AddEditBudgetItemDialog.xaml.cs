using Loot_V2.Core.Models;

using Microsoft.UI.Xaml.Controls;

namespace Loot_V2.Views;

public sealed partial class AddEditBudgetItemDialog : ContentDialog
{
    public BudgetItem Result { get; private set; } = new();

    public AddEditBudgetItemDialog(BudgetItem? existing = null)
    {
        InitializeComponent();
        Title = existing is null ? "Add Budget Item" : "Edit Budget Item";

        if (existing is not null)
        {
            Result = new BudgetItem
            {
                Id = existing.Id,
                Name = existing.Name,
                Amount = existing.Amount,
                IsCredit = existing.IsCredit,
                DayOfMonth = existing.DayOfMonth
            };
            NameBox.Text = existing.Name;
            AmountBox.Value = (double)existing.Amount;
            IsCreditBox.IsChecked = existing.IsCredit;
            DayBox.Value = existing.DayOfMonth;
        }

        PrimaryButtonClick += OnPrimaryButtonClick;
    }

    private void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (string.IsNullOrWhiteSpace(NameBox.Text))
        {
            args.Cancel = true;
            return;
        }

        Result.Name = NameBox.Text.Trim();
        Result.Amount = double.IsNaN(AmountBox.Value) ? 0m : (decimal)AmountBox.Value;
        Result.IsCredit = IsCreditBox.IsChecked == true;
        Result.DayOfMonth = double.IsNaN(DayBox.Value) ? 1 : (int)DayBox.Value;
    }
}
