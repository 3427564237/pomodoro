using APP.Core.Navigation;
using APP.Core.Services;
using APP.Core.StateMachine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Devices;

namespace APP.Features.Countdown
{
    public partial class CountdownPage : ContentPage
    {
        private readonly PomodoroStateMachine _coordinator;
        private readonly IFlipSensorService _flipSensor;
        private bool _flipSubscribed;

        private CountdownViewModel ViewModel => (CountdownViewModel)BindingContext;

        public CountdownPage()
            : this(
                MauiProgram.Services.GetRequiredService<CountdownViewModel>(),
                MauiProgram.Services.GetRequiredService<PomodoroStateMachine>(),
                MauiProgram.Services.GetRequiredService<IFlipSensorService>())
        {
        }

        public CountdownPage(CountdownViewModel viewModel, PomodoroStateMachine coordinator, IFlipSensorService flipSensor)
        {
            InitializeComponent();
            BindingContext = viewModel;
            _coordinator = coordinator;
            _flipSensor = flipSensor;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            ApplyKeepScreenOnSetting(_coordinator.Config.KeepScreenOnEnabled);
#if ANDROID
            MainActivity.RequestNotificationPermissionIfNeeded();
#endif
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
            ApplyKeepScreenOnSetting(false);
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
                ApplyKeepScreenOnSetting(config.KeepScreenOnEnabled);
                if (config.StrictModeEnabled)
                    StartFlipListening();
                else
                    StopFlipListening();
            });
        }

        private static void ApplyKeepScreenOnSetting(bool enabled)
        {
            DeviceDisplay.Current.KeepScreenOn = enabled;
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
            => ViewModel.RequestStop();

        private void OnPauseClicked(object sender, EventArgs e)
            => ViewModel.TogglePause();

        private void OnSkipClicked(object sender, EventArgs e)
            => ViewModel.RequestSkip();

        private void OnOverlayTapped(object sender, TappedEventArgs e)
        {
            var overlay = _coordinator.CurrentOverlay;

            if (overlay == APP.Core.Models.OverlayState.PutMeDown)
                ViewModel.RequestPutMeDownTap();
            else if (overlay == APP.Core.Models.OverlayState.BackToFocus)
                ViewModel.RequestBackToFocusTap();
            else
                ViewModel.RequestOverlayTap();
        }

        private async void OnNavigateToMain()
        {
            await Shell.Current.GoToAsync(Routes.MainAbsolute);
        }
    }
}
