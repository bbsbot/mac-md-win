using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MacMD.Core.Models;
using MacMD.Core.Services;

namespace MacMD.Win.ViewModels;

public partial class TagListViewModel : ObservableObject
{
    private readonly TagStore _tagStore;

    public TagListViewModel(TagStore tagStore)
    {
        _tagStore = tagStore;
    }

    public ObservableCollection<Tag> Tags { get; } = new();

    [ObservableProperty]
    private Tag? _selectedTag;

    public event Action<TagId>? TagSelected;

    partial void OnSelectedTagChanged(Tag? value)
    {
        if (value is not null)
            TagSelected?.Invoke(value.Id);
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        var tags = await _tagStore.GetAllAsync();
        Tags.Clear();
        foreach (var t in tags)
            Tags.Add(t);
    }

    public async Task CreateTagAsync(string name, string color)
    {
        await _tagStore.CreateAsync(name, color);
        await LoadAsync();
    }

    public async Task DeleteTagAsync(TagId id)
    {
        await _tagStore.DeleteAsync(id);
        Tags.Remove(Tags.FirstOrDefault(t => t.Id.Value == id.Value)!);
        if (SelectedTag?.Id.Value == id.Value)
            SelectedTag = null;
    }

    public async Task RenameTagAsync(TagId id, string newName)
    {
        await _tagStore.RenameAsync(id, newName);
        await LoadAsync();
    }

    public async Task UpdateTagColorAsync(TagId id, string color)
    {
        await _tagStore.UpdateColorAsync(id, color);
        await LoadAsync();
    }
}
