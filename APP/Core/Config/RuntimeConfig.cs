namespace APP.Core.Config
{
    // 番茄钟的运行配置
    public class RuntimeConfig
    {
        public int Cycles { get; set; }
        public TimeSpan FocusDuration { get; set; }
        public TimeSpan BreakDuration { get; set; }
        public bool StrictModeEnabled { get; set; } = true;
        public bool VibrationEnabled { get; set; } = true;

        public RuntimeConfig(int cycles, TimeSpan focusDuration, TimeSpan breakDuration)
        {
            Cycles = cycles;
            FocusDuration = focusDuration;
            BreakDuration = breakDuration;
        }

        public RuntimeConfig Copy()
        {
            return new RuntimeConfig(Cycles, FocusDuration, BreakDuration)
            {
                StrictModeEnabled = StrictModeEnabled,
                VibrationEnabled = VibrationEnabled
            };
        }
    }
}
