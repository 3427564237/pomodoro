using Android.App;
using Android.Runtime;

namespace APP
{
    [Application]
    public class MainApplication : MauiApplication
    {
        public MainApplication(IntPtr handle, JniHandleOwnership ownership)
            : base(handle, ownership)
        {
        }

        // Android 启动链最后还是回到这一个 MauiProgram，保证各平台拿到的是同一套依赖配置。
        // Android still resolves its app from the same MauiProgram so every platform shares one dependency setup.
        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }
}
