using MacMD.Core.Models;
using MacMD.Win.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace MacMD.Win.Views;

public sealed partial class DocumentListView : UserControl
{
    private readonly DispatcherTimer _searchDebounce;
    private bool _switchingMode;

    private static readonly SolidColorBrush SelectionBorderBrush =
        new(Windows.UI.Color.FromArgb(255, 0, 120, 212));
    private static readonly SolidColorBrush SelectionTintBrush =
        new(Windows.UI.Color.FromArgb(40, 0, 120, 212));

    public DocumentListViewModel? ViewModel
    {
        get => _vm;
        set
        {
            _vm = value;
            if (_vm is not null)
                DocumentsList.ItemsSource = _vm.Documents;
        }
    }
    private DocumentListViewModel? _vm;

    public DocumentListView()
    {
        this.InitializeComponent();
        _searchDebounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _searchDebounce.Tick += OnSearchDebounceTick;
    }

    public void SetSelectMode(bool enabled)
    {
        _switchingMode = true;
        try
        {
            DocumentsList.SelectionMode = enabled
                ? ListViewSelectionMode.Multiple
                : ListViewSelectionMode.Single;

            if (!enabled)
            {
                // Reset card visuals before deselecting
                foreach (var item in DocumentsList.Items)
                    ApplySelectionBorder(item, false);

                // SelectedItems is read-only in WinUI 3 — use SelectedIndex instead
                DocumentsList.SelectedIndex = -1;
                ViewModel?.SelectedDocumentIds.Clear();
            }
        }
        finally
        {
            _switchingMode = false;
        }
    }

    private void OnDocumentSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ViewModel is null || _switchingMode) return;

        if (DocumentsList.SelectionMode == ListViewSelectionMode.Multiple)
        {
            ViewModel.SelectedDocumentIds.Clear();
            foreach (var item in DocumentsList.SelectedItems)
                if (item is DocumentSummary d)
                    ViewModel.SelectedDocumentIds.Add(d.Id);

            // Update card border visuals for added/removed selections
            foreach (var item in e.AddedItems)
                ApplySelectionBorder(item, true);
            foreach (var item in e.RemovedItems)
                ApplySelectionBorder(item, false);
        }
        else if (DocumentsList.SelectedItem is DocumentSummary doc)
        {
            ViewModel.SelectedDocument = doc;
        }
    }

    private void ApplySelectionBorder(object item, bool selected)
    {
        if (DocumentsList.ContainerFromItem(item) is not GridViewItem container) return;
        var cardBorder = FindCardBorder(container);
        if (cardBorder is null) return;

        if (selected)
        {
            cardBorder.BorderBrush = SelectionBorderBrush;
            cardBorder.BorderThickness = new Thickness(2);
            cardBorder.Background = SelectionTintBrush;
        }
        else
        {
            cardBorder.BorderThickness = new Thickness(1);
            cardBorder.ClearValue(Border.BorderBrushProperty);   // restores ThemeResource binding
            cardBorder.ClearValue(Border.BackgroundProperty);    // restores ThemeResource binding
        }
    }

    private static Border? FindCardBorder(DependencyObject parent, int depth = 0)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is Border b && b.CornerRadius.TopLeft >= 8)
                return b;
            if (depth < 12)
            {
                var found = FindCardBorder(child, depth + 1);
                if (found is not null) return found;
            }
        }
        return null;
    }

    private void OnNewDocumentClick(object sender, RoutedEventArgs e)
    {
        ViewModel?.CreateDocumentCommand.Execute(null);
    }

    private void OnDeleteDocumentClick(object sender, RoutedEventArgs e)
    {
        ViewModel?.DeleteDocumentCommand.Execute(null);
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        _searchDebounce.Stop();
        _searchDebounce.Start();
    }

    private async void OnSearchDebounceTick(object? sender, object e)
    {
        _searchDebounce.Stop();
        if (ViewModel is null) return;
        await ViewModel.SearchAsync(SearchBox.Text);
    }

    private async void OnDocumentRightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        if (ViewModel is null) return;

        // Identify which item was right-clicked
        var element = e.OriginalSource as FrameworkElement;
        var doc = element?.DataContext as DocumentSummary;
        if (doc is null) return;

        // Select the right-clicked item
        DocumentsList.SelectedItem = doc;
        ViewModel.SelectedDocument = doc;

        // Refresh projects/tags for submenus
        await ViewModel.RefreshContextMenuDataAsync();

        // Build context menu
        var flyout = new MenuFlyout();

        // Rename
        var renameItem = new MenuFlyoutItem { Text = "Rename" };
        renameItem.Click += async (_, _) => await ShowRenameDocumentDialogAsync(doc);
        flyout.Items.Add(renameItem);

        // Duplicate
        var duplicateItem = new MenuFlyoutItem { Text = "Duplicate" };
        duplicateItem.Click += async (_, _) => await ViewModel.DuplicateDocumentAsync(doc.Id);
        flyout.Items.Add(duplicateItem);

        // Toggle Favorite
        var favText = doc.IsFavorite ? "Unfavorite" : "Favorite";
        var favoriteItem = new MenuFlyoutItem { Text = favText };
        favoriteItem.Click += async (_, _) => await ViewModel.ToggleFavoriteAsync(doc.Id);
        flyout.Items.Add(favoriteItem);

        // Archive / Unarchive
        if (ViewModel.CurrentFilter == DocumentFilter.Archived)
        {
            var unarchiveItem = new MenuFlyoutItem { Text = "Unarchive" };
            unarchiveItem.Click += async (_, _) => await ViewModel.UnarchiveDocumentAsync(doc.Id);
            flyout.Items.Add(unarchiveItem);
        }
        else
        {
            var archiveItem = new MenuFlyoutItem { Text = "Archive" };
            archiveItem.Click += async (_, _) => await ViewModel.ArchiveDocumentAsync(doc.Id);
            flyout.Items.Add(archiveItem);
        }

        flyout.Items.Add(new MenuFlyoutSeparator());

        // Move to Project submenu
        var projectSub = new MenuFlyoutSubItem { Text = "Move to Project" };
        var noneItem = new MenuFlyoutItem { Text = "None" };
        noneItem.Click += async (_, _) => await ViewModel.MoveToProjectAsync(doc.Id, null);
        projectSub.Items.Add(noneItem);

        foreach (var project in ViewModel.AvailableProjects)
        {
            var pItem = new MenuFlyoutItem { Text = project.Name };
            var pid = project.Id;
            pItem.Click += async (_, _) => await ViewModel.MoveToProjectAsync(doc.Id, pid);
            projectSub.Items.Add(pItem);
        }
        flyout.Items.Add(projectSub);

        // Apply Tags submenu
        var tagSub = new MenuFlyoutSubItem { Text = "Apply Tags" };
        var docTags = await GetDocumentTagIds(doc.Id);

        foreach (var tag in ViewModel.AvailableTags)
        {
            var tItem = new MenuFlyoutItem();
            var isApplied = docTags.Any(t => t.Value == tag.Id.Value);
            tItem.Text = (isApplied ? "\u2713 " : "    ") + tag.Name;

            // Try to show a colored icon
            try
            {
                var color = ParseColor(tag.Color);
                tItem.Icon = new FontIcon
                {
                    Glyph = "\u25CF",
                    Foreground = new SolidColorBrush(color),
                    FontSize = 12
                };
            }
            catch { /* skip icon if color parse fails */ }

            var tid = tag.Id;
            tItem.Click += async (_, _) =>
            {
                await ViewModel.ToggleTagAsync(doc.Id, tid);
            };
            tagSub.Items.Add(tItem);
        }
        flyout.Items.Add(tagSub);

        flyout.Items.Add(new MenuFlyoutSeparator());

        // Delete
        var deleteItem = new MenuFlyoutItem { Text = "Delete" };
        deleteItem.Click += async (_, _) =>
        {
            ViewModel.SelectedDocument = doc;
            await ViewModel.DeleteDocumentCommand.ExecuteAsync(null);
        };
        flyout.Items.Add(deleteItem);

        flyout.ShowAt(DocumentsList, e.GetPosition(DocumentsList));
    }

    private async Task<IReadOnlyList<TagId>> GetDocumentTagIds(DocumentId docId)
    {
        if (ViewModel is null) return Array.Empty<TagId>();
        // Access the document store through reflection or add a helper
        // For now, use the TagStore indirectly via a method on the ViewModel
        try
        {
            var store = GetDocumentStore();
            if (store is not null)
                return await store.GetDocumentTagIdsAsync(docId);
        }
        catch { }
        return Array.Empty<TagId>();
    }

    private MacMD.Core.Services.DocumentStore? GetDocumentStore()
    {
        // Get from the app's service provider
        return App.Current.Services.GetService(typeof(MacMD.Core.Services.DocumentStore))
            as MacMD.Core.Services.DocumentStore;
    }

    private async Task ShowRenameDocumentDialogAsync(DocumentSummary doc)
    {
        if (ViewModel is null) return;
        var nameBox = new TextBox { Text = doc.Title };
        nameBox.SelectAll();
        var dialog = new ContentDialog
        {
            Title = "Rename Document",
            Content = nameBox,
            PrimaryButtonText = "Rename",
            CloseButtonText = "Cancel",
            XamlRoot = this.XamlRoot,
            DefaultButton = ContentDialogButton.Primary,
        };
        if (await dialog.ShowAsync() == ContentDialogResult.Primary
            && !string.IsNullOrWhiteSpace(nameBox.Text))
        {
            await ViewModel.RenameDocumentAsync(doc.Id, nameBox.Text.Trim());
        }
    }

    private static Color ParseColor(string hex)
    {
        hex = hex.TrimStart('#');
        if (hex.Length == 6)
        {
            return Color.FromArgb(255,
                byte.Parse(hex[0..2], System.Globalization.NumberStyles.HexNumber),
                byte.Parse(hex[2..4], System.Globalization.NumberStyles.HexNumber),
                byte.Parse(hex[4..6], System.Globalization.NumberStyles.HexNumber));
        }
        return Colors.Gray;
    }
}
