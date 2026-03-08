namespace APP.Core.Services
{
    // 核心层只关心“翻上来 / 翻下去”这两个事件，不直接依赖具体传感器 API。
    // The core layer only cares about face-up and face-down events, not the sensor APIs underneath.
    public interface IFlipSensorService
    {
        event Action? FlipDownDetected;
        event Action? FlipUpDetected;
        FlipOrientation CurrentOrientation { get; }
        void StartListening();
        void StopListening();
    }
}
