using System.Net.Http.Json;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.AspNetCore.SignalR.Client;
using SticksAndStones.Messages;
using SticksAndStones.Models;

namespace SticksAndStones.Services;

public partial class LobbyService
{
    private readonly SemaphoreSlim semaphoreSlim = new(1, 1);

    private ServiceConnection service { get; init; }
    private Settings settings { get; init; }

    public Player? CurrentPlayer { get; protected set; }

    public bool IsConnected { get; protected set; }

    public System.Collections.ObjectModel.ObservableCollection<Player> Players { get; } = new();

    public LobbyService(Settings settings, ServiceConnection service)
    {
        this.service = service;
        this.settings = settings;
    }

    public async Task<Player> Connect(Player player)
    {
        await semaphoreSlim.WaitAsync();

        this.CurrentPlayer = player;

        service.Hub.Value.Result?.On<PlayerStatusChanged>(Constants.PlayerStatusChanged, PlayerStatusChangedHandler);
        service.Hub.Value.Result?.On<PlayerChallenge>(Constants.Challenge, PlayerChallengeHandler);
        service.Hub.Value.Result.SendAsync()

        (var response, var error) = await service.PostAsync<ConnectResult>(new($"{settings.ServerUrl}/Lobby/Connect"), player);
        if (error is null)
        {
            response.Players.ForEach(Players.Add);
            CurrentPlayer = response.Player;
            IsConnected = true;
        }
        else
        {
            IsConnected = false;
        }

        semaphoreSlim.Release();

        return CurrentPlayer;
    }

    public async Task<string> VerifyImage(string playerId, string base64Image, string imageFormat)
    {
        //         var response = await httpClient.PostAsJsonAsync($"{httpClient.BaseAddress}/Lobby/CheckPhoto", new PhotoMessage(playerId) { Base64Photo = base64Image, ImageFormat = imageFormat });
        (var response, var error) = await service.PostAsync<PhotoUrlMessage>(new($"{settings.ServerUrl}/Lobby/CheckPhoto"), new PhotoMessage(playerId) { Base64Photo = base64Image, ImageFormat = imageFormat });
        //         if (response.IsSuccessStatusCode)
        if (error is null)
        {
            PhotoUrlMessage? photoUrlMsg = response as PhotoUrlMessage;
            if (photoUrlMsg is not null)
            {
#if DEBUG && ANDROID // For Android use the redirection Addresses to point to "localhost"
                photoUrlMsg.Url = photoUrlMsg.Url.Replace("127.0.0.1", "10.0.2.2");
#endif
            }
            return photoUrlMsg?.Url ?? string.Empty;
        }
        return string.Empty;
    }

    public async Task<bool> Disconnect()
    {
        // var response = await httpClient.PostAsJsonAsync($"{httpClient.BaseAddress}/Lobby/Disconnect", CurrentPlayer);
        // Players.Clear();
        // return response.IsSuccessStatusCode;
        return true;
    }

    public async Task RefreshPlayerList()
    {
        // var response = await httpClient.PostAsJsonAsync($"{httpClient.BaseAddress}/Lobby/GetPlayers", CurrentPlayer);
        // if (response.IsSuccessStatusCode)
        // {
        //     var playerList = await response.Content.ReadFromJsonAsync<List<Player>>();
        //     Players.Clear();
        //     playerList.ForEach(Players.Add);
        // }
        return;
    }

    public async Task IssueChallenge(Player opponent)
    {
        // var response = await httpClient.PostAsJsonAsync($"{httpClient.BaseAddress}/Lobby/Challenge", new PlayerChallenge(CurrentPlayer, opponent, ChallengeStatus.Issued));
        // if (response.IsSuccessStatusCode)
        // {
        // }
        return;
    }

    public async Task ChallengePlayer(Player challenger, ChallengeStatus status)
    {
        // var response = await httpClient.PostAsJsonAsync($"{httpClient.BaseAddress}/Lobby/Challenge", new PlayerChallenge(challenger, CurrentPlayer, status));
        // if (response.IsSuccessStatusCode)
        // {
        // }
        return;
    }

    private void PlayerStatusChangedHandler(PlayerStatusChanged playerStatusChanged)
    {
        var changedPlayer = (from player in Players
                             where player.Id == playerStatusChanged.Player.Id
                             select player).FirstOrDefault();
        if (playerStatusChanged.Connected)
        {
            if (changedPlayer is not null)
            {
                changedPlayer.GameId = changedPlayer.GameId;
            }
            else
            {
                Players.Add(playerStatusChanged.Player);
            }
        }
        else
        {
            Players.Remove(changedPlayer);
        }
    }

    private void PlayerChallengeHandler(PlayerChallenge challenge)
    {
        if (challenge.Challenger.Id == CurrentPlayer.Id)
        {
            switch (challenge.Status)
            {
                case ChallengeStatus.Accepted:
                case ChallengeStatus.Declined:
                    WeakReferenceMessenger.Default.Send(new ChallengeResponse(challenge));
                    break;
                default:
                    return;
            }
        }
        if (challenge.Opponent.Id == CurrentPlayer.Id)
        {
            switch (challenge.Status)
            {
                case ChallengeStatus.Issued:
                    WeakReferenceMessenger.Default.Send(new ChallengeIssued(challenge.Challenger));
                    break;
                case ChallengeStatus.Accepted:
                    WeakReferenceMessenger.Default.Send(new ChallengeResponse(challenge));
                    break;
                case ChallengeStatus.Declined:
                default:
                    return;
            }
        }
    }

}
