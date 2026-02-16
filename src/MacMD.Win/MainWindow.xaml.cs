using Microsoft.UI.Xaml;
using MacMD.Core.Services;
using MacMD.Win.ViewModels;

namespace MacMD.Win;

public sealed partial class MainWindow : Window
{
    private bool _isDark = true;
    private EditorViewModel _editorViewModel;

    public MainWindow()
    {
        this.InitializeComponent();
        this.Title = "Mac MD";

        // Set up the editor ViewModel
        var markdownService = App.Current.Services.GetService(typeof(MarkdownService)) as MarkdownService;
        if (markdownService == null)
        {
            // Fallback in case service resolution fails
            markdownService = new MarkdownService();
        }
        _editorViewModel = new EditorViewModel(markdownService);
        EditorView.ViewModel = _editorViewModel;

        // Set initial size
        var appWindow = this.AppWindow;
        appWindow.Resize(new Windows.Graphics.SizeInt32(1280, 800));
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
