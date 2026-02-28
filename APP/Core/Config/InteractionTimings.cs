namespace APP.Core.Config
{
    public static class InteractionTimings
    {
        public static readonly TimeSpan PutMeDownGrace = TimeSpan.FromSeconds(5);
        public static readonly TimeSpan PutMeDownAutoDismiss = TimeSpan.FromSeconds(5);
        public static readonly TimeSpan BreakPromptAutoDismiss = TimeSpan.FromSeconds(3);
        public static readonly TimeSpan FlipDebounce = TimeSpan.FromMilliseconds(500);
        public static readonly TimeSpan FlipCooldown = TimeSpan.FromMilliseconds(500);
    }
}
