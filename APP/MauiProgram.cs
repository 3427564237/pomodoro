using APP.Core.Services;
using APP.Core.StateMachine;
using APP.Core.Config;
using APP.Features.Main;
using APP.Features.Countdown;
using APP.Features.TimeSettings;
using APP.Features.Settings;
using APP.Features.Placeholders;
using APP.Platforms.Android;

namespace APP
{
    public static class MauiProgram
    {
        public static IServiceProvider Services { get; private set; } = null!;

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

            // 初始化核心服务
            var timerEngine = new TimerEngine();
            var flipSensorService = new FlipSensorService();
            var settingsStore = new AppSettingsStore();
            var themeService = new ThemeService();
            var initialConfig = settingsStore.Load();

            var pomodoroStateMachine = new PomodoroStateMachine(timerEngine,
                () => flipSensorService.CurrentOrientation == FlipOrientation.FaceUp,
                initialConfig,
                settingsStore,
                themeService);
            var overlayAlertService = new OverlayAlertService(pomodoroStateMachine);

            // 注册页面和ViewModel
            builder.Services.AddSingleton<AppShell>();
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

            // 提供服务实例给应用
            builder.Services.AddSingleton(pomodoroStateMachine);
            builder.Services.AddSingleton(timerEngine);
            builder.Services.AddSingleton(settingsStore);
            builder.Services.AddSingleton(themeService);
            builder.Services.AddSingleton(overlayAlertService);
            builder.Services.AddSingleton<IFlipSensorService>(flipSensorService);

            var app = builder.Build();
            Services = app.Services;
            return app;
        }
    }
}
