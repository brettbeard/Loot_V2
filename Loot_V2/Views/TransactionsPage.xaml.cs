using CommunityToolkit.WinUI.UI.Controls;

using Loot_V2.Core.Contracts.Services;
using Loot_V2.Core.Models;
using Loot_V2.ViewModels;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

using Windows.Storage.Pickers;
using Windows.UI;

using WinRT.Interop;

namespace Loot_V2.Views;

public sealed partial class TransactionsPage : Page
{
    public TransactionsViewModel ViewModel { get; }

    private MonthTransaction? _rightClickedTransaction;
    private readonly MenuFlyout _contextMenu;
    private readonly MenuFlyoutItem _importMenuItem;
    private readonly MenuFlyoutItem _deleteMenuItem;

    public TransactionsPage()
    {
        ViewModel = App.GetService<TransactionsViewModel>();
        InitializeComponent();

        var editItem = new MenuFlyoutItem { Text = "Edit" };
        editItem.Click += OnEditMenuItemClicked;

        _importMenuItem = new MenuFlyoutItem { Text = "Import (Match OFX Transaction)" };
        _importMenuItem.Click += OnImportMenuItemClicked;

        _deleteMenuItem = new MenuFlyoutItem { Text = "Delete" };
        _deleteMenuItem.Click += OnDeleteMenuItemClicked;

        _contextMenu = new MenuFlyout();
        _contextMenu.Items.Add(editItem);
        _contextMenu.Items.Add(_importMenuItem);
        _contextMenu.Items.Add(_deleteMenuItem);
    }

    private async void OnNewMonthClicked(object sender, RoutedEventArgs e)
    {
        if (!await ConfirmDiscardChangesAsync()) return;

        var budgetItems = await ViewModel.GetBudgetItemsAsync();

        var picker = new BudgetItemPickerDialog(budgetItems.ToList());
        picker.XamlRoot = XamlRoot;
        var result = await picker.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            ViewModel.StartNewMonth(picker.Year, picker.Month, picker.SelectedItems);
        }
    }

    private async void OnOpenClicked(object sender, RoutedEventArgs e)
    {
        if (!await ConfirmDiscardChangesAsync()) return;

        var filePicker = new FileOpenPicker();
        InitializeWithWindow.Initialize(filePicker, WindowNative.GetWindowHandle(App.MainWindow));
        filePicker.FileTypeFilter.Add(".lootv2");
        filePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

        var file = await filePicker.PickSingleFileAsync();
        if (file is not null)
        {
            var data = ViewModel.OpenMonth(file.Path);
            if (data is null)
            {
                var err = new ContentDialog
                {
                    Title = "Error",
                    Content = "Could not open the selected file.",
                    CloseButtonText = "OK",
                    XamlRoot = XamlRoot
                };
                await err.ShowAsync();
            }
        }
    }

    private async void OnSaveClicked(object sender, RoutedEventArgs e)
    {
        if (ViewModel.CurrentFilePath is not null)
            ViewModel.SaveMonth(ViewModel.CurrentFilePath);
        else
            await ShowSaveAsDialogAsync();
    }

    private async void OnSaveAsClicked(object sender, RoutedEventArgs e)
    {
        await ShowSaveAsDialogAsync();
    }

    private async Task ShowSaveAsDialogAsync()
    {
        var month = ViewModel.CurrentMonth;
        if (month is null) return;

        var filePicker = new FileSavePicker();
        InitializeWithWindow.Initialize(filePicker, WindowNative.GetWindowHandle(App.MainWindow));
        filePicker.FileTypeChoices.Add("Loot V2 File", new List<string> { ".lootv2" });
        filePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        filePicker.SuggestedFileName = $"{month.Year}-{month.Month:D2}";

        var file = await filePicker.PickSaveFileAsync();
        if (file is not null)
            ViewModel.SaveMonth(file.Path);
    }

    private async Task<bool> ConfirmDiscardChangesAsync()
    {
        if (!ViewModel.IsDirtyCheck()) return true;

        var dialog = new ContentDialog
        {
            Title = "Unsaved Changes",
            Content = "You have unsaved changes. Save before continuing?",
            PrimaryButtonText = "Save",
            SecondaryButtonText = "Discard",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot
        };

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            if (ViewModel.CurrentFilePath is not null)
                ViewModel.SaveMonth(ViewModel.CurrentFilePath);
            else
                await ShowSaveAsDialogAsync();
            return true;
        }
        else if (result == ContentDialogResult.Secondary)
        {
            return true;
        }

        return false;
    }

    private void OnLoadingRow(object sender, CommunityToolkit.WinUI.UI.Controls.DataGridRowEventArgs e)
    {
        if (e.Row.DataContext is MonthTransaction tx)
        {
            e.Row.Background = tx.Status switch
            {
                TransactionStatus.Reconciled => new SolidColorBrush(Color.FromArgb(255, 56, 142, 60)),
                TransactionStatus.Unexpected => new SolidColorBrush(Color.FromArgb(255, 200, 120, 0)),
                _ => new SolidColorBrush(Color.FromArgb(0, 0, 0, 0))
            };
        }
    }

    private void OnGridRightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        if (e.OriginalSource is FrameworkElement fe)
        {
            _rightClickedTransaction = FindTransactionInParent(fe);
            if (_rightClickedTransaction is null) return;

            var canImport = _rightClickedTransaction is { Status: TransactionStatus.Expected, IsStartingBalance: false }
                && ViewModel.CurrentMonth?.OFXTransactions.Any(o => !o.IsMatched) == true;
            _importMenuItem.Visibility = canImport ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;

            _deleteMenuItem.Visibility = _rightClickedTransaction.IsStartingBalance
                ? Microsoft.UI.Xaml.Visibility.Collapsed
                : Microsoft.UI.Xaml.Visibility.Visible;

            _contextMenu.ShowAt(TransactionsGrid, e.GetPosition(TransactionsGrid));
            e.Handled = true;
        }
    }

    private static MonthTransaction? FindTransactionInParent(FrameworkElement element)
    {
        var current = element as DependencyObject;
        while (current is not null)
        {
            if (current is FrameworkElement fe && fe.DataContext is MonthTransaction tx)
                return tx;
            current = VisualTreeHelper.GetParent(current);
        }
        return null;
    }

    private void OnGridSorting(object sender, DataGridColumnEventArgs e)
    {
        var direction = e.Column.SortDirection == DataGridSortDirection.Ascending
            ? DataGridSortDirection.Descending
            : DataGridSortDirection.Ascending;

        foreach (var col in TransactionsGrid.Columns)
            col.SortDirection = null;
        e.Column.SortDirection = direction;

        var ascending = direction == DataGridSortDirection.Ascending;
        var items = ViewModel.Source.ToList();

        var sorted = e.Column.Tag as string switch
        {
            "Date"          => ascending ? items.OrderBy(i => i.Date)           : items.OrderByDescending(i => i.Date),
            "Name"          => ascending ? items.OrderBy(i => i.Name)           : items.OrderByDescending(i => i.Name),
            "Amount"        => ascending ? items.OrderBy(i => i.SignedAmount)     : items.OrderByDescending(i => i.SignedAmount),
            "Status"        => ascending ? items.OrderBy(i => i.Status)         : items.OrderByDescending(i => i.Status),
            "RunningBalance"=> ascending ? items.OrderBy(i => i.RunningBalance) : items.OrderByDescending(i => i.RunningBalance),
            _               => items.AsEnumerable()
        };

        var list = sorted.ToList();
        ViewModel.Source.Clear();
        foreach (var item in list)
            ViewModel.Source.Add(item);
    }

    private async void OnGridDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (e.OriginalSource is FrameworkElement fe)
        {
            var tx = FindTransactionInParent(fe);
            if (tx is not null)
                await EditTransactionAsync(tx);
        }
    }

    private async void OnEditMenuItemClicked(object sender, RoutedEventArgs e)
    {
        if (_rightClickedTransaction is not null)
            await EditTransactionAsync(_rightClickedTransaction);
        _rightClickedTransaction = null;
    }

    private async Task EditTransactionAsync(MonthTransaction transaction)
    {
        var dialog = new AddEditTransactionDialog(transaction);
        dialog.XamlRoot = XamlRoot;
        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
            ViewModel.UpdateTransaction(dialog.Result);
    }

    private async void OnImportMenuItemClicked(object sender, RoutedEventArgs e)
    {
        if (_rightClickedTransaction is null) return;

        var unmatched = ViewModel.GetUnmatchedOFXTransactions();
        if (unmatched.Count == 0)
        {
            var info = new ContentDialog
            {
                Title = "No OFX Transactions",
                Content = "No unmatched OFX transactions available. Load an OFX file on the Imports page first.",
                CloseButtonText = "OK",
                XamlRoot = XamlRoot
            };
            await info.ShowAsync();
            return;
        }

        var ranked = App.GetService<IMatchingService>()
            .RankCandidates(_rightClickedTransaction, unmatched);

        var dialog = new MatchTransactionDialog(_rightClickedTransaction, ranked);
        dialog.XamlRoot = XamlRoot;
        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary && dialog.SelectedTransaction is not null)
        {
            ViewModel.MatchTransaction(_rightClickedTransaction.Id, dialog.SelectedTransaction);
        }

        _rightClickedTransaction = null;
    }

    private async void OnDeleteMenuItemClicked(object sender, RoutedEventArgs e)
    {
        if (_rightClickedTransaction is null) return;
        var tx = _rightClickedTransaction;
        _rightClickedTransaction = null;

        var confirm = new ContentDialog
        {
            Title = "Delete Transaction",
            Content = $"Delete '{tx.Name}'? This cannot be undone.",
            PrimaryButtonText = "Delete",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot
        };

        var result = await confirm.ShowAsync();
        if (result == ContentDialogResult.Primary)
            ViewModel.DeleteTransaction(tx.Id);
    }
}
