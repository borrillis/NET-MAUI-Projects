namespace DoToo.ViewModels;

using DoToo.Repositories;

public class MainViewModel : ViewModel
{
    private readonly ITodoItemRepository repository;

    public MainViewModel(ITodoItemRepository repository)
    {
        this.repository = repository; 
        Task.Run(async () => await LoadDataAsync());
    }

    private async Task LoadDataAsync()
    {

    }
}
