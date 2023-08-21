
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SticksAndStones.Models;
using SticksAndStones.Services;

namespace SticksAndStones.ViewModels;


public partial class PlayerViewModel : ObservableObject
{
    public PlayerViewModel(Player player, LobbyService lobbyService)
    {
        playerModel = player;
        this.lobbyService = lobbyService;
    }

    private readonly LobbyService lobbyService;

    public Player Player => playerModel;
    private readonly Player playerModel;

    public string Id
    {
        get => playerModel.Id;
        set => SetProperty(playerModel.Id, value, playerModel, (p, n) => p.Id = n);
    }

    public string Name
    {
        get => playerModel.Name;
        set => SetProperty(playerModel.Name, value, playerModel, (p, n) => p.Name = n);
    }
    public string EmailAddress
    {
        get => playerModel.EmailAddress;
        set => SetProperty(playerModel.EmailAddress, value, playerModel, (p, n) => p.EmailAddress = n);
    }

    public string AvatarUrl
    {
        get => playerModel.AvatarUrl;
        set => SetProperty(playerModel.AvatarUrl, value, playerModel, (p, n) => p.AvatarUrl = n);
    }

    public bool CanChallenge => !playerModel.InGame;

    public string Status => playerModel.InGame switch
    {
        true => "In a game",
        false => "Waiting for opponent"
    };

    public bool IsChallenging { get; set; } = false;

    [RelayCommand(CanExecute = nameof(CanChallenge))]
    public void Challenge(PlayerViewModel opponent)
    {
        IsChallenging = true;
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            bool answer = await Shell.Current.CurrentPage.DisplayAlert("Issue Challenge!", $"You are about to join a game with {opponent.Name}\nAre you sure?", "Yes", "No");
            if (answer)
            {
                await lobbyService.IssueChallenge(opponent.Player);
            }
            IsChallenging = false;
        });
        return;
    }
}
