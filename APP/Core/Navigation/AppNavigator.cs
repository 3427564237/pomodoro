namespace APP.Core.Navigation
{
    public class AppNavigator : IAppNavigator
    {
        public Task GoToCountdownAsync()
            => Shell.Current.GoToAsync(Routes.Countdown);

        public Task GoToTimeSettingsAsync()
            => Shell.Current.GoToAsync(Routes.TimeSettings);

        public Task GoToSettingsAsync()
            => Shell.Current.GoToAsync(Routes.Settings);

        public Task GoToPlaceholderAsync(string title)
            => Shell.Current.GoToAsync($"{Routes.Placeholder}?title={Uri.EscapeDataString(title)}");

        public Task GoToMainAsync()
            => Shell.Current.GoToAsync(Routes.MainAbsolute);

        public Task GoBackAsync()
            => Shell.Current.GoToAsync("..");
    }
}
