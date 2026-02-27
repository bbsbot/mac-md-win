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

    private void OnDocumentSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ViewModel is not null && DocumentsList.SelectedItem is DocumentSummary d)
            ViewModel.SelectedDocument = d;
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
