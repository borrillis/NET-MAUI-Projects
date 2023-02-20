﻿namespace DoToo.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DoToo.Models;
using DoToo.Repositories;
using DoToo.Views;
using System.Collections.ObjectModel;

public partial class MainViewModel : ViewModel
{
    private readonly ITodoItemRepository repository;
    private readonly IServiceProvider services;

    [ObservableProperty]
    ObservableCollection<TodoItemViewModel> items;

    public MainViewModel(ITodoItemRepository repository, IServiceProvider services)
    {
        this.repository = repository;
        repository.OnItemAdded += (sender, item) => Items.Add(CreateTodoItemViewModel(item));
        repository.OnItemUpdated += (sender, item) => Task.Run(async () => await LoadDataAsync());

        this.services = services;
        Task.Run(async () => await LoadDataAsync());
    }

    private async Task LoadDataAsync()
    {
        var items = await repository.GetItemsAsync(); 
        var itemViewModels = items.Select(i => CreateTodoItemViewModel(i));
        Items = new ObservableCollection<TodoItemViewModel>(itemViewModels);
    }

    private TodoItemViewModel CreateTodoItemViewModel(TodoItem item)
    {
        var itemViewModel = new TodoItemViewModel(item);
        itemViewModel.ItemStatusChanged += ItemStatusChanged; 
        return itemViewModel;
    }

    private void ItemStatusChanged(object sender, EventArgs e)
    {
    }

    [RelayCommand]
    public async Task AddItemAsync() => await Navigation.PushAsync(services.GetRequiredService<ItemView>());

}
