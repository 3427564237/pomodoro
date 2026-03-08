namespace APP.Features.Countdown
{
    /// <summary>
    /// Handles back-button behavior for the countdown page.
    /// 
    public static class CountdownBackPressHandler
    {
        /// <summary>
        /// Determines how to handle a back-button press.
        /// 
        /// <param name="hasActiveSession">Whether a timer session is currently active.</param>
        /// <param name="stop">Action to stop the active session.</param>
        /// <returns><c>true</c> if the back press is consumed.</returns>
        public static bool Handle(bool hasActiveSession, Action stop)
        {
            if (!hasActiveSession)
                return false;

            stop();
            return true;
        }
    }
}
