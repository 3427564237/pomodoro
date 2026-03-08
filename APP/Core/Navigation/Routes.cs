namespace APP.Core.Navigation
{
    public static class Routes
    {
        public const string Main = "MainPage";
        // 带 // 的绝对路由会把返回栈收回首页，适合会话结束后兜底回主页面。
        // The absolute route with // resets the back stack to home, which is useful when a session ends and we want a clean return.
        public const string MainAbsolute = "//MainPage";
        public const string Countdown = "CountdownPage";
        public const string TimeSettings = "TimeSettingsPage";
        public const string Settings = "SettingsPage";
        public const string Placeholder = "PlaceholderPage";
    }
}
