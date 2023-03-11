using System;
using Weather.Services;

namespace Weather.ViewModels;

public partial class MainViewModel : ViewModel
{
    private readonly IWeatherService weatherService;

    public MainViewModel(IWeatherService weatherService)
    {
        this.weatherService = weatherService;
    }

    public async Task LoadDataAsync()
    {
        var status = await AppPermissions.CheckAndRequestRequiredPermissionAsync();
        if (status == PermissionStatus.Granted)
        {
            var location = await Geolocation.GetLastKnownLocationAsync() ??
                           await Geolocation.GetLocationAsync();

            var forecast = await weatherService.GetForecastAsync(location.Latitude, location.Longitude);
        }
    }
}
