using System.Reflection;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SticksAndStones.Models;
using SticksAndStones.Services;

namespace SticksAndStones.ViewModels;

public partial class ConnectViewModel : ObservableObject
{
    private const string PlayerKey = nameof(PlayerKey);
    private const string ServerUrlKey = nameof(ServerUrlKey);
#if DEBUG && ANDROID
    private const string ServerUrlDefault = "http://10.0.2.2:7071/api";
#else
    private const string ServerUrlDefault = "http://localhost:7071/api";
#endif   

    private readonly LobbyService lobbyService;
    private readonly Player player;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConnectCommand))]
    [NotifyCanExecuteChangedFor(nameof(TakePhotoCommand))]
    private bool isConnecting;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConnectCommand))]
    [NotifyCanExecuteChangedFor(nameof(TakePhotoCommand))]
    private bool isVerifingImage;

    [ObservableProperty]
    private string imageStatus;

    [ObservableProperty]
    private string connectStatus;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConnectCommand))]
    private string username;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConnectCommand))]
    private string emailAddress;

    [ObservableProperty]
    private string avatarImage;

    public ConnectViewModel(LobbyService lobbyService)
    {
        this.lobbyService = lobbyService;
        ImageStatus = "Tap to Update image";
        ConnectStatus = "Connect";

        string serverUrl;
        //Initialize Server Url
        if (Preferences.ContainsKey(ServerUrlKey))
        {
            serverUrl = Preferences.Get(ServerUrlKey, ServerUrlDefault);
        }
        else
        {
            serverUrl = ServerUrlDefault;
            Preferences.Set(ServerUrlKey, serverUrl);
        }

        //lobbyService.SetServerUrl(serverUrl);

        // Load Player settings
        AvatarImage = "profile.png";
        if (Preferences.ContainsKey(PlayerKey))
        {
            player = JsonSerializer.Deserialize<Player>(Preferences.Get(PlayerKey, "{}"));
            Username = player.Name;
            EmailAddress = player.EmailAddress;
            AvatarImage = string.IsNullOrEmpty(player.AvatarUrl) ? AvatarImage : player.AvatarUrl;
        }
        else
        {
            player = new Player();
        }

        if (!string.IsNullOrEmpty(player.Id) && !string.IsNullOrEmpty(player.Name))
        {
            //Task.Run(async () => await Connect());
        }
    }

    [RelayCommand(CanExecute = nameof(CanExecuteConnect))]
    public async Task Connect()
    {
        IsConnecting = true;
        ConnectStatus = "Connecting...";

        player.Name = Username;
        player.EmailAddress = EmailAddress;

        Preferences.Set(PlayerKey, JsonSerializer.Serialize(player));

        player.Id = (await Connect(player)).Id;

        ConnectStatus = "Connect";
        IsConnecting = false;
    }

    private bool CanExecuteConnect() => !string.IsNullOrEmpty(Username) && !string.IsNullOrEmpty(EmailAddress) && !IsConnecting && !IsVerifingImage;

    private async Task<Player> Connect(Player player)
    {
        // Get SignalR Connection
        var playerUpdate = await lobbyService.Connect(player);

        if (lobbyService.IsConnected)
        {
            await Shell.Current.GoToAsync($"//Lobby");
        }
        return playerUpdate;
    }

    [RelayCommand(CanExecute = nameof(CanExecuteTakePhoto))]
    public async void TakePhoto()
    {
        if (MediaPicker.Default.IsCaptureSupported)
        {
            var status = await AppPermissions.CheckAndRequestRequiredPermissionAsync();
            if (status == PermissionStatus.Granted)
            {
                FileResult photo = await MediaPicker.Default.CapturePhotoAsync();

                if (photo != null)
                {
                    using var stream = await photo.OpenReadAsync();
                    var imageType = photo.ContentType.Split("/").Last();
                    var converter = new Converters.Base64ToImageConverter();
                    ImageStatus = "Verifying...";
                    IsVerifingImage = true;
                    var base64Image = (string)converter.ConvertBack(stream, typeof(string), null, null);
                    var photoUrl = await lobbyService.VerifyImage(player.Id, base64Image, imageType);
                    if (!string.IsNullOrEmpty(photoUrl))
                    {
                        AvatarImage = photoUrl;
                        player.AvatarUrl = photoUrl;
                    };
                    IsVerifingImage = false;
                    ImageStatus = "Click to update image.";
                }
            }
        }
    }

    private bool CanExecuteTakePhoto() => !IsConnecting && !IsVerifingImage;

}

