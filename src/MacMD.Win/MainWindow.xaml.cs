using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MacMD.Core.Models;
using MacMD.Core.Services;
using MacMD.Win.Services;
using MacMD.Win.ViewModels;
using Windows.Storage.Pickers;

namespace MacMD.Win;

public sealed partial class MainWindow : Window
{
    private readonly MarkdownService _markdownService;
    private readonly DocumentStore _documentStore;
    private readonly ThemeService _themeService;
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

    public MainWindow()
    {
        this.InitializeComponent();
        this.Title = "Mac MD";

        // Resolve services
        _markdownService = Resolve<MarkdownService>() ?? new MarkdownService();
        _documentStore = Resolve<DocumentStore>()!;
        _exportService = Resolve<ExportService>()!;
        _pdfExportService = Resolve<PdfExportService>()!;
        _themeService = Resolve<ThemeService>()!;
        var projectStore = Resolve<ProjectStore>()!;
        var tagStore = Resolve<TagStore>()!;

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
        ProjectListView.FavoritesClicked += () => _ = _documentListVm.LoadFavoritesAsync();
        ProjectListView.RecentClicked += () => _ = _documentListVm.LoadRecentAsync();
        ProjectListView.TagClicked += tagId => _ = _documentListVm.LoadForTagAsync(tagId);

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

        // Theme picker
        ThemePicker.Initialize(_themeService);
        ThemePicker.ThemeSelected += OnThemeSelected;

        // Initial load
        AppWindow.Resize(new Windows.Graphics.SizeInt32(1280, 800));
        _ = LoadInitialDataAsync();

        // Apply theme after the visual tree is ready
        this.Activated += OnWindowActivated;
    }

    private void OnWindowActivated(object sender, WindowActivatedEventArgs e)
    {
        this.Activated -= OnWindowActivated; // only once
        ApplyTheme(_themeService.CurrentTheme);
    }

    private async Task LoadInitialDataAsync()
    {
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

        _currentDocId = docId;
        _currentDocTitle = doc.Title;
        _editorViewModel.MarkdownText = doc.Content;
        PreviewView.UpdateHtml(_markdownService.ToHtml(doc.Content));
        ExportHtmlItem.IsEnabled = true;
        ExportPdfItem.IsEnabled = true;
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

    private void OnThemeSelected(MacMD.Core.Models.ColorTheme theme)
    {
        ApplyTheme(theme);
    }

    private void ApplyTheme(MacMD.Core.Models.ColorTheme theme)
    {
        if (this.Content is FrameworkElement root)
            root.RequestedTheme = theme.IsDark ? ElementTheme.Dark : ElementTheme.Light;

        // Update preview CSS to match the selected theme
        if (_currentDocId is not null)
            PreviewView.UpdateHtml(_markdownService.ToHtml(_editorViewModel.MarkdownText));
    }

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

    private static T? Resolve<T>() where T : class
        => App.Current.Services.GetService(typeof(T)) as T;
}
