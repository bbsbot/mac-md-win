using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MacMD.Core.Models;
using MacMD.Core.Services;

namespace MacMD.Win.ViewModels;

public partial class ProjectListViewModel : ObservableObject
{
    private readonly ProjectStore _projectStore;

    public ProjectListViewModel(ProjectStore projectStore)
    {
        _projectStore = projectStore;
    }

    public ObservableCollection<Project> Projects { get; } = new();

    [ObservableProperty]
    private Project? _selectedProject;

    /// <summary>Raised when the selected project changes so the document list can reload.</summary>
    public event Action<ProjectId?>? ProjectSelected;

    partial void OnSelectedProjectChanged(Project? value)
    {
        ProjectSelected?.Invoke(value?.Id);
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        var projects = await _projectStore.GetAllAsync();
        Projects.Clear();
        foreach (var p in projects)
            Projects.Add(p);
    }

    [RelayCommand]
    private async Task CreateProjectAsync()
    {
        var id = await _projectStore.CreateAsync("New Project");
        await LoadAsync();
        SelectedProject = Projects.FirstOrDefault(p => p.Id.Value == id.Value);
    }

    public async Task RenameProjectAsync(ProjectId id, string newName)
    {
        await _projectStore.RenameAsync(id, newName);
        await LoadAsync();
    }

    public async Task DeleteProjectAsync(ProjectId id, bool deleteDocuments)
    {
        if (!deleteDocuments)
        {
            // Unlink documents from this project before deleting
            await _projectStore.UnlinkDocumentsAsync(id);
        }
        await _projectStore.DeleteAsync(id);
        if (SelectedProject?.Id.Value == id.Value)
            SelectedProject = null;
        await LoadAsync();
    }
}
