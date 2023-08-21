using SticksAndStones.Views;

namespace SticksAndStones;

public partial class AppShell : Shell
{
    public AppShell()
    {
        Routing.RegisterRoute("Game", typeof(GameView));

        InitializeComponent();
    }
}

