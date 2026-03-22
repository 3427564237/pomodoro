using APP.Core.Navigation;
using APP.Features.Countdown;
using APP.Features.Main;
using APP.Features.Placeholders;
using APP.Features.Settings;
using APP.Features.TimeSettings;

namespace APP
{
    public partial class AppShell : Shell
    {
        public AppShell(MainPage mainPage)
        {
            InitializeComponent();

            Items.Add(new ShellContent
            {
                Title = "FlipDoro",
                Route = Routes.Main,
                Content = mainPage
            });

            // 首页是 Shell 里的常驻入口，其他这些页都按“次级路由”注册，按需跳进去。
            // The home page stays as the Shell entry point; these pages are registered as secondary routes and opened on demand.
            Routing.RegisterRoute(Routes.Countdown, typeof(CountdownPage));
            Routing.RegisterRoute(Routes.TimeSettings, typeof(TimeSettingsPage));
            Routing.RegisterRoute(Routes.Settings, typeof(SettingsPage));
            Routing.RegisterRoute(Routes.Placeholder, typeof(PlaceholderPage));
        }
    }
}
