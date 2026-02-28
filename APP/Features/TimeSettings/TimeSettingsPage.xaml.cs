using APP.Core.Navigation;

namespace APP.Features.TimeSettings
{
    public partial class TimeSettingsPage : ContentPage
    {
        private readonly IAppNavigator _navigator;

        public TimeSettingsPage(TimeSettingsViewModel viewModel, IAppNavigator navigator)
        {
            InitializeComponent();
            BindingContext = viewModel;
            _navigator = navigator;
        }

        private async void OnSaveClicked(object sender, EventArgs e)
            => await _navigator.GoBackAsync();
    }
}
