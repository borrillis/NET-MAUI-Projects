using Microsoft.Maui.Maps;

namespace MeTracker.Views;

public partial class MainView : ContentPage
{
	public MainView()
	{
		InitializeComponent();
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            var status = await AppPermissions.CheckAndRequestRequiredPermissionAsync();
            if (status == PermissionStatus.Granted)
            {
                var location = await Geolocation.GetLastKnownLocationAsync();

                if (location == null)
                {
                    location = await Geolocation.GetLocationAsync();
                }

                Map.MoveToRegion(MapSpan.FromCenterAndRadius(
                    location,
                    Distance.FromKilometers(5)));
            }
        });

    }
}