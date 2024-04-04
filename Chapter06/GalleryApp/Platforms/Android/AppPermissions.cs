[assembly: Android.App.UsesPermission(Android.Manifest.Permission.ReadMediaImages)]
[assembly: Android.App.UsesPermission(Android.Manifest.Permission.ReadExternalStorage, MaxSdkVersion = 32)]

namespace GalleryApp;

using Android.OS;

internal partial class AppPermissions
{
    internal partial class AppPermission : Permissions.Photos
    {
        public override (string androidPermission, bool isRuntime)[] RequiredPermissions
        {
            get
            {
#pragma warning disable CA1416 // Validate platform compatibility
                List<(string androidPermission, bool isRuntime)> perms =
                [
                    Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu ? (Android.Manifest.Permission.ReadMediaImages, true) : (Android.Manifest.Permission.ReadExternalStorage, true),
                ];
#pragma warning restore CA1416 // Validate platform compatibility

                return [.. perms];
            }
        }
    }
}
