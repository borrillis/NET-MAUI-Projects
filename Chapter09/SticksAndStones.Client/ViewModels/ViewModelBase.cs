using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SticksAndStones.ViewModels;

public abstract partial class ViewModelBase : ObservableRecipient
{
    protected abstract Task RefreshInternal();

    [ObservableProperty]
    private bool isRefreshing;

    [RelayCommand]
    public async Task Refresh()
    {
        IsRefreshing = true;
        await RefreshInternal();
        IsRefreshing = false;
        return;
    }
}
