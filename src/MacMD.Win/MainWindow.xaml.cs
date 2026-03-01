using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using MacMD.Core.Models;
using MacMD.Core.Services;
using MacMD.Win.Services;
using MacMD.Win.ViewModels;
using MacMD.Win.Views;
using Windows.Storage.Pickers;
using SortBy = MacMD.Win.ViewModels.SortBy;

namespace MacMD.Win;

public sealed partial class MainWindow : Window
{
    private readonly MarkdownService _markdownService;
    private readonly DocumentStore _documentStore;
    private readonly ThemeService _themeService;
    private readonly SettingsService _settingsService;
    private readonly EditorViewModel _editorViewModel;
    private readonly ProjectListViewModel _projectListVm;
    private readonly DocumentListViewModel _documentListVm;
    private readonly TagListViewModel _tagListVm;
    private readonly ExportService _exportService;
    private readonly PdfExportService _pdfExportService;
    private readonly DispatcherTimer _previewDebounce;
    private readonly DispatcherTimer _saveDebounce;
    private DocumentId? _currentDocId;
    private string? _currentDocTitle;
    private bool _selectMode;
    private bool _isDragging;
    private double _dragStartX;
    private double _dragStartColWidth;
    private int _dragTargetCol;

    // Settings window — singleton, open non-modal
    private SettingsWindow? _settingsWindow;

    public MainWindow()
    {
        this.InitializeComponent();
        this.Title = "Mac MD";

        // Resolve services
        _markdownService  = Resolve<MarkdownService>() ?? new MarkdownService();
        _documentStore    = Resolve<DocumentStore>()!;
        _exportService    = Resolve<ExportService>()!;
        _pdfExportService = Resolve<PdfExportService>()!;
        _themeService     = Resolve<ThemeService>()!;
        _settingsService  = Resolve<SettingsService>()!;
        var projectStore  = Resolve<ProjectStore>()!;
        var tagStore      = Resolve<TagStore>()!;

        // Editor
        _editorViewModel = new EditorViewModel();
        EditorView.ViewModel = _editorViewModel;

        // Project list
        _projectListVm = new ProjectListViewModel(projectStore);
        ProjectListView.ViewModel = _projectListVm;
        _projectListVm.ProjectSelected += OnProjectSelected;

        // Document list
        _documentListVm = new DocumentListViewModel(_documentStore, projectStore, tagStore);
        DocumentListView.ViewModel = _documentListVm;
        _documentListVm.DocumentSelected += OnDocumentSelected;

        // Tag list
        _tagListVm = new TagListViewModel(tagStore);
        ProjectListView.TagViewModel = _tagListVm;
        ProjectListView.AllDocumentsClicked += () => _ = _documentListVm.LoadForProjectAsync(null);
        ProjectListView.FavoritesClicked    += () => _ = _documentListVm.LoadFavoritesAsync();
        ProjectListView.RecentClicked       += () => _ = _documentListVm.LoadRecentAsync();
        ProjectListView.TagClicked          += tagId => _ = _documentListVm.LoadForTagAsync(tagId);
        ProjectListView.ArchivedClicked     += () => _ = _documentListVm.LoadArchivedAsync();

        // Preview debounce (300 ms)
        _previewDebounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _previewDebounce.Tick += OnPreviewDebounceTick;

        // Auto-save debounce (2 s)
        _saveDebounce = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _saveDebounce.Tick += OnSaveDebounceTick;

        // React to text changes
        _editorViewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(EditorViewModel.MarkdownText))
            {
                _previewDebounce.Stop();
                _previewDebounce.Start();
                _saveDebounce.Stop();
                _saveDebounce.Start();
            }
        };

        // Subscribe to settings changes → push font updates to views
        _settingsService.SettingsChanged += OnSettingsChanged;

        // Initial load
        AppWindow.Resize(new Windows.Graphics.SizeInt32(1280, 800));
        _ = LoadInitialDataAsync();

        // Apply theme + fonts after the visual tree is ready
        this.Activated += OnWindowActivated;
    }

    private void OnWindowActivated(object sender, WindowActivatedEventArgs e)
    {
        this.Activated -= OnWindowActivated; // only once
        ApplyTheme(_themeService.CurrentTheme);
        ApplyFontSettings();
    }

    private async Task LoadInitialDataAsync()
    {
        // Restore persisted sort preference before first load
        var savedSort = _settingsService.DocumentSort;
        _documentListVm.CurrentSortBy = SortKeyToEnum(savedSort);
        UpdateSortCheckmarks(savedSort);

        // Restore preview layout (manipulate grid before content renders)
        SetPreviewLayout(_settingsService.PreviewLayout, persist: false);

        await _projectListVm.LoadCommand.ExecuteAsync(null);
        await _tagListVm.LoadCommand.ExecuteAsync(null);
        await _documentListVm.LoadForProjectAsync(null); // all docs
    }

    private async void OnProjectSelected(ProjectId? projectId)
    {
        await _documentListVm.LoadForProjectAsync(projectId);
    }

    private async void OnDocumentSelected(DocumentId docId)
    {
        // Save current doc first
        await SaveCurrentAsync();

        var doc = await _documentStore.GetByIdAsync(docId);
        if (doc is null) return;

        _currentDocId    = docId;
        _currentDocTitle = doc.Title;
        _editorViewModel.MarkdownText = doc.Content;
        PreviewView.UpdateHtml(_markdownService.ToHtml(doc.Content));
        ExportHtmlItem.IsEnabled = true;
        ExportPdfItem.IsEnabled  = true;
    }

    private void OnPreviewDebounceTick(object? sender, object e)
    {
        _previewDebounce.Stop();
        PreviewView.UpdateHtml(_markdownService.ToHtml(_editorViewModel.MarkdownText));
    }

    private async void OnSaveDebounceTick(object? sender, object e)
    {
        _saveDebounce.Stop();
        await SaveCurrentAsync();
    }

    private async Task SaveCurrentAsync()
    {
        if (_currentDocId is { } id)
            await _documentStore.UpdateContentAsync(id, _editorViewModel.MarkdownText);
    }

    // ── Settings window ───────────────────────────────────────────────────

    private void OnSettingsClick(object sender, RoutedEventArgs e)
    {
        if (_settingsWindow is not null)
        {
            _settingsWindow.Activate();
            return;
        }

        var vm = Resolve<SettingsViewModel>()!;
        _settingsWindow = new SettingsWindow(vm, _themeService);
        _settingsWindow.ThemeChanged += OnThemeSelected;
        _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        _settingsWindow.Activate();
    }

    // ── Settings changed handler ──────────────────────────────────────────

    private void OnSettingsChanged()
    {
        DispatcherQueue.TryEnqueue(ApplyFontSettings);
    }

    private void ApplyFontSettings()
    {
        var familyKey = _settingsService.EditorFontFamily;
        var editorSize = _settingsService.EditorFontSize;
        var previewSize = _settingsService.PreviewFontSize;

        EditorView.ApplyFontSettings(familyKey, editorSize);
        PreviewView.ApplyFontSize(previewSize);

        if (_currentDocId is not null)
            PreviewView.UpdateHtml(_markdownService.ToHtml(_editorViewModel.MarkdownText));
    }

    // ── Theme ─────────────────────────────────────────────────────────────

    private void OnThemeSelected(ColorTheme theme)
    {
        ApplyTheme(theme);
    }

    private void ApplyTheme(ColorTheme theme)
    {
        if (this.Content is FrameworkElement root)
            root.RequestedTheme = theme.IsDark ? ElementTheme.Dark : ElementTheme.Light;

        RootGrid.Background = new SolidColorBrush(ParseHexColor(theme.Background));
        EditorView.ApplyTheme(theme);
        PreviewView.ApplyTheme(theme);

        if (_currentDocId is not null)
            PreviewView.UpdateHtml(_markdownService.ToHtml(_editorViewModel.MarkdownText));
    }

    private static Windows.UI.Color ParseHexColor(string hex)
    {
        hex = hex.TrimStart('#');
        if (hex.Length == 6)
        {
            return Windows.UI.Color.FromArgb(255,
                byte.Parse(hex[0..2], System.Globalization.NumberStyles.HexNumber),
                byte.Parse(hex[2..4], System.Globalization.NumberStyles.HexNumber),
                byte.Parse(hex[4..6], System.Globalization.NumberStyles.HexNumber));
        }
        return Windows.UI.Color.FromArgb(255, 30, 30, 30);
    }

    // ── Export ────────────────────────────────────────────────────────────

    private async void OnExportHtml(object sender, RoutedEventArgs e)
    {
        var picker = new FileSavePicker();
        picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        picker.FileTypeChoices.Add("HTML Document", new[] { ".html" });
        picker.SuggestedFileName = _currentDocTitle ?? "document";

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        var file = await picker.PickSaveFileAsync();
        if (file is null) return;

        await _exportService.ExportHtmlAsync(
            _currentDocTitle ?? "Untitled",
            _editorViewModel.MarkdownText,
            file.Path);
    }

    private async void OnExportPdf(object sender, RoutedEventArgs e)
    {
        var picker = new FileSavePicker();
        picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        picker.FileTypeChoices.Add("PDF Document", new[] { ".pdf" });
        picker.SuggestedFileName = _currentDocTitle ?? "document";

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        var file = await picker.PickSaveFileAsync();
        if (file is null) return;

        await _pdfExportService.ExportPdfAsync(
            _currentDocTitle ?? "Untitled",
            _editorViewModel.MarkdownText,
            file.Path,
            PreviewView.WebView);
    }

    // ── Preview Layout ────────────────────────────────────────────────────

    private void OnLayoutSelected(object sender, RoutedEventArgs e)
    {
        if (sender is not ToggleMenuFlyoutItem item) return;
        var layout = item.Tag as string ?? "right";
        SetPreviewLayout(layout, persist: true);
    }

    private void SetPreviewLayout(string layout, bool persist)
    {
        var grid = EditorPreviewGrid;
        var splitter = EditorPreviewSplitter;

        grid.ColumnDefinitions.Clear();
        grid.RowDefinitions.Clear();

        // Reset all positions to (row=0, col=0) first
        Grid.SetRow(EditorView, 0);    Grid.SetColumn(EditorView, 0);
        Grid.SetRow(splitter, 0);      Grid.SetColumn(splitter, 0);
        Grid.SetRow(PreviewView, 0);   Grid.SetColumn(PreviewView, 0);

        switch (layout)
        {
            case "left":
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                Grid.SetColumn(PreviewView, 0);
                Grid.SetColumn(splitter, 1);
                Grid.SetColumn(EditorView, 2);
                splitter.Width = 1; splitter.Height = double.NaN;
                break;
            case "below":
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1) });
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                Grid.SetRow(EditorView, 0);
                Grid.SetRow(splitter, 1);
                Grid.SetRow(PreviewView, 2);
                splitter.Width = double.NaN; splitter.Height = 1;
                break;
            default: // "right"
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                Grid.SetColumn(EditorView, 0);
                Grid.SetColumn(splitter, 1);
                Grid.SetColumn(PreviewView, 2);
                splitter.Width = 1; splitter.Height = double.NaN;
                break;
        }

        if (persist)
            _settingsService.PreviewLayout = layout;

        UpdateLayoutCheckmarks(layout);
    }

    private void UpdateLayoutCheckmarks(string layout)
    {
        LayoutRight.IsChecked = layout == "right";
        LayoutLeft.IsChecked  = layout == "left";
        LayoutBelow.IsChecked = layout == "below";
    }

    // ── Multi-Select ──────────────────────────────────────────────────────

    private void OnSelectModeToggle(object sender, RoutedEventArgs e)
    {
        _selectMode = !_selectMode;
        DocumentListView.SetSelectMode(_selectMode);
        SelectToggleButton.Content = _selectMode ? "Done" : "Select";
        BulkActionButton.Visibility = _selectMode ? Visibility.Visible : Visibility.Collapsed;
        SelectToggleButton.Style = _selectMode
            ? (Style)Application.Current.Resources["AccentButtonStyle"]
            : null;
    }

    private async void OnBulkActionClick(object sender, RoutedEventArgs e)
    {
        await _documentListVm.RefreshContextMenuDataAsync();
        var count = _documentListVm.SelectedDocumentIds.Count;
        var commonTagValues = await _documentListVm.GetCommonTagValuesAsync();
        var flyout = new MenuFlyout();

        // Delete
        var deleteItem = new MenuFlyoutItem
        {
            Text = $"Delete {count} Document{(count != 1 ? "s" : "")}"
        };
        deleteItem.Click += async (_, _) =>
        {
            await _documentListVm.DeleteSelectedAsync();
            OnSelectModeToggle(null!, null!);
        };
        flyout.Items.Add(deleteItem);

        flyout.Items.Add(new MenuFlyoutSeparator());

        // Move to Project
        var moveSub = new MenuFlyoutSubItem { Text = "Move to Project" };
        var noneItem = new MenuFlyoutItem { Text = "None" };
        noneItem.Click += async (_, _) => await _documentListVm.MoveSelectedAsync(null);
        moveSub.Items.Add(noneItem);
        foreach (var project in _documentListVm.AvailableProjects)
        {
            var pid = project.Id;
            var pItem = new MenuFlyoutItem { Text = project.Name };
            pItem.Click += async (_, _) => await _documentListVm.MoveSelectedAsync(pid);
            moveSub.Items.Add(pItem);
        }
        flyout.Items.Add(moveSub);

        // Tags — checkmarks show which tags ALL selected docs share; clicking toggles
        var tagSub = new MenuFlyoutSubItem { Text = "Tags" };
        foreach (var tag in _documentListVm.AvailableTags)
        {
            var tid = tag.Id;
            bool allHave = commonTagValues.Contains(tag.Id.Value);
            var tItem = new MenuFlyoutItem { Text = (allHave ? "\u2713 " : "    ") + tag.Name };
            try
            {
                tItem.Icon = new FontIcon
                {
                    Glyph = "\u25CF",
                    Foreground = new SolidColorBrush(ParseHexColor(tag.Color)),
                    FontSize = 12
                };
            }
            catch { }
            tItem.Click += async (_, _) => await _documentListVm.ToggleTagSelectedAsync(tid, allHave);
            tagSub.Items.Add(tItem);
        }
        flyout.Items.Add(tagSub);

        flyout.Items.Add(new MenuFlyoutSeparator());

        // Favorites
        var favItem = new MenuFlyoutItem { Text = "Add to Favorites" };
        favItem.Click += async (_, _) => await _documentListVm.AddToFavoritesSelectedAsync();
        flyout.Items.Add(favItem);

        var unfavItem = new MenuFlyoutItem { Text = "Remove from Favorites" };
        unfavItem.Click += async (_, _) => await _documentListVm.RemoveFromFavoritesSelectedAsync();
        flyout.Items.Add(unfavItem);

        // Archive
        var archiveItem = new MenuFlyoutItem { Text = "Archive" };
        archiveItem.Click += async (_, _) =>
        {
            await _documentListVm.ArchiveSelectedAsync();
            OnSelectModeToggle(null!, null!);
        };
        flyout.Items.Add(archiveItem);

        flyout.ShowAt(BulkActionButton);
    }

    // ── Sort ──────────────────────────────────────────────────────────────

    private void OnSortSelected(object sender, RoutedEventArgs e)
    {
        if (sender is not ToggleMenuFlyoutItem item) return;
        var tag = item.Tag as string ?? "dateModified";
        UpdateSortCheckmarks(tag);
        _settingsService.DocumentSort = tag;
        _ = _documentListVm.SetSortAsync(SortKeyToEnum(tag));
    }

    private void UpdateSortCheckmarks(string tag)
    {
        SortByDateModified.IsChecked = tag == "dateModified";
        SortByDateCreated.IsChecked  = tag == "dateCreated";
        SortByTitle.IsChecked        = tag == "title";
        SortByWordCount.IsChecked    = tag == "wordCount";
    }

    private static SortBy SortKeyToEnum(string key) => key switch
    {
        "dateCreated" => SortBy.DateCreated,
        "title"       => SortBy.Title,
        "wordCount"   => SortBy.WordCount,
        _             => SortBy.DateModified,
    };

    // ── Resizable Splitters ───────────────────────────────────────────────

    private void OnSplitterPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not FrameworkElement elem) return;
        _dragTargetCol = int.Parse(elem.Tag as string ?? "0");
        _dragStartX = e.GetCurrentPoint(RootGrid).Position.X;
        _dragStartColWidth = RootGrid.ColumnDefinitions[_dragTargetCol].ActualWidth;
        _isDragging = true;
        elem.CapturePointer(e.Pointer);
        e.Handled = true;
    }

    private void OnSplitterPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_isDragging) return;
        var delta = e.GetCurrentPoint(RootGrid).Position.X - _dragStartX;
        var (minW, maxW) = _dragTargetCol == 0 ? (150.0, 400.0) : (180.0, 800.0);
        var newWidth = Math.Clamp(_dragStartColWidth + delta, minW, maxW);
        RootGrid.ColumnDefinitions[_dragTargetCol].Width = new GridLength(newWidth);
        e.Handled = true;
    }

    private void OnSplitterPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (!_isDragging) return;
        if (sender is UIElement elem)
            elem.ReleasePointerCapture(e.Pointer);
        _isDragging = false;
        e.Handled = true;
    }

    private static T? Resolve<T>() where T : class
        => App.Current.Services.GetService(typeof(T)) as T;
}
