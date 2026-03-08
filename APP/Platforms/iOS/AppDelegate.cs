using Foundation;

namespace APP
{
    [Register("AppDelegate")]
    public class AppDelegate : MauiUIApplicationDelegate
    {
        // iOS 生命周期挂在这里，但 MAUI 应用本体还是统一从 MauiProgram 创建。
        // The iOS lifecycle hooks live here, but the MAUI app itself is still created from MauiProgram.
        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }
}
