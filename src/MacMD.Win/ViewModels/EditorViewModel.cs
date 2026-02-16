using CommunityToolkit.Mvvm.ComponentModel;

namespace MacMD.Win.ViewModels;

public partial class EditorViewModel : ObservableObject
{
    [ObservableProperty]
    private string _markdownText = string.Empty;
}