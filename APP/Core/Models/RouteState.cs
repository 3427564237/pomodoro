namespace APP.Core.Models
{
    // RouteState 预留给更显式的页面状态表达，和 Shell 的字符串路由不是一回事。
    // RouteState is reserved for an explicit app-level page state model; it is not the same thing as Shell route strings.
    public enum RouteState
    {
        Main,
        TimeSettings,
        Countdown,
        Settings,
        Placeholder
    }
}
