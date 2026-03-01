namespace APP.Features.Countdown
{
    /// <summary>
    /// Handles back-button behavior for the countdown page.
    /// 
    /// 
    public static class CountdownBackPressHandler
    {
        /// <summary>
        /// Determine how to handle a back-button press on the Countdown page.
        /// 
        /// <param name="hasActiveSession">Whether a timer session is currently active.</param>
        /// <param name="stop">Action to stop the active session.</param>
        /// <returns>
        /// <c>true</c> if the back press is consumed (active session was stopped,
        /// navigation will follow via SessionEnded);
        /// <c>false</c> if the press should fall through to default system behavior.
        /// </returns>
        public static bool Handle(bool hasActiveSession, Action stop)
        {
            if (!hasActiveSession)
                return false;

            stop();
            return true;
        }
    }
}
