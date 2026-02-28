using APP.Core.Navigation;
using APP.Core.StateMachine;

namespace APP.Features.Countdown
{
    public partial class CountdownPage : ContentPage
    {
        private readonly IAppNavigator _navigator;
        private readonly IPomodoroCoordinator _coordinator;
        private CountdownViewModel ViewModel => (CountdownViewModel)BindingContext;

        public CountdownPage(CountdownViewModel viewModel, IAppNavigator navigator,
                             IPomodoroCoordinator coordinator)
        {
            InitializeComponent();
            BindingContext = viewModel;
            _navigator = navigator;
            _coordinator = coordinator;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            Shell.SetBackButtonBehavior(this, new BackButtonBehavior { IsVisible = false });
            ViewModel.NavigateToMainRequested += OnNavigateToMain;
            ViewModel.Activate();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            ViewModel.NavigateToMainRequested -= OnNavigateToMain;
            ViewModel.Deactivate();
        }

        /// <summary>
        /// Intercept Android hardware/system back button.
        /// If a session is active, stop it — this triggers <c>SessionEnded</c> which
        /// fires <c>NavigateToMainRequested</c> and navigates back via the single
        /// existing path (no duplicate GoToMainAsync call).
        /// If no session is active, allow the default back-button behavior.
        /// 
        protected override bool OnBackButtonPressed()
        {
            if (!_coordinator.HasActiveSession)
                return base.OnBackButtonPressed();

            // Stop will trigger SessionEnded → NavigateToMainRequested → GoToMainAsync
            _coordinator.Stop();
            return true;
        }

        private void OnStopClicked(object sender, EventArgs e)
        {
            ViewModel.RequestStop();
        }

        private void OnPauseClicked(object sender, EventArgs e)
        {
            ViewModel.TogglePause();
        }

        private void OnSkipClicked(object sender, EventArgs e)
        {
            ViewModel.RequestSkip();
        }

        private void OnOverlayTapped(object sender, TappedEventArgs e)
        {
            ViewModel.RequestOverlayTap();
        }

        private async void OnNavigateToMain()
        {
            await _navigator.GoToMainAsync();
        }
    }
}
