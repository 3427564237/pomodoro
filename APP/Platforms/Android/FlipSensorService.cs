using APP.Core.Config;
using APP.Core.Services;
using Microsoft.Maui.Devices.Sensors;
using System.Diagnostics;

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
            // 平台服务只负责接线，真正的翻面判定细节都交给 FlipDetector。
            // This platform service mostly wires things together; FlipDetector owns the actual orientation heuristics.
            _detector.FlipDownDetected += () => FlipDownDetected?.Invoke();
            _detector.FlipUpDetected += () => FlipUpDetected?.Invoke();
        }

        public void StartListening()
        {
            lock (_lock)
            {
                if (_listening) return;
                _listening = true;
                // 每次重新开始监听都重置一次状态，避免把上个页面留下的朝向记忆带进来。
                // Reset the detector on every start so orientation state from a previous page does not leak into the new one.
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
            try
            {
                var z = e.Reading.Acceleration.Z;
                _detector.OnAccelerometerReading(z, DateTimeOffset.UtcNow);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FlipSensorService] {ex}");
            }
        }

        public void Dispose()
        {
            StopListening();
        }
    }
}
