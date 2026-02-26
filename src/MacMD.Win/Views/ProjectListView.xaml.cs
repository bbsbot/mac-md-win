using MacMD.Core.Models;
using MacMD.Win.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MacMD.Win.Views;

public sealed partial class ProjectListView : UserControl
{
    public ProjectListViewModel? ViewModel
    {
        get => _vm;
        set
        {
            _vm = value;
            if (_vm is not null)
                ProjectsList.ItemsSource = _vm.Projects;
        }
    }
    private ProjectListViewModel? _vm;

    public TagListViewModel? TagViewModel
    {
        get => _tagVm;
        set
        {
            _tagVm = value;
            if (_tagVm is not null)
                TagsList.ItemsSource = _tagVm.Tags;
        }
    }
    private TagListViewModel? _tagVm;

    public event Action? AllDocumentsClicked;
    public event Action? FavoritesClicked;
    public event Action? RecentClicked;
    public event Action<TagId>? TagClicked;

    public ProjectListView()
    {
        this.InitializeComponent();
    }

    private void OnProjectSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ViewModel is not null && ProjectsList.SelectedItem is Project p)
        {
            TagsList.SelectedItem = null;
            ViewModel.SelectedProject = p;
        }
    }

    private void OnNewProjectClick(object sender, RoutedEventArgs e)
    {
        ViewModel?.CreateProjectCommand.Execute(null);
    }

    private void OnAllDocumentsClick(object sender, RoutedEventArgs e)
    {
        ProjectsList.SelectedItem = null;
        TagsList.SelectedItem = null;
        if (ViewModel is not null)
            ViewModel.SelectedProject = null;
        AllDocumentsClicked?.Invoke();
    }

    private void OnFavoritesClick(object sender, RoutedEventArgs e)
    {
        ProjectsList.SelectedItem = null;
        TagsList.SelectedItem = null;
        FavoritesClicked?.Invoke();
    }

    private void OnRecentClick(object sender, RoutedEventArgs e)
    {
        ProjectsList.SelectedItem = null;
        TagsList.SelectedItem = null;
        RecentClicked?.Invoke();
    }

    private void OnTagSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TagViewModel is not null && TagsList.SelectedItem is Tag t)
        {
            ProjectsList.SelectedItem = null;
            TagClicked?.Invoke(t.Id);
        }
    }

    private async void OnNewTagClick(object sender, RoutedEventArgs e)
    {
        if (TagViewModel is null) return;

        var nameBox = new TextBox { PlaceholderText = "Tag name" };
        var dialog = new ContentDialog
        {
            Title = "New Tag",
            Content = nameBox,
            PrimaryButtonText = "Create",
            CloseButtonText = "Cancel",
            XamlRoot = this.XamlRoot,
            DefaultButton = ContentDialogButton.Primary,
        };

        if (await dialog.ShowAsync() == ContentDialogResult.Primary
            && !string.IsNullOrWhiteSpace(nameBox.Text))
        {
            // Cycle through some default colors
            var colors = new[] { "#FF3B30", "#007AFF", "#34C759", "#FF9500", "#AF52DE", "#FF2D55" };
            var color = colors[TagViewModel.Tags.Count % colors.Length];
            await TagViewModel.CreateTagAsync(nameBox.Text.Trim(), color);
        }
    }
}
