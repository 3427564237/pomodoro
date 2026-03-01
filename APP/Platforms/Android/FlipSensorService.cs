using APP.Core.Config;
using APP.Core.Services;
using Microsoft.Maui.Devices.Sensors;

namespace APP.Platforms.Android
{
    public sealed class FlipSensorService : IFlipSensorService, IDisposable
    {
        private readonly FlipDetector _detector;
        private bool _listening;
        private readonly object _lock = new();

        public event Action? FlipDownDetected;
        public event Action? FlipUpDetected;
        public FlipOrientation CurrentOrientation => _detector.CurrentOrientation;

        public FlipSensorService()
        {
            _detector = new FlipDetector();
            _detector.FlipDownDetected += () => FlipDownDetected?.Invoke();
            _detector.FlipUpDetected += () => FlipUpDetected?.Invoke();
        }

        public void StartListening()
        {
            lock (_lock)
            {
                if (_listening) return;
                _listening = true;
                _detector.Reset();

                if (Accelerometer.Default.IsSupported)
                {
                    Accelerometer.Default.ReadingChanged += OnAccelerometerReading;
                    Accelerometer.Default.Start(SensorSpeed.UI);
                }
            }
        }

        public void StopListening()
        {
            lock (_lock)
            {
                if (!_listening) return;
                _listening = false;

                if (Accelerometer.Default.IsSupported && Accelerometer.Default.IsMonitoring)
                {
                    Accelerometer.Default.Stop();
                    Accelerometer.Default.ReadingChanged -= OnAccelerometerReading;
                }
            }
        }

        private void OnAccelerometerReading(object? sender, AccelerometerChangedEventArgs e)
        {
            // MAUI Accelerometer returns values in g (gravity units).
            // Z ≈ -1 when face-down, Z ≈ +1 when face-up.
            var z = e.Reading.Acceleration.Z;
            _detector.OnAccelerometerReading(z, DateTimeOffset.UtcNow);
        }

        public void Dispose()
        {
            StopListening();
        }
    }
}
