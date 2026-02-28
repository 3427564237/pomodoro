using APP.Core.Navigation;
using APP.Core.Services;
using APP.Core.StateMachine;

namespace APP.Features.Main
{
    public partial class MainPage : ContentPage
    {
        private readonly IAppNavigator _navigator;
        private readonly IPomodoroCoordinator _coordinator;
        private readonly IFlipSensorService _flipSensor;
        private bool _flipSubscribed;

        public MainPage(MainViewModel viewModel, IAppNavigator navigator,
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
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _coordinator.RequestStartFocus();
            });
        }

        private async void OnSettingsClicked(object sender, EventArgs e)
            => await _navigator.GoToSettingsAsync();

        private async void OnTimerCircleTapped(object sender, TappedEventArgs e)
            => await _navigator.GoToTimeSettingsAsync();

        private void OnStartClicked(object sender, EventArgs e)
        {
            _coordinator.RequestStartFocus();
        }

        private async void OnCalendarClicked(object sender, EventArgs e)
            => await _navigator.GoToPlaceholderAsync("Calendar");

        private async void OnJournalClicked(object sender, EventArgs e)
            => await _navigator.GoToPlaceholderAsync("Journal");

        private async void OnStatsClicked(object sender, EventArgs e)
            => await _navigator.GoToPlaceholderAsync("Stats");

        private async void OnTodoClicked(object sender, EventArgs e)
            => await _navigator.GoToPlaceholderAsync("To-do");
    }
}
