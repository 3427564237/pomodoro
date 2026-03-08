using Microsoft.Extensions.DependencyInjection;

namespace APP
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            // 应用启动先交给 Shell，后面的页面跳转和路由都按同一套路走。
            // Start the app with Shell so the rest of the navigation stack and route handling all follow one path.
            return new Window(new AppShell());
        }
    }
}
