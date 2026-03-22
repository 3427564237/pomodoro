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
            => await Shell.Current.GoToAsync(Routes.Settings);

        private async void OnTimerCircleTapped(object sender, TappedEventArgs e)
            => await Shell.Current.GoToAsync(Routes.TimeSettings);

        private void OnStartClicked(object sender, EventArgs e)
            => _coordinator.RequestStartFocus();

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
