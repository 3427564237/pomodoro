namespace APP.Core.Models
{
    // Phase 只描述计时流程走到哪一段，不负责表达页面上的提示层。
    // Phase only describes which timer segment we are in; overlays are tracked separately.
    public enum PhaseState
    {
        Idle,
        Focus,
        Break
    }
}
