
namespace SticksAndStones.Services;

public partial class Settings
{
    private const string PlayerKey = nameof(PlayerKey);
    private const string ServerUrlKey = nameof(ServerUrlKey);

#if DEBUG && ANDROID
    private const string ServerUrlDefault = "http://10.0.2.2:7071/api";
#else
    private const string ServerUrlDefault = "http://localhost:7071/api";
#endif   

    public string ServerUrl
    {
        get
        {
            if (Preferences.ContainsKey(ServerUrlKey))
            {
                return Preferences.Get(ServerUrlKey, ServerUrlDefault);
            }
            return ServerUrlDefault;
        }
        set
        {
            Preferences.Set(ServerUrlKey, value);
        }
    }

    public string LocalPlayer
    {
        get
        {
            if (Preferences.ContainsKey(PlayerKey))
            {
                return Preferences.Get(PlayerKey, null);
            }
            return null;
        }
        set
        {
            Preferences.Set(PlayerKey, value);
        }
    }


}
