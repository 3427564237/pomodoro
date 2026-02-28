namespace APP.Core.Models
{
    public readonly record struct TimerSnapshot(
        TimeSpan Total,
        TimeSpan Remaining,
        bool IsRunning);
}
