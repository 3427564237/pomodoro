using Android.App;
using Android.Content.PM;
using Android.OS;

namespace APP
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        // Android 端真正展示窗口的入口 activity，其他 MAUI 页面都会挂在它下面。
        // This is the Android activity that hosts the app window; every MAUI page ultimately lives under it.
    }
}
