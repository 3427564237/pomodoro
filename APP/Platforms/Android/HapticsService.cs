using APP.Core.Config;
using APP.Core.Models;
using APP.Core.StateMachine;
using System.Diagnostics;

namespace APP.Platforms.Android
{
    public sealed class HapticsService : IDisposable
    {
        private static readonly TimeSpan StrongPulse = TimeSpan.FromMilliseconds(450);
        private static readonly TimeSpan StrongPause = TimeSpan.FromMilliseconds(250);
        private static readonly TimeSpan WeakPulse = TimeSpan.FromMilliseconds(120);
        private static readonly TimeSpan WeakPause = TimeSpan.FromMilliseconds(900);

        private readonly PomodoroStateMachine _coordinator;
        private readonly object _lock = new();

        private CancellationTokenSource? _vibrationCts;
        private bool _overlaySubscribed;

        public HapticsService(PomodoroStateMachine coordinator)
        {
            _coordinator = coordinator;
            _coordinator.ConfigChanged += OnConfigChanged;
            ApplyVibrationSetting(_coordinator.Config);
        }

        private void OnConfigChanged(RuntimeConfig config)
        {
            ApplyVibrationSetting(config);
        }

        private void ApplyVibrationSetting(RuntimeConfig config)
        {
            if (config.VibrationEnabled)
                SubscribeOverlay();
            else
                UnsubscribeOverlay();
        }

        private void SubscribeOverlay()
        {
            if (_overlaySubscribed) return;

            _overlaySubscribed = true;
            _coordinator.OverlayChanged += OnOverlayChanged;
            PlayForOverlay(_coordinator.CurrentOverlay);
        }

        private void UnsubscribeOverlay()
        {
            if (_overlaySubscribed)
            {
                _coordinator.OverlayChanged -= OnOverlayChanged;
                _overlaySubscribed = false;
            }

            StopVibration();
        }

        private void OnOverlayChanged(OverlayState overlay)
        {
            PlayForOverlay(overlay);
        }

        private void PlayForOverlay(OverlayState overlay)
        {
            StopVibration();

            if (overlay == OverlayState.PutMeDown)
            {
                StartLoop(StrongPulse, StrongPause);
                return;
            }

            if (overlay == OverlayState.HaveABreak
                || overlay == OverlayState.BackToFocus
                || overlay == OverlayState.YouDidIt)
            {
                StartLoop(WeakPulse, WeakPause);
            }
        }

        private void StartLoop(TimeSpan pulse, TimeSpan pause)
        {
            var cts = new CancellationTokenSource();
            CancellationTokenSource? previous;

            lock (_lock)
            {
                previous = _vibrationCts;
                _vibrationCts = cts;
            }

            CancelAndDispose(previous);
            _ = RunLoopAsync(pulse, pause, cts.Token);
        }

        private async Task RunLoopAsync(TimeSpan pulse, TimeSpan pause, CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        Vibration.Default.Vibrate(pulse);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[HapticsService] Vibrate failed: {ex}");
                    }

                    await Task.Delay(pulse + pause, token);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        private void StopVibration()
        {
            CancellationTokenSource? cts;

            lock (_lock)
            {
                cts = _vibrationCts;
                _vibrationCts = null;
            }

            CancelAndDispose(cts);

            try
            {
                Vibration.Default.Cancel();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[HapticsService] Cancel failed: {ex}");
            }
        }

        private static void CancelAndDispose(CancellationTokenSource? cts)
        {
            if (cts == null) return;

            try
            {
                cts.Cancel();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[HapticsService] Cancel token failed: {ex}");
            }

            cts.Dispose();
        }

        public void Dispose()
        {
            UnsubscribeOverlay();
            _coordinator.ConfigChanged -= OnConfigChanged;
        }
    }
}
