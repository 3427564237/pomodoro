namespace APP.Core.Services
{
    public interface IFlipSensorService
    {
        event Action? FlipDownDetected;
        event Action? FlipUpDetected;
        FlipOrientation CurrentOrientation { get; }
        void StartListening();
        void StopListening();
    }
}
