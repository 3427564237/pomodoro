using System.Diagnostics;
using APP.Core.Config;
using APP.Core.StateMachine;
using APP.Features.Loading;
using Microsoft.Extensions.DependencyInjection;

namespace APP
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            ApplySavedTheme();

            AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            {
                Debug.WriteLine($"[UnhandledException] {args.ExceptionObject}");
            };

            TaskScheduler.UnobservedTaskException += (_, args) =>
            {
                Debug.WriteLine($"[UnobservedTaskException] {args.Exception}");
                args.SetObserved();
            };
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new LoadingPage());
        }

        private static void ApplySavedTheme()
        {
            try
            {
                var stateMachine = MauiProgram.Services.GetRequiredService<PomodoroStateMachine>();
                var themeService = MauiProgram.Services.GetRequiredService<ThemeService>();
                themeService.ApplyTheme(stateMachine.Config.Theme);
            }
            catch
            {
                // Startup should continue even if theme resources are not ready.
            }
        }
    }
}
