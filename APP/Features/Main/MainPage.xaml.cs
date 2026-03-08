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
            // 首页常驻时就开始听翻面，用户把手机扣下去可以直接从主页面起一轮。
            // Start listening for flips while home is visible so a face-down gesture can launch a session right from the main page.
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
            // 页面一离开就把传感器停掉，省电，也避免别的页面还收到首页的启动手势。
            // Stop the sensor as soon as the page leaves to save work and avoid home-page gestures firing in other screens.
            _flipSensor.StopListening();
            _flipSensor.FlipDownDetected -= OnFlipDown;
        }

        private void OnFlipDown()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                // 翻面回调不保证在 UI 线程，这里统一切回主线程再触发会话启动。
                // Flip callbacks are not guaranteed to arrive on the UI thread, so hop back before starting a session.
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
