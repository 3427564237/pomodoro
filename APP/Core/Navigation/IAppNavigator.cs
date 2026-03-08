namespace APP.Core.Navigation
{
    // 页面层只依赖这个接口，不直接碰 Shell，这样导航规则改动时影响面会小很多。
    // Pages depend on this interface instead of touching Shell directly, which keeps navigation changes contained.
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
