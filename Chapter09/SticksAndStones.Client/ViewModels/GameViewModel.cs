using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SticksAndStones.Models;
using SticksAndStones.Services;

namespace SticksAndStones.ViewModels;

public partial class GameViewModel : ViewModelBase, IQueryAttributable
{

    private readonly GameService gameService;
    private PlayerId? currentPlayer;
    private int lastSelectedStick = -1;

    [ObservableProperty]
    private Game? game;

    [ObservableProperty]
    private string? title;

    public GameViewModel(GameService gameService)
    {
        this.gameService = gameService;
    }

    [RelayCommand]
    private async Task Play()
    {
        if (lastSelectedStick == -1)
        {
            await Shell.Current.CurrentPage.DisplayAlert("Make a move", "You must make a move before you play.", "ok");
            return;
        }
    }

    [RelayCommand]
    private Task SelectStick(string arg)
    {
        if (currentPlayer == PlayerId.None) return Task.CompletedTask;

        int pos = -1;
        int.TryParse(arg, out pos);
        if (pos != -1)
        {
            if (lastSelectedStick != -1 && lastSelectedStick != pos)
                Game.Sticks[pos] = (int)PlayerId.None;

            Game.Sticks[pos] = (int)currentPlayer;
            lastSelectedStick = pos;
        }
        return Task.CompletedTask;
    }

    protected override Task RefreshInternal()
    {
        return Task.CompletedTask;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        MainThread.InvokeOnMainThreadAsync(async () =>
        {
            var gameId = query[Constants.ArgumentNames.GameId] as string;
            if (gameId is not null)
            {
                Game = await gameService.GetGameByIdAsync(gameId);
                Title = $"{Game?.PlayerOne.Name ?? "Player One"} VS {Game?.PlayerTwo.Name ?? "Player Two"}";
            }
        });
    }
}

