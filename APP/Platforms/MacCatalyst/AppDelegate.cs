using Foundation;

namespace APP
{
    [Register("AppDelegate")]
    public class AppDelegate : MauiUIApplicationDelegate
    {
        // MacCatalyst 这层主要是接系统生命周期，真正的应用装配还是交给 MauiProgram。
        // On MacCatalyst this layer mainly receives lifecycle callbacks; real app composition still comes from MauiProgram.
        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }
}
