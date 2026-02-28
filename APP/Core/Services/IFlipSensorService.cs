namespace APP.Core.Services
{
    public interface IFlipSensorService
    {
        event Action? FlipDownDetected;
        event Action? FlipUpDetected;
        void StartListening();
        void StopListening();
    }
}
