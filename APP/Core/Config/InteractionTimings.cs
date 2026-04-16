namespace APP.Core.Config
{
    public static class Constants
    {
        // 番茄钟的基本时间设置
        public const int DefaultCycles = 2;
        public static readonly TimeSpan DefaultFocusDuration = TimeSpan.FromMinutes(25);
        public static readonly TimeSpan DefaultBreakDuration = TimeSpan.FromMinutes(5);

        // 界面提示的显示时间
        public static readonly TimeSpan OverlayDisplayTime = TimeSpan.FromSeconds(3);
        public static readonly TimeSpan PutMeDownDisplayTime = TimeSpan.FromSeconds(5);

        // 翻转检测相关
        public static readonly TimeSpan FlipDebounce = TimeSpan.FromMilliseconds(300);
        public static readonly TimeSpan FaceUpGraceDelay = TimeSpan.FromSeconds(3);

        // 加速度计阈值：Z轴接近-1表示屏幕朝下，接近1表示屏幕朝上
        public const double FaceDownThreshold = -0.8;
        //z小于等于-0.8表示屏幕朝下，z大于等于0.8表示屏幕朝上
        public const double FaceUpThreshold = 0.8;
        public const double LiftedFromDownThreshold = -0.3;
        // face down状态下，z轴从小于-0.8上升到大于-0.3，表示被拿起
    }
}
