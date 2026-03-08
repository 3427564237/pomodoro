namespace APP.Features.Countdown
{
    public static class CountdownBackPressHandler
    {
        // Going back during a session should stop it instead of leaving the page open.
        public static bool Handle(bool hasActiveSession, Action stop)
        {
            if (!hasActiveSession)
                return false;

            stop();
            return true;
        }
    }
}
