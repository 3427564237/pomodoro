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
            // 标题走 query string，这样占位页本身不用再为每个入口单独建一页。
            // Pass the title through the query string so one placeholder page can stand in for several future sections.
            => Shell.Current.GoToAsync($"{Routes.Placeholder}?title={Uri.EscapeDataString(title)}");

        public Task GoToMainAsync()
            => Shell.Current.GoToAsync(Routes.MainAbsolute);

        public Task GoBackAsync()
            => Shell.Current.GoToAsync("..");
    }
}
