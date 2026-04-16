namespace APP.Core.Models
{
    // 计时器每次对外广播都带这个快照，避免外层再去拼零散状态。
    // Every timer update is published as this snapshot so callers do not have to reconstruct state from scattered fields.
    public readonly record struct TimerSnapshot(
        TimeSpan Total,
        TimeSpan Remaining,
        bool IsRunning);
}
