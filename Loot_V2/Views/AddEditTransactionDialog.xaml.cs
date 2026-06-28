using Loot_V2.Core.Models;

using Microsoft.UI.Xaml.Controls;

namespace Loot_V2.Views;

public sealed partial class AddEditTransactionDialog : ContentDialog
{
    public MonthTransaction Result { get; }

    public AddEditTransactionDialog(MonthTransaction transaction)
    {
        InitializeComponent();

        Result = new MonthTransaction
        {
            Id = transaction.Id,
            Date = transaction.Date,
            Name = transaction.Name,
            Amount = transaction.Amount,
            IsCredit = transaction.IsCredit,
            Status = transaction.Status,
            OFXTransactionId = transaction.OFXTransactionId,
            IsStartingBalance = transaction.IsStartingBalance
        };

        if (transaction.IsStartingBalance)
        {
            NameBox.IsEnabled = false;
            DatePicker.IsEnabled = false;
            IsCreditBox.IsEnabled = false;
        }

        NameBox.Text = transaction.Name;
        DatePicker.Date = new DateTimeOffset(transaction.Date.Year, transaction.Date.Month, transaction.Date.Day,
            0, 0, 0, TimeSpan.Zero);
        AmountBox.Value = (double)transaction.Amount;
        IsCreditBox.IsChecked = transaction.IsCredit;

        PrimaryButtonClick += OnPrimaryButtonClick;
    }

    private void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (!Result.IsStartingBalance && string.IsNullOrWhiteSpace(NameBox.Text))
        {
            args.Cancel = true;
            return;
        }

        if (!Result.IsStartingBalance)
        {
            Result.Name = NameBox.Text.Trim();
            Result.Date = DatePicker.Date.HasValue
                ? DateOnly.FromDateTime(DatePicker.Date.Value.Date)
                : Result.Date;
            Result.IsCredit = IsCreditBox.IsChecked == true;
        }

        Result.Amount = double.IsNaN(AmountBox.Value) ? 0m : (decimal)AmountBox.Value;
    }
}
