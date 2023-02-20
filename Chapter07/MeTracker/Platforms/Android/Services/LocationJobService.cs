using Android.App;
using Android.App.Job;

namespace MeTracker.Platforms.Android.Services;

[Service(Name = "MeTracker.Platforms.Android.Services.LocationJobService", Permission = "android.permission.BIND_JOB_SERVICE")]
internal class LocationJobService : JobService
{ 
    public override bool OnStartJob(JobParameters @params)
    {
        return true;
    }

    public override bool OnStopJob(JobParameters @params)
    {
        return true;
    }
}

