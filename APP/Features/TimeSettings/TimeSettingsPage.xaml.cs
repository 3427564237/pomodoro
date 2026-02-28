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

        protected override void OnAppearing()
        {
            base.OnAppearing();
            ((TimeSettingsViewModel)BindingContext).LoadFromConfig();
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            var vm = (TimeSettingsViewModel)BindingContext;
            if (vm.TrySave())
                await _navigator.GoBackAsync();
        }

        private async void OnCancelClicked(object sender, EventArgs e)
            => await _navigator.GoBackAsync();
    }
}
