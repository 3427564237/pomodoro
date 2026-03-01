using APP.Core.Services;

namespace APP.Platforms.Android
{
    public sealed class HapticsService : IHapticsService
    {
        public void PlayShortBuzz()
        {
            try
            {
                Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(100));
            }
            catch { }
        }

        public void StartContinuousVibration()
        {
            try
            {
                // Repeated short bursts — platform-safe approximation.
                // Full pattern-based vibration will be implemented in P0-6.
                Vibration.Default.Vibrate(TimeSpan.FromSeconds(5));
            }
            catch { }
        }

        public void StopVibration()
        {
            try
            {
                Vibration.Default.Cancel();
            }
            catch { }
        }
    }
}
