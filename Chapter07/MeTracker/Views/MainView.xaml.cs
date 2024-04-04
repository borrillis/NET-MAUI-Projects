using MeTracker.ViewModels;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;

namespace MeTracker.Views;

public partial class MainView : ContentPage
{
    public MainView(MainViewModel viewModel)
	{
		InitializeComponent();

        BindingContext = viewModel;

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            var status = await AppPermissions.CheckAndRequestRequiredPermissionAsync();
            if (status == PermissionStatus.Granted)
            {
                var location = await Geolocation.GetLastKnownLocationAsync() ?? await Geolocation.GetLocationAsync();
                if (location is not null)
                {
                    Map.MoveToRegion(MapSpan.FromCenterAndRadius(
                        location,
                        Distance.FromKilometers(5)));
                }
            }
        });

    }
}