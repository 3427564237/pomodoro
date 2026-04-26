using APP.Core.Navigation;
using APP.Core.Services;
using APP.Core.StateMachine;

namespace APP.Features.Main
{
    public partial class MainPage : ContentPage
    {
        private readonly PomodoroStateMachine _coordinator;
        private readonly IFlipSensorService _flipSensor;
        private bool _flipSubscribed;
        private bool _isNavigating;

        public MainPage(MainViewModel viewModel, PomodoroStateMachine coordinator, IFlipSensorService flipSensor)
        {
            InitializeComponent();
            BindingContext = viewModel;
            _coordinator = coordinator;
            _flipSensor = flipSensor;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _isNavigating = false;
            ((MainViewModel)BindingContext).Activate();
            StartFlipListening();
        }

        protected override void OnDisappearing()
        {
            StopFlipListening();
            ((MainViewModel)BindingContext).Deactivate();
            base.OnDisappearing();
        }

        private void StartFlipListening()
        {
            if (_flipSubscribed) return;
            _flipSubscribed = true;
            _flipSensor.FlipDownDetected += OnFlipDown;
            _flipSensor.StartListening();
        }

        private void StopFlipListening()
        {
            if (!_flipSubscribed) return;
            _flipSubscribed = false;
            _flipSensor.StopListening();
            _flipSensor.FlipDownDetected -= OnFlipDown;
        }

        private void OnFlipDown()
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await StartCountdownAsync();
            });
        }

        private async void OnSettingsClicked(object sender, EventArgs e)
            => await NavigateOnceAsync(Routes.Settings);

        private async void OnTimerCircleTapped(object sender, TappedEventArgs e)
            => await NavigateOnceAsync(Routes.TimeSettings);

        private async void OnStartClicked(object sender, EventArgs e)
            => await StartCountdownAsync();

        private async Task StartCountdownAsync()
        {
            if (_isNavigating) return;

            _isNavigating = true;
            try
            {
                await Shell.Current.GoToAsync(Routes.Countdown, false);

                if (!_coordinator.HasActiveSession && !_coordinator.RequestStartFocus())
                {
                    await Shell.Current.GoToAsync("..", false);
                }
            }
            catch
            {
                if (_coordinator.HasActiveSession)
                    _coordinator.Stop();
            }
            finally
            {
                _isNavigating = false;
            }
        }

        private async Task NavigateOnceAsync(string route)
        {
            if (_isNavigating) return;

            _isNavigating = true;
            try
            {
                await Shell.Current.GoToAsync(route, false);
            }
            finally
            {
                _isNavigating = false;
            }
        }

        private async void OnCalendarClicked(object sender, EventArgs e)
            => await Shell.Current.GoToAsync($"{Routes.Placeholder}?title=Calendar");

        private async void OnJournalClicked(object sender, EventArgs e)
            => await Shell.Current.GoToAsync($"{Routes.Placeholder}?title=Journal");

        private async void OnStatsClicked(object sender, EventArgs e)
            => await Shell.Current.GoToAsync($"{Routes.Placeholder}?title=Stats");

        private async void OnTodoClicked(object sender, EventArgs e)
            => await Shell.Current.GoToAsync($"{Routes.Placeholder}?title=To-do");
    }
}
