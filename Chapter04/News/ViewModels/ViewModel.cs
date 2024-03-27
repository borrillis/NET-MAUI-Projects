namespace News.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;

public abstract partial class ViewModel : ObservableObject
{
    public INavigate Navigation { get; init; }

    internal ViewModel(INavigate navigation) => Navigation = navigation;
}
