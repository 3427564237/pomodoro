namespace APP.Core.Config
{
    // StrictMode 默认打开，这样开箱就是“翻过来会提醒放下”的完整体验。
    // StrictMode starts enabled so the default out-of-box behavior includes the face-up reminder.
    public record RuntimeConfig(
        int Cycles,
        TimeSpan FocusDuration,
        TimeSpan BreakDuration,
        bool StrictModeEnabled = true);
}
