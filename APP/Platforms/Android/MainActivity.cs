using Android.App;
using Android.Content.PM;
using Android.OS;
using Permission = Android.Content.PM.Permission;

namespace APP
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        private const int NotificationPermissionRequestCode = 1001;
        private static MainActivity? _currentActivity;
        private static bool _notificationPermissionRequested;

        public static bool IsAppInForeground { get; private set; }

        // This activity hosts the MAUI window and also tracks foreground state for overlay alerts.
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            _currentActivity = this;
        }

        protected override void OnResume()
        {
            base.OnResume();
            _currentActivity = this;
            IsAppInForeground = true;
        }

        protected override void OnStop()
        {
            IsAppInForeground = false;
            base.OnStop();
        }

        protected override void OnDestroy()
        {
            if (ReferenceEquals(_currentActivity, this))
                _currentActivity = null;

            base.OnDestroy();
        }

        public static void RequestNotificationPermissionIfNeeded()
        {
            if (_notificationPermissionRequested || Build.VERSION.SdkInt < BuildVersionCodes.Tiramisu)
                return;

            var activity = _currentActivity;
            if (activity == null)
                return;

            if (activity.CheckSelfPermission(Android.Manifest.Permission.PostNotifications) == Permission.Granted)
            {
                _notificationPermissionRequested = true;
                return;
            }

            _notificationPermissionRequested = true;
            activity.RequestPermissions(
                [Android.Manifest.Permission.PostNotifications],
                NotificationPermissionRequestCode);
        }
    }
}
