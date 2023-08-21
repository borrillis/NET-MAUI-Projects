using System.Collections.ObjectModel;
using System.Collections.Specialized;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SticksAndStones.Messages;
using SticksAndStones.Models;
using SticksAndStones.Services;

namespace SticksAndStones.ViewModels;

public partial class LobbyViewModel : ViewModelBase
{
    private readonly LobbyService lobbyService;

    public ObservableCollection<PlayerViewModel> Players { get; init; }

    public LobbyViewModel(LobbyService lobbyService)
    {
        this.lobbyService = lobbyService;
        Players = new(from p in lobbyService.Players
                      where p.Id != lobbyService.CurrentPlayer.Id
                      select new PlayerViewModel(p, lobbyService));
        lobbyService.Players.CollectionChanged += (s, e) =>
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var player in e.NewItems.Cast<Player>())
                {
                    Players.Add(new PlayerViewModel(player, lobbyService));
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var player in e.OldItems.Cast<Player>())
                {
                    var toRemove = Players.FirstOrDefault(p => p.Id == player.Id);
                    Players.Remove(toRemove);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Replace)
            {
            }
            else if (e.Action == NotifyCollectionChangedAction.Reset)
            {

            }
        };
        this.IsActive = true;
        // If the player has an in progress game, take them to it.
        if (lobbyService.CurrentPlayer?.InGame ?? false)
        {
            MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await Shell.Current.GoToAsync($"Game", new Dictionary<string, object>() { { Constants.ArgumentNames.GameId, lobbyService.CurrentPlayer.GameId } });
            });
        }
    }

    protected override void OnActivated()
    {
        Messenger.Register<ChallengeIssued>(this, (r, m) => OnChallengeIssued(m.Value));
        Messenger.Register<ChallengeResponse>(this, (r, m) => OnChallengeResponse(m.Value));
    }

    protected override async Task RefreshInternal()
    {
        await this.lobbyService.RefreshPlayerList();
        return;
    }

    [RelayCommand]
    private void Disconnect()
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {

            bool answer = await Shell.Current.CurrentPage.DisplayAlert("Disconnect", $"This will log you out of the server\nAre you sure?", "Yes", "No");
            if (answer)
            {
                if (await lobbyService.Disconnect())
                {
                    await Shell.Current.GoToAsync("//Connect?Disconnect=true");
                }
                else
                {
                    await Shell.Current.CurrentPage.DisplayAlert("Disconnect", $"Failed to disconnect from the server.", "Ok");
                }
                IsActive = false;
            }
        });
    }

    private void OnChallengeIssued(Player opponent)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            bool answer = await Shell.Current.CurrentPage.DisplayAlert("You have been challenged!", $"{opponent.Name} has challenged you to a game of Sticks And Stones, do you accept?", "Yes", "No");
            await lobbyService.ChallengePlayer(opponent, answer ? ChallengeStatus.Accepted : ChallengeStatus.Declined);
        });
    }

    private void OnChallengeResponse(PlayerChallenge challenge)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            if (challenge.Status == ChallengeStatus.Accepted)
            {
                //await Shell.Current.CurrentPage.DisplayAlert("The game is afoot!", $"Click OK to start your match!", "Ok");
                await Shell.Current.GoToAsync($"Game", new Dictionary<string, object>() { { Constants.ArgumentNames.GameId, challenge.GameId } });
            }
            if (challenge.Status == ChallengeStatus.Declined && challenge.Challenger.Id == lobbyService.CurrentPlayer.Id)
            {
                await Shell.Current.CurrentPage.DisplayAlert("Challenge declined!", $"{challenge.Opponent.Id} has declined your challenge!", "Ok");
            }
        });
    }
}
