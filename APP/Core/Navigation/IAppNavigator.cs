namespace APP.Core.Navigation
{
    public interface IAppNavigator
    {
        Task GoToCountdownAsync();
        Task GoToTimeSettingsAsync();
        Task GoToSettingsAsync();
        Task GoToPlaceholderAsync(string title);
        Task GoToMainAsync();
        Task GoBackAsync();
    }
}
