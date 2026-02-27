using MacMD.Core.Models;
using MacMD.Win.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

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
    public event Action? ArchivedClicked;
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

    private void OnArchivedClick(object sender, RoutedEventArgs e)
    {
        ProjectsList.SelectedItem = null;
        TagsList.SelectedItem = null;
        ArchivedClicked?.Invoke();
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

    private void OnProjectRightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        if (ViewModel is null) return;
        var element = e.OriginalSource as FrameworkElement;
        var project = element?.DataContext as Project;
        if (project is null) return;

        ProjectsList.SelectedItem = project;
        ViewModel.SelectedProject = project;

        var flyout = new MenuFlyout();

        var renameItem = new MenuFlyoutItem { Text = "Rename" };
        renameItem.Click += async (_, _) => await ShowRenameProjectDialogAsync(project);
        flyout.Items.Add(renameItem);

        var deleteItem = new MenuFlyoutItem { Text = "Delete" };
        deleteItem.Click += async (_, _) => await ShowDeleteProjectDialogAsync(project);
        flyout.Items.Add(deleteItem);

        flyout.ShowAt(ProjectsList, e.GetPosition(ProjectsList));
    }

    private async Task ShowRenameProjectDialogAsync(Project project)
    {
        if (ViewModel is null) return;
        var nameBox = new TextBox { Text = project.Name };
        nameBox.SelectAll();
        var dialog = new ContentDialog
        {
            Title = "Rename Project",
            Content = nameBox,
            PrimaryButtonText = "Rename",
            CloseButtonText = "Cancel",
            XamlRoot = this.XamlRoot,
            DefaultButton = ContentDialogButton.Primary,
        };
        if (await dialog.ShowAsync() == ContentDialogResult.Primary
            && !string.IsNullOrWhiteSpace(nameBox.Text))
        {
            await ViewModel.RenameProjectAsync(project.Id, nameBox.Text.Trim());
        }
    }

    private async Task ShowDeleteProjectDialogAsync(Project project)
    {
        if (ViewModel is null) return;
        var dialog = new ContentDialog
        {
            Title = "Delete Project",
            Content = $"Delete project \"{project.Name}\"?\nYou can keep documents or delete them too.",
            PrimaryButtonText = "Delete Documents",
            SecondaryButtonText = "Keep Documents",
            CloseButtonText = "Cancel",
            XamlRoot = this.XamlRoot,
        };
        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
            await ViewModel.DeleteProjectAsync(project.Id, deleteDocuments: true);
        else if (result == ContentDialogResult.Secondary)
            await ViewModel.DeleteProjectAsync(project.Id, deleteDocuments: false);
    }

    private void OnTagRightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        if (TagViewModel is null) return;
        var element = e.OriginalSource as FrameworkElement;
        var tag = element?.DataContext as Tag;
        if (tag is null) return;

        TagsList.SelectedItem = tag;

        var flyout = new MenuFlyout();

        var renameItem = new MenuFlyoutItem { Text = "Rename" };
        renameItem.Click += async (_, _) => await ShowRenameTagDialogAsync(tag);
        flyout.Items.Add(renameItem);

        var colorItem = new MenuFlyoutItem { Text = "Change Color" };
        colorItem.Click += async (_, _) => await ShowChangeColorDialogAsync(tag);
        flyout.Items.Add(colorItem);

        flyout.Items.Add(new MenuFlyoutSeparator());

        var deleteItem = new MenuFlyoutItem { Text = "Delete" };
        deleteItem.Click += async (_, _) =>
        {
            var dialog = new ContentDialog
            {
                Title = "Delete Tag",
                Content = $"Delete tag \"{tag.Name}\"? Documents will not be deleted.",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                XamlRoot = this.XamlRoot,
            };
            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
                await TagViewModel.DeleteTagAsync(tag.Id);
        };
        flyout.Items.Add(deleteItem);

        flyout.ShowAt(TagsList, e.GetPosition(TagsList));
    }

    private async Task ShowRenameTagDialogAsync(Tag tag)
    {
        if (TagViewModel is null) return;
        var nameBox = new TextBox { Text = tag.Name };
        nameBox.SelectAll();
        var dialog = new ContentDialog
        {
            Title = "Rename Tag",
            Content = nameBox,
            PrimaryButtonText = "Rename",
            CloseButtonText = "Cancel",
            XamlRoot = this.XamlRoot,
            DefaultButton = ContentDialogButton.Primary,
        };
        if (await dialog.ShowAsync() == ContentDialogResult.Primary
            && !string.IsNullOrWhiteSpace(nameBox.Text))
        {
            await TagViewModel.RenameTagAsync(tag.Id, nameBox.Text.Trim());
        }
    }

    private async Task ShowChangeColorDialogAsync(Tag tag)
    {
        if (TagViewModel is null) return;
        var colors = new[] { "#FF3B30", "#007AFF", "#34C759", "#FF9500", "#AF52DE", "#FF2D55" };
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };

        string selectedColor = tag.Color;

        foreach (var hex in colors)
        {
            var btn = new Button
            {
                Width = 32, Height = 32,
                CornerRadius = new CornerRadius(16),
                Background = new SolidColorBrush(ParseColor(hex)),
                Tag = hex,
            };
            if (hex == tag.Color)
                btn.BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.White);
            btn.Click += (s, _) =>
            {
                selectedColor = (string)((Button)s!).Tag;
                // Highlight selected
                foreach (var child in panel.Children)
                {
                    if (child is Button b)
                        b.BorderBrush = (string)b.Tag == selectedColor
                            ? new SolidColorBrush(Microsoft.UI.Colors.White)
                            : null;
                }
            };
            panel.Children.Add(btn);
        }

        var dialog = new ContentDialog
        {
            Title = "Change Tag Color",
            Content = panel,
            PrimaryButtonText = "Apply",
            CloseButtonText = "Cancel",
            XamlRoot = this.XamlRoot,
            DefaultButton = ContentDialogButton.Primary,
        };
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            await TagViewModel.UpdateTagColorAsync(tag.Id, selectedColor);
    }

    private static Windows.UI.Color ParseColor(string hex)
    {
        hex = hex.TrimStart('#');
        if (hex.Length == 6)
        {
            return Windows.UI.Color.FromArgb(255,
                byte.Parse(hex[0..2], System.Globalization.NumberStyles.HexNumber),
                byte.Parse(hex[2..4], System.Globalization.NumberStyles.HexNumber),
                byte.Parse(hex[4..6], System.Globalization.NumberStyles.HexNumber));
        }
        return Windows.UI.Color.FromArgb(255, 128, 128, 128);
    }
}
