using Microsoft.Extensions.Logging;
using APP.Core.Config;
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

            // 核心服务，做成单例
            builder.Services.AddSingleton<TimerEngine>();
            builder.Services.AddSingleton<IHapticsService, APP.Platforms.Android.HapticsService>();
            builder.Services.AddSingleton<IFlipSensorService, APP.Platforms.Android.FlipSensorService>();

            // 状态机
            builder.Services.AddSingleton(sp =>
            {
                var flipSensor = sp.GetRequiredService<IFlipSensorService>();
                return new PomodoroStateMachine(
                    sp.GetRequiredService<TimerEngine>(),
                    sp.GetRequiredService<IHapticsService>(),
                    InteractionTimings.BreakPromptAutoDismiss,
                    InteractionTimings.PutMeDownAutoDismiss,
                    isFaceUpQuery: () => flipSensor.CurrentOrientation == FlipOrientation.FaceUp);
            });

            // 页面
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<CountdownPage>();
            builder.Services.AddTransient<TimeSettingsPage>();
            builder.Services.AddTransient<SettingsPage>();
            builder.Services.AddTransient<PlaceholderPage>();

            // ViewModel
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
