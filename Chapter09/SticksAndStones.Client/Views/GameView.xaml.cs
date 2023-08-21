using SticksAndStones.ViewModels;

namespace SticksAndStones.Views;

public partial class GameView : ContentPage
{
    private GameViewModel ViewModel => (GameViewModel)BindingContext;

    public GameView(GameViewModel viewModel)
    {
        this.BindingContext = viewModel;

        InitializeComponent();
    }
}
