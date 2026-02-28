namespace APP.Core.Config
{
    public static class InteractionTimings
    {
        public static readonly TimeSpan PutMeDownGrace = TimeSpan.FromSeconds(5);
        public static readonly TimeSpan PutMeDownAutoDismiss = TimeSpan.FromSeconds(5);
        public static readonly TimeSpan BreakPromptAutoDismiss = TimeSpan.FromSeconds(3);
        public static readonly TimeSpan FlipDebounce = TimeSpan.FromMilliseconds(300);
        public static readonly TimeSpan FlipCooldown = TimeSpan.FromMilliseconds(500);
        public static readonly TimeSpan TimerTickInterval = TimeSpan.FromMilliseconds(250);
        public static readonly TimeSpan DefaultFocusDuration = TimeSpan.FromMinutes(25);
        public static readonly TimeSpan DefaultBreakDuration = TimeSpan.FromMinutes(5);
        public static readonly int DefaultCycles = 2;

        // Flip detection thresholds (accelerometer Z-axis, normalized to g)
        public static readonly double FaceDownThreshold = -0.8;
        public static readonly double FaceUpThreshold = 0.8;
    }
}
