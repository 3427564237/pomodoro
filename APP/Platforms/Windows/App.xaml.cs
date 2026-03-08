using Microsoft.UI.Xaml;

namespace APP.WinUI
{
    public partial class App : MauiWinUIApplication
    {
        public App()
        {
            InitializeComponent();
        }

        // Windows 端同样从 MauiProgram 建应用对象，这样平台壳只负责托管，不重复配服务。
        // Windows builds the app from MauiProgram as well, so the platform shell only hosts the app instead of duplicating setup.
        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }
}
