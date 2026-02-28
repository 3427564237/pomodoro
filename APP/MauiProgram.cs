using Microsoft.Extensions.Logging;
using APP.Core.Navigation;
using APP.Features.Main;
using APP.Features.Countdown;
using APP.Features.TimeSettings;
using APP.Features.Settings;
using APP.Features.Placeholders;

namespace APP
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Navigation
            builder.Services.AddSingleton<IAppNavigator, AppNavigator>();

            // Pages
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<CountdownPage>();
            builder.Services.AddTransient<TimeSettingsPage>();
            builder.Services.AddTransient<SettingsPage>();
            builder.Services.AddTransient<PlaceholderPage>();

            // ViewModels
            builder.Services.AddTransient<MainViewModel>();
            builder.Services.AddTransient<CountdownViewModel>();
            builder.Services.AddTransient<TimeSettingsViewModel>();
            builder.Services.AddTransient<SettingsViewModel>();
            builder.Services.AddTransient<PlaceholderViewModel>();

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
