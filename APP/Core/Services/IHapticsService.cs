namespace APP.Core.Services
{
    public interface IHapticsService
    {
        void PlayShortBuzz();
        void StartContinuousVibration();
        void StopVibration();
    }
}
