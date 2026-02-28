using APP.Core.Navigation;

namespace APP.Features.Main
{
    public partial class MainPage : ContentPage
    {
        private readonly IAppNavigator _navigator;

        public MainPage(MainViewModel viewModel, IAppNavigator navigator)
        {
            InitializeComponent();
            BindingContext = viewModel;
            _navigator = navigator;
        }

        private async void OnSettingsClicked(object sender, EventArgs e)
            => await _navigator.GoToSettingsAsync();

        private async void OnTimerCircleTapped(object sender, TappedEventArgs e)
            => await _navigator.GoToTimeSettingsAsync();

        private async void OnStartClicked(object sender, EventArgs e)
            => await _navigator.GoToCountdownAsync();

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
