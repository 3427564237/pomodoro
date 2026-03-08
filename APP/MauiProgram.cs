using Microsoft.Extensions.Logging;
using APP.Core.Config;
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

            // 这几项都做成单例，页面来回切时会话状态、导航入口和底层服务才不会断掉。
            // Keep these as singletons so session state, navigation, and device services survive page switches.
            builder.Services.AddSingleton<IAppNavigator, AppNavigator>();
            builder.Services.AddSingleton<ITimerEngine, TimerEngine>();
            builder.Services.AddSingleton<IHapticsService, APP.Platforms.Android.HapticsService>();
            builder.Services.AddSingleton<IFlipSensorService, APP.Platforms.Android.FlipSensorService>();

            builder.Services.AddSingleton<IPomodoroCoordinator>(sp =>
            {
                var flipSensor = sp.GetRequiredService<IFlipSensorService>();
                // 状态机只关心“现在是不是正面朝上”，具体传感器细节留在平台层处理。
                // The state machine only asks whether the phone is face-up right now; sensor details stay in the platform layer.
                return new PomodoroStateMachine(
                    sp.GetRequiredService<ITimerEngine>(),
                    sp.GetRequiredService<IAppNavigator>(),
                    sp.GetRequiredService<IHapticsService>(),
                    InteractionTimings.BreakPromptAutoDismiss,
                    InteractionTimings.PutMeDownAutoDismiss,
                    isFaceUpQuery: () => flipSensor.CurrentOrientation == FlipOrientation.FaceUp);
            });

            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<CountdownPage>();
            builder.Services.AddTransient<TimeSettingsPage>();
            builder.Services.AddTransient<SettingsPage>();
            builder.Services.AddTransient<PlaceholderPage>();

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
