using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Microsoft.Maui.Platform;
using Permission = Android.Content.PM.Permission;
using MauiColor = Microsoft.Maui.Graphics.Color;

namespace APP
{
    [Activity(Theme = "@style/AppSplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        private const int NotificationPermissionRequestCode = 1001;
        private static MainActivity? _currentActivity;
        private static bool _notificationPermissionRequested;

        public static event Action<bool>? ForegroundChanged;

        public static bool IsAppInForeground { get; private set; }

        // This activity hosts the MAUI window and also tracks foreground state for overlay alerts.
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            // Avoid restoring stale Fragment state when MAUI root page is replaced during startup.
            base.OnCreate(null);
            _currentActivity = this;
        }

        protected override void OnStart()
        {
            base.OnStart();
            _currentActivity = this;
            SetAppInForeground(true);
        }

        protected override void OnResume()
        {
            base.OnResume();
            _currentActivity = this;
            SetAppInForeground(true);
            APP.Platforms.Android.AndroidThemeBridge.RefreshSystemBars();
        }

        protected override void OnStop()
        {
            SetAppInForeground(false);
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

        public static void ApplySystemBarColor(MauiColor color)
        {
            var activity = _currentActivity;
            if (activity?.Window == null)
                return;

            activity.RunOnUiThread(() =>
            {
                var window = activity.Window;
                if (window == null)
                    return;

                window.SetStatusBarColor(color.ToPlatform());

                if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
                {
                    var decor = window.DecorView;
                    var flags = decor.SystemUiFlags;

                    if (ShouldUseDarkStatusBarIcons(color))
                        flags |= SystemUiFlags.LightStatusBar;
                    else
                        flags &= ~SystemUiFlags.LightStatusBar;

                    decor.SystemUiFlags = flags;
                }
            });
        }

        private static bool ShouldUseDarkStatusBarIcons(MauiColor color)
        {
            var luminance = (0.299f * color.Red) + (0.587f * color.Green) + (0.114f * color.Blue);
            return luminance > 0.42f;
        }

        private static void SetAppInForeground(bool value)
        {
            if (IsAppInForeground == value)
                return;

            IsAppInForeground = value;
            ForegroundChanged?.Invoke(value);
        }
    }
}
