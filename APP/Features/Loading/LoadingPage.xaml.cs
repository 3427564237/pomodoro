using APP.Core.Config;
using APP.Core.StateMachine;
using Microsoft.Extensions.DependencyInjection;

namespace APP.Features.Loading
{
    public partial class LoadingPage : ContentPage
    {
        private bool _hasStarted;

        public LoadingPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (_hasStarted) return;
            _hasStarted = true;
            ApplySavedTheme();

            await Task.Delay(650);

            OpenMainPage();
        }

        private static void OpenMainPage()
        {
            var appShell = MauiProgram.Services.GetRequiredService<AppShell>();
            var window = Application.Current?.Windows.FirstOrDefault();

            if (window != null)
            {
                window.Page = appShell;
            }
        }

        private static void ApplySavedTheme()
        {
            var stateMachine = MauiProgram.Services.GetRequiredService<PomodoroStateMachine>();
            var themeService = MauiProgram.Services.GetRequiredService<ThemeService>();
            themeService.ApplyTheme(stateMachine.Config.Theme);
        }
    }
}
