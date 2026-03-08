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
            // 这个页面自己接管返回键，避免用户退回去以后计时还在后台偷偷跑。
            // This page takes over the back button so the timer does not keep running in the background after the user leaves.
            Shell.SetBackButtonBehavior(this, new BackButtonBehavior { IsVisible = false });
            ViewModel.NavigateToMainRequested += OnNavigateToMain;
            _coordinator.ConfigChanged += OnConfigChanged;
            ViewModel.Activate();

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
            var overlay = _coordinator.CurrentOverlay;

            // 不同 overlay 的确认动作不完全一样，这里别偷懒全走同一个入口。
            // Not every overlay confirms the same way, so do not funnel all taps through one generic handler.
            if (overlay == APP.Core.Models.OverlayState.PutMeDown)
                ViewModel.RequestPutMeDownTap();
            else if (overlay == APP.Core.Models.OverlayState.BackToFocus)
                ViewModel.RequestBackToFocusTap();
            else
                ViewModel.RequestOverlayTap();
        }

        private async void OnNavigateToMain()
        {
            await _navigator.GoToMainAsync();
        }
    }
}
