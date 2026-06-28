using CommunityToolkit.WinUI.UI.Controls;

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

public sealed partial class ImportsPage : Page
{
    public ImportsViewModel ViewModel { get; }

    private OFXImportTransaction? _rightClickedTransaction;
    private readonly MenuFlyout _contextMenu;
    private readonly MenuFlyoutItem _addMenuItem;
    private readonly MenuFlyoutItem _hideMenuItem;
    private readonly MenuFlyoutItem _unhideMenuItem;
    private string? _sortTag;
    private bool _sortAscending = true;

    public ImportsPage()
    {
        ViewModel = App.GetService<ImportsViewModel>();
        InitializeComponent();

        _addMenuItem = new MenuFlyoutItem { Text = "Add to Transactions" };
        _addMenuItem.Click += OnAddToTransactionsClicked;

        _hideMenuItem = new MenuFlyoutItem { Text = "Hide" };
        _hideMenuItem.Click += OnHideClicked;

        _unhideMenuItem = new MenuFlyoutItem { Text = "Unhide" };
        _unhideMenuItem.Click += OnUnhideClicked;

        _contextMenu = new MenuFlyout();
        _contextMenu.Items.Add(_addMenuItem);
        _contextMenu.Items.Add(_hideMenuItem);
        _contextMenu.Items.Add(_unhideMenuItem);
    }

    private async void OnLoadOFXClicked(object sender, RoutedEventArgs e)
    {
        var filePicker = new FileOpenPicker();
        InitializeWithWindow.Initialize(filePicker, WindowNative.GetWindowHandle(App.MainWindow));
        filePicker.FileTypeFilter.Add(".ofx");
        filePicker.FileTypeFilter.Add(".qfx");
        filePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

        var file = await filePicker.PickSingleFileAsync();
        if (file is null) return;

        try
        {
            await ViewModel.LoadOFXFileAsync(file.Path);
        }
        catch (Exception ex)
        {
            var dialog = new ContentDialog
            {
                Title = "OFX Import Error",
                Content = $"Could not parse the OFX file:\n{ex.Message}",
                CloseButtonText = "OK",
                XamlRoot = XamlRoot
            };
            await dialog.ShowAsync();
        }
    }

    private void OnLoadingRow(object sender, CommunityToolkit.WinUI.UI.Controls.DataGridRowEventArgs e)
    {
        if (e.Row.DataContext is OFXImportTransaction tx)
        {
            e.Row.Background = tx.IsHidden
                ? new SolidColorBrush(Color.FromArgb(255, 80, 80, 85))
                : tx.IsMatched
                    ? new SolidColorBrush(Color.FromArgb(128, 180, 180, 180))
                    : new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
        }
    }

    private void OnGridRightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        if (e.OriginalSource is FrameworkElement fe)
        {
            _rightClickedTransaction = FindTransactionInParent(fe);

            if (_rightClickedTransaction is null || !ViewModel.HasMonth) return;

            var hidden = _rightClickedTransaction.IsHidden;
            _addMenuItem.Visibility    = (!hidden && !_rightClickedTransaction.IsMatched) ? Visibility.Visible : Visibility.Collapsed;
            _hideMenuItem.Visibility   = !hidden ? Visibility.Visible : Visibility.Collapsed;
            _unhideMenuItem.Visibility = hidden  ? Visibility.Visible : Visibility.Collapsed;

            _contextMenu.ShowAt(OFXGrid, e.GetPosition(OFXGrid));
            e.Handled = true;
        }
    }

    private static OFXImportTransaction? FindTransactionInParent(FrameworkElement element)
    {
        var current = element as DependencyObject;
        while (current is not null)
        {
            if (current is FrameworkElement fe && fe.DataContext is OFXImportTransaction tx)
                return tx;
            current = VisualTreeHelper.GetParent(current);
        }
        return null;
    }

    private async void OnAddToTransactionsClicked(object sender, RoutedEventArgs e)
    {
        if (_rightClickedTransaction is null) return;
        var tx = _rightClickedTransaction;
        _rightClickedTransaction = null;

        var dialog = new AddImportedTransactionDialog(tx);
        dialog.XamlRoot = XamlRoot;
        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            ViewModel.AddToTransactions(tx, dialog.EditedName);
            ApplyCurrentSort();
        }
    }

    private void OnHideClicked(object sender, RoutedEventArgs e)
    {
        if (_rightClickedTransaction is null) return;
        ViewModel.SetTransactionHidden(_rightClickedTransaction.Id, true);
        _rightClickedTransaction = null;
    }

    private void OnUnhideClicked(object sender, RoutedEventArgs e)
    {
        if (_rightClickedTransaction is null) return;
        ViewModel.SetTransactionHidden(_rightClickedTransaction.Id, false);
        _rightClickedTransaction = null;
    }

    private void OnGridSorting(object sender, DataGridColumnEventArgs e)
    {
        var direction = e.Column.SortDirection == DataGridSortDirection.Ascending
            ? DataGridSortDirection.Descending
            : DataGridSortDirection.Ascending;

        foreach (var col in OFXGrid.Columns)
            col.SortDirection = null;
        e.Column.SortDirection = direction;

        _sortTag = e.Column.Tag as string;
        _sortAscending = direction == DataGridSortDirection.Ascending;

        ApplyCurrentSort();
    }

    private void ApplyCurrentSort()
    {
        if (_sortTag is null) return;

        var items = ViewModel.Source.ToList();
        var sorted = (_sortTag, _sortAscending) switch
        {
            ("Date",      true)  => items.OrderBy(i => i.Date).AsEnumerable(),
            ("Date",      false) => items.OrderByDescending(i => i.Date),
            ("Name",      true)  => items.OrderBy(i => i.Name),
            ("Name",      false) => items.OrderByDescending(i => i.Name),
            ("Amount",    true)  => items.OrderBy(i => i.Amount),
            ("Amount",    false) => items.OrderByDescending(i => i.Amount),
            ("IsMatched", true)  => items.OrderBy(i => i.IsMatched),
            ("IsMatched", false) => items.OrderByDescending(i => i.IsMatched),
            _ => items.AsEnumerable()
        };

        var list = sorted.ToList();
        ViewModel.Source.Clear();
        foreach (var item in list)
            ViewModel.Source.Add(item);
    }
}
