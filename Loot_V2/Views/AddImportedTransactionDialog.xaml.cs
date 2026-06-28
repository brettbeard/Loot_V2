using Loot_V2.Core.Models;

using Microsoft.UI.Xaml.Controls;

namespace Loot_V2.Views;

public sealed partial class AddImportedTransactionDialog : ContentDialog
{
    public string EditedName => NameBox.Text;

    public AddImportedTransactionDialog(OFXImportTransaction transaction)
    {
        InitializeComponent();

        NameBox.Text = transaction.Name;
        DateText.Text = transaction.Date.ToString("MMM d, yyyy");
        AmountText.Text = Math.Abs(transaction.Amount).ToString("C");
        TypeText.Text = transaction.Amount > 0 ? "Income" : "Expense";
    }
}
