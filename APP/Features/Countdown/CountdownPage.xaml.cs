using APP.Core.Navigation;
using APP.Core.Services;
using APP.Core.StateMachine;

namespace APP.Features.Countdown
{
    public partial class CountdownPage : ContentPage
    {
        private readonly IAppNavigator _navigator;
        private readonly IPomodoroCoordinator _coordinator;
        private readonly IFlipSensorService _flipSensor;
        private bool _flipSubscribed;

        private CountdownViewModel ViewModel => (CountdownViewModel)BindingContext;

        public CountdownPage(CountdownViewModel viewModel, IAppNavigator navigator,
                             IPomodoroCoordinator coordinator, IFlipSensorService flipSensor)
        {
            InitializeComponent();
            BindingContext = viewModel;
            _navigator = navigator;
            _coordinator = coordinator;
            _flipSensor = flipSensor;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            Shell.SetBackButtonBehavior(this, new BackButtonBehavior { IsVisible = false });
            ViewModel.NavigateToMainRequested += OnNavigateToMain;
            _coordinator.ConfigChanged += OnConfigChanged;
            ViewModel.Activate();

            // 
            if (_coordinator.Config.StrictModeEnabled)
                StartFlipListening();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            StopFlipListening();
            _coordinator.ConfigChanged -= OnConfigChanged;
            ViewModel.NavigateToMainRequested -= OnNavigateToMain;
            ViewModel.Deactivate();
        }

        /// <summary>
        /// Intercept Android hardware/system back button.
        /// 
        /// 
        protected override bool OnBackButtonPressed()
        {
            var consumed = CountdownBackPressHandler.Handle(
                _coordinator.HasActiveSession,
                () => _coordinator.Stop());

            return consumed || base.OnBackButtonPressed();
        }

        private void OnConfigChanged(APP.Core.Config.RuntimeConfig config)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (config.StrictModeEnabled)
                    StartFlipListening();
                else
                    StopFlipListening();
            });
        }

        private void StartFlipListening()
        {
            if (_flipSubscribed) return;
            _flipSubscribed = true;
            _flipSensor.FlipUpDetected += OnFlipUp;
            _flipSensor.FlipDownDetected += OnFlipDown;
            _flipSensor.StartListening();
        }

        private void StopFlipListening()
        {
            if (!_flipSubscribed) return;
            _flipSubscribed = false;
            _flipSensor.StopListening();
            _flipSensor.FlipUpDetected -= OnFlipUp;
            _flipSensor.FlipDownDetected -= OnFlipDown;
        }

        private void OnFlipUp()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _coordinator.OnFlipUpDetected();
            });
        }

        private void OnFlipDown()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _coordinator.OnFlipDownDetected();
            });
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
            // Route to appropriate handler based on current overlay type
            if (_coordinator.CurrentOverlay == APP.Core.Models.OverlayState.PutMeDown)
                ViewModel.RequestPutMeDownTap();
            else
                ViewModel.RequestOverlayTap();
        }

        private async void OnNavigateToMain()
        {
            await _navigator.GoToMainAsync();
        }
    }
}
