using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

namespace APP
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

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
            var appShell = MauiProgram.Services.GetRequiredService<AppShell>();
            return new Window(appShell);
        }
    }
}
