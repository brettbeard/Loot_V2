using CommunityToolkit.WinUI.UI.Controls;

using Loot_V2.Core.Models;
using Loot_V2.ViewModels;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace Loot_V2.Views;

public sealed partial class BudgetPage : Page
{
    public BudgetViewModel ViewModel { get; }

    private BudgetItem? _rightClickedItem;
    private readonly MenuFlyout _contextMenu;

    public BudgetPage()
    {
        ViewModel = App.GetService<BudgetViewModel>();
        InitializeComponent();

        var addItem = new MenuFlyoutItem { Text = "Add to Transactions" };
        addItem.Click += OnAddToTransactionsClicked;
        _contextMenu = new MenuFlyout();
        _contextMenu.Items.Add(addItem);
    }

    private async void OnAddClicked(object sender, RoutedEventArgs e)
    {
        var dialog = new AddEditBudgetItemDialog();
        dialog.XamlRoot = XamlRoot;
        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            await ViewModel.AddItemAsync(dialog.Result);
        }
    }

    private void OnGridSorting(object sender, DataGridColumnEventArgs e)
    {
        var direction = e.Column.SortDirection == DataGridSortDirection.Ascending
            ? DataGridSortDirection.Descending
            : DataGridSortDirection.Ascending;

        foreach (var col in BudgetGrid.Columns)
            col.SortDirection = null;
        e.Column.SortDirection = direction;

        var ascending = direction == DataGridSortDirection.Ascending;
        var items = ViewModel.Source.ToList();

        var sorted = e.Column.Tag as string switch
        {
            "Name"      => ascending ? items.OrderBy(i => i.Name)       : items.OrderByDescending(i => i.Name),
            "IsCredit"  => ascending ? items.OrderBy(i => i.IsCredit)   : items.OrderByDescending(i => i.IsCredit),
            "Amount"    => ascending ? items.OrderBy(i => i.Amount)     : items.OrderByDescending(i => i.Amount),
            "DayOfMonth"=> ascending ? items.OrderBy(i => i.DayOfMonth) : items.OrderByDescending(i => i.DayOfMonth),
            _           => items.OrderBy(i => i.DayOfMonth).AsEnumerable()
        };

        var list = sorted.ToList();
        ViewModel.Source.Clear();
        foreach (var item in list)
            ViewModel.Source.Add(item);
    }

    private async void OnGridDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        await EditSelectedItemAsync();
    }

    private async void OnEditClicked(object sender, RoutedEventArgs e) => await EditSelectedItemAsync();

    private async Task EditSelectedItemAsync()
    {
        if (ViewModel.SelectedItem is null) return;

        var dialog = new AddEditBudgetItemDialog(ViewModel.SelectedItem);
        dialog.XamlRoot = XamlRoot;
        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            await ViewModel.UpdateItemAsync(dialog.Result);
        }
    }

    private void OnGridRightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        if (!ViewModel.HasMonth) return;
        if (e.OriginalSource is FrameworkElement fe)
        {
            _rightClickedItem = FindItemInParent(fe);
            if (_rightClickedItem is null) return;
            _contextMenu.ShowAt(BudgetGrid, e.GetPosition(BudgetGrid));
            e.Handled = true;
        }
    }

    private static BudgetItem? FindItemInParent(FrameworkElement element)
    {
        var current = element as DependencyObject;
        while (current is not null)
        {
            if (current is FrameworkElement f && f.DataContext is BudgetItem item)
                return item;
            current = VisualTreeHelper.GetParent(current);
        }
        return null;
    }

    private void OnAddToTransactionsClicked(object sender, RoutedEventArgs e)
    {
        if (_rightClickedItem is null) return;
        ViewModel.AddToCurrentMonth(_rightClickedItem);
        _rightClickedItem = null;
    }

    private async void OnDeleteClicked(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedItem is null) return;

        var confirm = new ContentDialog
        {
            Title = "Delete Budget Item",
            Content = $"Delete '{ViewModel.SelectedItem.Name}'?",
            PrimaryButtonText = "Delete",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot
        };

        var result = await confirm.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            await ViewModel.DeleteItemCommand.ExecuteAsync(null);
        }
    }
}
