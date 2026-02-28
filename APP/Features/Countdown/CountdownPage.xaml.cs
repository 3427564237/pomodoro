using APP.Core.Navigation;

namespace APP.Features.Countdown
{
    public partial class CountdownPage : ContentPage
    {
        private readonly IAppNavigator _navigator;
        private CountdownViewModel ViewModel => (CountdownViewModel)BindingContext;

        public CountdownPage(CountdownViewModel viewModel, IAppNavigator navigator)
        {
            InitializeComponent();
            BindingContext = viewModel;
            _navigator = navigator;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            ViewModel.Activate();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            ViewModel.Deactivate();
        }

        private async void OnStopClicked(object sender, EventArgs e)
        {
            ViewModel.RequestStop();
            await _navigator.GoToMainAsync();
        }

        private void OnPauseClicked(object sender, EventArgs e)
        {
            ViewModel.TogglePause();
        }

        private void OnSkipClicked(object sender, EventArgs e)
        {
            ViewModel.RequestSkip();
        }
    }
}
