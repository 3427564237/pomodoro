namespace APP.Core.Config
{
    public static class InteractionTimings
    {
        // 这一组参数主要在调两件事：提示层停多久，以及翻面判定要多“稳”。
        // These values mainly tune two things: how long overlays stay up, and how strict the flip detection feels.
        public static readonly TimeSpan PutMeDownGrace = TimeSpan.FromSeconds(5);
        public static readonly TimeSpan PutMeDownAutoDismiss = TimeSpan.FromSeconds(5);
        public static readonly TimeSpan BreakPromptAutoDismiss = TimeSpan.FromSeconds(3);
        public static readonly TimeSpan BackToFocusFaceUpGrace = TimeSpan.FromSeconds(3);
        public static readonly TimeSpan FlipDebounce = TimeSpan.FromMilliseconds(300);
        public static readonly TimeSpan FlipCooldown = TimeSpan.FromMilliseconds(500);
        public static readonly TimeSpan TimerTickInterval = TimeSpan.FromMilliseconds(250);
        public static readonly TimeSpan DefaultFocusDuration = TimeSpan.FromMinutes(25);
        public static readonly TimeSpan DefaultBreakDuration = TimeSpan.FromMinutes(5);
        public static readonly int DefaultCycles = 2;

        // 这里只看加速度计的 Z 轴：越接近 -1 越像扣在桌上，越接近 1 越像屏幕朝上。
        // We only look at the accelerometer Z axis here: closer to -1 means face-down on the desk, closer to 1 means face-up.
        public static readonly double FaceDownThreshold = -0.8;
        public static readonly double FaceUpThreshold = 0.8;
        public static readonly double LiftedFromDownThreshold = -0.3;
    }
}
