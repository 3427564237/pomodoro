using APP.Core.Navigation;

namespace APP.Features.Countdown
{
    public partial class CountdownPage : ContentPage
    {
        private readonly IAppNavigator _navigator;

        public CountdownPage(CountdownViewModel viewModel, IAppNavigator navigator)
        {
            InitializeComponent();
            BindingContext = viewModel;
            _navigator = navigator;
        }

        private async void OnStopClicked(object sender, EventArgs e)
            => await _navigator.GoToMainAsync();

        private void OnPauseClicked(object sender, EventArgs e)
        {
        }

        private void OnSkipClicked(object sender, EventArgs e)
        {
        }
    }
}
