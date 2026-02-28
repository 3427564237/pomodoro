using APP.Core.Navigation;
using APP.Features.Countdown;
using APP.Features.Placeholders;
using APP.Features.Settings;
using APP.Features.TimeSettings;

namespace APP
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute(Routes.Countdown, typeof(CountdownPage));
            Routing.RegisterRoute(Routes.TimeSettings, typeof(TimeSettingsPage));
            Routing.RegisterRoute(Routes.Settings, typeof(SettingsPage));
            Routing.RegisterRoute(Routes.Placeholder, typeof(PlaceholderPage));
        }
    }
}
