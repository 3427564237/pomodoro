using Microsoft.Extensions.Logging;
using APP.Core.Navigation;
using APP.Core.Services;
using APP.Core.StateMachine;
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

            // Core services
            builder.Services.AddSingleton<ITimerEngine, TimerEngine>();
            builder.Services.AddSingleton<IPomodoroCoordinator>(sp =>
                new PomodoroStateMachine(
                    sp.GetRequiredService<ITimerEngine>(),
                    sp.GetRequiredService<IAppNavigator>()));

            // Flip sensor (Android implementation)
            builder.Services.AddSingleton<IFlipSensorService, APP.Platforms.Android.FlipSensorService>();

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
