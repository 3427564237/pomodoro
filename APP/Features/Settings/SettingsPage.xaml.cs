using APP.Core.Config;
using Microsoft.Extensions.DependencyInjection;

namespace APP.Features.Settings
{
    public partial class SettingsPage : ContentPage
    {
        public SettingsPage()
            : this(MauiProgram.Services.GetRequiredService<SettingsViewModel>())
        {
        }

        public SettingsPage(SettingsViewModel viewModel)
        {
            InitializeComponent();
            // 这页现在只是轻量设置面板，直接绑一个简单 view model 就够了。
            // This page is just a light settings panel for now, so a simple bound view model is enough.
            BindingContext = viewModel;
        }

        private SettingsViewModel ViewModel => (SettingsViewModel)BindingContext;

        protected override void OnAppearing()
        {
            base.OnAppearing();
            ViewModel.Activate();
        }

        protected override void OnDisappearing()
        {
            ViewModel.Deactivate();
            base.OnDisappearing();
        }

        private void OnTropicalThemeTapped(object sender, TappedEventArgs e)
            => SelectTheme(FlipTheme.TropicalSunrise);

        private void OnVioletThemeTapped(object sender, TappedEventArgs e)
            => SelectTheme(FlipTheme.Violet);

        private void SelectTheme(FlipTheme theme)
        {
            ViewModel.SetPressedTheme(theme);
            ViewModel.SelectTheme(theme);
            Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(90), () => ViewModel.SetPressedTheme(null));
        }
    }
}
