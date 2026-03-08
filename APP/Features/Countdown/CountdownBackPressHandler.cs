namespace APP.Features.Countdown
{
    public static class CountdownBackPressHandler
    {
        // 倒计时页按返回，不是单纯离开页面，而是把当前这轮会话一起收掉。
        // Pressing back on the countdown page should end the current session, not just leave the page behind.
        public static bool Handle(bool hasActiveSession, Action stop)
        {
            if (!hasActiveSession)
                return false;

            stop();
            return true;
        }
    }
}
