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
}
