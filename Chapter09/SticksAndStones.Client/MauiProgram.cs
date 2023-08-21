using Microsoft.Extensions.Logging;

namespace SticksAndStones;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("Font Awesome 6 Free-Regular-400.otf", "FontAwesome");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif
        builder.Services.AddSingleton<Services.Settings>();
        builder.Services.AddSingleton<Services.ServiceConnection>();

        builder.Services.AddSingleton<Services.LobbyService>();

        builder.Services.AddTransient<ViewModels.ConnectViewModel>();
        builder.Services.AddTransient<ViewModels.GameViewModel>();
        builder.Services.AddTransient<ViewModels.LobbyViewModel>();

        builder.Services.AddTransient<Views.ConnectView>();
        builder.Services.AddTransient<Views.GameView>();
        builder.Services.AddTransient<Views.LobbyView>();

        return builder.Build();
    }
}

