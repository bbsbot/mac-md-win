using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MacMD.Core.Models;
using MacMD.Core.Services;
using MacMD.Win.ViewModels;

namespace MacMD.Win;

public sealed partial class MainWindow : Window
{
    private bool _isDark = true;
    private readonly MarkdownService _markdownService;
    private readonly DocumentStore _documentStore;
    private readonly EditorViewModel _editorViewModel;
    private readonly ProjectListViewModel _projectListVm;
    private readonly DocumentListViewModel _documentListVm;
    private readonly DispatcherTimer _previewDebounce;
    private readonly DispatcherTimer _saveDebounce;
    private DocumentId? _currentDocId;

    public MainWindow()
    {
        this.InitializeComponent();
        this.Title = "Mac MD";

        // Resolve services
        _markdownService = Resolve<MarkdownService>() ?? new MarkdownService();
        _documentStore = Resolve<DocumentStore>()!;
        var projectStore = Resolve<ProjectStore>()!;

        // Editor
        _editorViewModel = new EditorViewModel();
        EditorView.ViewModel = _editorViewModel;

        // Project list
        _projectListVm = new ProjectListViewModel(projectStore);
        ProjectListView.ViewModel = _projectListVm;
        _projectListVm.ProjectSelected += OnProjectSelected;

        // Document list
        _documentListVm = new DocumentListViewModel(_documentStore);
        DocumentListView.ViewModel = _documentListVm;
        _documentListVm.DocumentSelected += OnDocumentSelected;

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

        // Initial load
        AppWindow.Resize(new Windows.Graphics.SizeInt32(1280, 800));
        _ = LoadInitialDataAsync();
    }

    private async Task LoadInitialDataAsync()
    {
        await _projectListVm.LoadCommand.ExecuteAsync(null);
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
        _editorViewModel.MarkdownText = doc.Content;
        PreviewView.UpdateHtml(_markdownService.ToHtml(doc.Content));
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

    private void OnToggleTheme(object sender, RoutedEventArgs e)
    {
        _isDark = !_isDark;
        if (this.Content is FrameworkElement root)
            root.RequestedTheme = _isDark ? ElementTheme.Dark : ElementTheme.Light;
    }

    private static T? Resolve<T>() where T : class
        => App.Current.Services.GetService(typeof(T)) as T;
}
