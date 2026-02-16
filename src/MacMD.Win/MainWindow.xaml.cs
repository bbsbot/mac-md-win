using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MacMD.Core.Services;
using MacMD.Win.ViewModels;

namespace MacMD.Win;

public sealed partial class MainWindow : Window
{
    private bool _isDark = true;
    private readonly MarkdownService _markdownService;
    private readonly EditorViewModel _editorViewModel;
    private readonly DispatcherTimer _debounceTimer;

    public MainWindow()
    {
        this.InitializeComponent();
        this.Title = "Mac MD";

        // Resolve services
        _markdownService = (App.Current.Services.GetService(typeof(MarkdownService)) as MarkdownService)
            ?? new MarkdownService();

        // Wire editor ViewModel
        _editorViewModel = new EditorViewModel();
        EditorView.ViewModel = _editorViewModel;

        // Debounce timer for live preview (300 ms)
        _debounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _debounceTimer.Tick += OnDebounceTick;

        // Listen for text changes in the editor
        _editorViewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(EditorViewModel.MarkdownText))
            {
                _debounceTimer.Stop();
                _debounceTimer.Start();
            }
        };

        // Set initial size
        AppWindow.Resize(new Windows.Graphics.SizeInt32(1280, 800));
    }

    private void OnDebounceTick(object? sender, object e)
    {
        _debounceTimer.Stop();
        var html = _markdownService.ToHtml(_editorViewModel.MarkdownText);
        PreviewView.UpdateHtml(html);
    }

    private void OnToggleTheme(object sender, RoutedEventArgs e)
    {
        _isDark = !_isDark;

        if (this.Content is FrameworkElement root)
        {
            root.RequestedTheme = _isDark
                ? ElementTheme.Dark
                : ElementTheme.Light;
        }
    }
}
