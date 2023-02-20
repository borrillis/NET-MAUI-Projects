using MeTracker.Repositories;
using MeTracker.Services;

namespace MeTracker.ViewModels;

public partial class MainViewModel : ViewModel
{
    private readonly ILocationRepository locationRepository; 
    private readonly ILocationTrackingService locationTrackingService;

    public MainViewModel(ILocationTrackingService locationTrackingService, ILocationRepository locationRepository)
    {
        this.locationTrackingService = locationTrackingService;
        this.locationRepository = locationRepository;
        MainThread.BeginInvokeOnMainThread(() =>
        {
            locationTrackingService.StartTracking();
        });
    }
}