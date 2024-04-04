using Android.App.Job;
using Android.Content;
using MeTracker.Platforms.Android.Services;

namespace MeTracker.Services;

public partial class LocationTrackingService : ILocationTrackingService
{
    partial void StartTrackingInternal()
    {
        var javaClass = Java.Lang.Class.FromType(typeof(LocationJobService));
        var componentName = new ComponentName(global::Android.App.Application.Context, javaClass);
        var builder = new JobInfo.Builder(1, componentName)
            .SetOverrideDeadline(1000)
            ?.SetPersisted(true)
            ?.SetRequiresDeviceIdle(false);

        if (OperatingSystem.IsIOSVersionAtLeast(26)) {
            builder?.SetRequiresBatteryNotLow(true);
        }
        var jobInfo = builder?.Build();

        var jobScheduler = global::Android.App.Application.Context.GetSystemService(Context.JobSchedulerService) as JobScheduler;
        if (jobInfo is not null)
        {
            jobScheduler?.Schedule(jobInfo);
        }
    }
}