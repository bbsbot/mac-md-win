using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MacMD.Core.Services;
using Microsoft.UI.Xaml.Controls;

namespace MacMD.Win.ViewModels;

/// <summary>
/// ViewModel for the Markdown editor view.
/// </summary>
public partial class EditorViewModel : ObservableObject
{
    private readonly MarkdownService _markdownService;

    public EditorViewModel(MarkdownService markdownService)
    {
        _markdownService = markdownService ?? throw new ArgumentNullException(nameof(markdownService));
    }

    [ObservableProperty]
    private string _markdownText = string.Empty;

    [ObservableProperty]
    private string _htmlPreview = string.Empty;

    [RelayCommand]
    private void UpdatePreview()
    {
        HtmlPreview = _markdownService.ToHtml(MarkdownText);
    }
}