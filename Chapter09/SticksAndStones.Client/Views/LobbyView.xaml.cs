using SticksAndStones.ViewModels;

namespace SticksAndStones.Views;

public partial class LobbyView : ContentPage
{
    private LobbyViewModel ViewModel => (LobbyViewModel)BindingContext;

    public LobbyView(LobbyViewModel viewModel)
    {
        this.BindingContext = viewModel;
        InitializeComponent();
    }
}
