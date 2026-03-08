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
                // 这里先用固定时长顶一下，至少各家 Android 机型上都比较稳。
                // Use a fixed-duration buzz for now; it is the most reliable cross-device option on Android.
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
