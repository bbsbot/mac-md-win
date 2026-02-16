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

    public ProjectListView()
    {
        this.InitializeComponent();
    }

    private void OnProjectSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ViewModel is not null && ProjectsList.SelectedItem is Project p)
            ViewModel.SelectedProject = p;
    }

    private void OnNewProjectClick(object sender, RoutedEventArgs e)
    {
        ViewModel?.CreateProjectCommand.Execute(null);
    }

    private void OnAllDocumentsClick(object sender, RoutedEventArgs e)
    {
        ProjectsList.SelectedItem = null;
        if (ViewModel is not null)
            ViewModel.SelectedProject = null;
    }
}
