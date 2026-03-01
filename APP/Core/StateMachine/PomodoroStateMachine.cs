using System.Diagnostics;
using APP.Core.Config;
using APP.Core.Models;
using APP.Core.Navigation;
using APP.Core.Services;

namespace APP.Core.StateMachine
{
    public class PomodoroStateMachine : IPomodoroCoordinator
    {
        private readonly ITimerEngine _timer;
        private readonly IAppNavigator? _navigator;
        private readonly IHapticsService? _haptics;
        private readonly TimeSpan _overlayAutoDismiss;
        private readonly TimeSpan _putMeDownAutoDismiss;

        private volatile RuntimeConfig _config;
        private PhaseState _currentPhase = PhaseState.Idle;
        private OverlayState _currentOverlay = OverlayState.None;
        private int _cyclesRemaining;
        private TimerSnapshot _currentSnapshot;
        private bool _isPaused;
        private CancellationTokenSource? _overlayDismissCts;
        private int _overlayDismissGuard; // 0 = open, 1 = claimed (Interlocked)

        public event Action<PhaseState>? PhaseChanged;
        public event Action<OverlayState>? OverlayChanged;
        public event Action<TimerSnapshot>? TimerUpdated;
        public event Action? SessionEnded;
        public event Action<RuntimeConfig>? ConfigChanged;

        public PhaseState CurrentPhase => _currentPhase;
        public OverlayState CurrentOverlay => _currentOverlay;
        public int CyclesRemaining => _cyclesRemaining;
        public TimerSnapshot CurrentSnapshot => _currentSnapshot;
        public bool IsPaused => _isPaused;
        public bool HasActiveSession => _currentPhase != PhaseState.Idle;
        public RuntimeConfig Config => _config;

        public PomodoroStateMachine(ITimerEngine timer)
            : this(timer, null, null,
                   InteractionTimings.BreakPromptAutoDismiss,
                   InteractionTimings.PutMeDownAutoDismiss)
        { }

        public PomodoroStateMachine(ITimerEngine timer, TimeSpan overlayAutoDismiss)
            : this(timer, null, null, overlayAutoDismiss,
                   InteractionTimings.PutMeDownAutoDismiss)
        { }

        public PomodoroStateMachine(ITimerEngine timer, IAppNavigator? navigator)
            : this(timer, navigator, null,
                   InteractionTimings.BreakPromptAutoDismiss,
                   InteractionTimings.PutMeDownAutoDismiss)
        { }

        public PomodoroStateMachine(ITimerEngine timer, IAppNavigator? navigator,
                                    TimeSpan overlayAutoDismiss)
            : this(timer, navigator, null, overlayAutoDismiss,
                   InteractionTimings.PutMeDownAutoDismiss)
        { }

        public PomodoroStateMachine(ITimerEngine timer, TimeSpan overlayAutoDismiss,
                                    TimeSpan putMeDownAutoDismiss)
            : this(timer, null, null, overlayAutoDismiss, putMeDownAutoDismiss) { }

        public PomodoroStateMachine(ITimerEngine timer, IAppNavigator? navigator,
                                    IHapticsService? haptics)
            : this(timer, navigator, haptics,
                   InteractionTimings.BreakPromptAutoDismiss,
                   InteractionTimings.PutMeDownAutoDismiss)
        { }

        public PomodoroStateMachine(ITimerEngine timer, IAppNavigator? navigator,
                                    IHapticsService? haptics,
                                    TimeSpan overlayAutoDismiss,
                                    TimeSpan putMeDownAutoDismiss)
        {
            _timer = timer;
            _navigator = navigator;
            _haptics = haptics;
            _overlayAutoDismiss = overlayAutoDismiss;
            _putMeDownAutoDismiss = putMeDownAutoDismiss;
            _config = new RuntimeConfig(
                InteractionTimings.DefaultCycles,
                InteractionTimings.DefaultFocusDuration,
                InteractionTimings.DefaultBreakDuration);
            _timer.Tick += OnTimerTick;
            _timer.Completed += OnTimerCompleted;
        }

        public void UpdateConfig(int cycles, TimeSpan focusDuration, TimeSpan breakDuration)
        {
            if (cycles < 1)
                throw new ArgumentOutOfRangeException(nameof(cycles));
            if (focusDuration <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(focusDuration));
            if (breakDuration <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(breakDuration));

            _config = _config with { Cycles = cycles, FocusDuration = focusDuration, BreakDuration = breakDuration };
            ConfigChanged?.Invoke(_config);
        }

        public void UpdateStrictMode(bool enabled)
        {
            var prev = _config.StrictModeEnabled;
            _config = _config with { StrictModeEnabled = enabled };
            if (prev != enabled)
            {
                // 
                if (!enabled && _currentOverlay == OverlayState.PutMeDown)
                    DismissPutMeDown();

                ConfigChanged?.Invoke(_config);
            }
        }

        public bool RequestStartFocus()
        {
            if (HasActiveSession) return false;

            var cfg = _config;
            StartFocusInternal(cfg.Cycles, cfg.FocusDuration);
            _ = NavigateToCountdownAsync();
            return true;
        }

        private async Task NavigateToCountdownAsync()
        {
            try
            {
                if (_navigator != null)
                    await _navigator.GoToCountdownAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PomodoroStateMachine] Navigation to Countdown failed: {ex.Message}");
            }
        }

        public void StartFocus(int cycles, TimeSpan focusDuration, TimeSpan breakDuration)
        {
            UpdateConfig(cycles, focusDuration, breakDuration);
            StartFocusInternal(_config.Cycles, _config.FocusDuration);
        }

        private void StartFocusInternal(int cycles, TimeSpan focusDuration)
        {
            if (cycles < 1)
                throw new ArgumentOutOfRangeException(nameof(cycles));

            CancelOverlayTimer();
            Volatile.Write(ref _overlayDismissGuard, 1);
            _timer.Stop();

            _cyclesRemaining = cycles;
            _isPaused = false;
            _currentSnapshot = new TimerSnapshot(focusDuration, focusDuration, true);

            SetPhase(PhaseState.Focus);
            SetOverlay(OverlayState.None);
            _timer.Start(focusDuration);
        }

        public void Stop()
        {
            if (_currentPhase == PhaseState.Idle) return;

            CancelOverlayTimer();
            Volatile.Write(ref _overlayDismissGuard, 1);
            _timer.Stop();
            _haptics?.StopVibration();
            ResetSession();
            SessionEnded?.Invoke();
        }

        public void Pause()
        {
            if (_currentPhase == PhaseState.Idle) return;
            if (_currentOverlay != OverlayState.None) return;

            _timer.Pause();
            _isPaused = true;
            _currentSnapshot = new TimerSnapshot(
                _currentSnapshot.Total, _timer.Remaining, false);
            TimerUpdated?.Invoke(_currentSnapshot);
        }

        public void Resume()
        {
            if (_currentPhase == PhaseState.Idle) return;
            if (!_isPaused) return;

            _timer.Resume();
            _isPaused = false;
        }

        public void Skip()
        {
            if (_currentPhase == PhaseState.Idle) return;
            if (_currentOverlay != OverlayState.None) return;

            _isPaused = false;
            _timer.Skip();
        }

        public void OverlayTapped()
        {
            if (_currentOverlay == OverlayState.None) return;

            if (_currentOverlay == OverlayState.PutMeDown)
            {
                DismissPutMeDown();
                return;
            }

            DismissOverlayAndProceed();
        }

        // ── PutMeDown flip events ───────────────────────────────

        public void OnFlipUpDetected()
        {
            if (!_config.StrictModeEnabled) return;
            if (_currentPhase != PhaseState.Focus && _currentPhase != PhaseState.Break) return;
            if (_currentOverlay != OverlayState.None) return;

            ShowPutMeDown();
        }

        public void OnFlipDownDetected()
        {
            if (_currentOverlay != OverlayState.PutMeDown) return;
            DismissPutMeDown();
        }

        public void PutMeDownTapped()
        {
            if (_currentOverlay != OverlayState.PutMeDown) return;
            DismissPutMeDown();
        }

        private void ShowPutMeDown()
        {
            CancelOverlayTimer();
            Volatile.Write(ref _overlayDismissGuard, 0);
            SetOverlay(OverlayState.PutMeDown);
            _haptics?.StartContinuousVibration();

            _overlayDismissCts = new CancellationTokenSource();
            var ct = _overlayDismissCts.Token;
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(_putMeDownAutoDismiss, ct);
                    if (!ct.IsCancellationRequested)
                        DismissPutMeDown();
                }
                catch (OperationCanceledException) { }
            });
        }

        private void DismissPutMeDown()
        {
            if (Interlocked.CompareExchange(ref _overlayDismissGuard, 1, 0) != 0)
                return;

            CancelOverlayTimer();
            _haptics?.StopVibration();
            SetOverlay(OverlayState.None);
            // Timer never paused — no resume needed
        }

        // ── Timer callbacks ─────────────────────────────────────

        private void OnTimerTick(TimerSnapshot snapshot)
        {
            _currentSnapshot = snapshot;
            TimerUpdated?.Invoke(snapshot);
        }

        private void OnTimerCompleted()
        {
            _isPaused = false;

            // If PutMeDown was showing when timer expires, dismiss it first
            if (_currentOverlay == OverlayState.PutMeDown)
            {
                CancelOverlayTimer();
                Volatile.Write(ref _overlayDismissGuard, 1);
                _haptics?.StopVibration();
                _currentOverlay = OverlayState.None;
                OverlayChanged?.Invoke(OverlayState.None);
            }

            if (_currentPhase == PhaseState.Focus)
                OnFocusCompleted();
            else if (_currentPhase == PhaseState.Break)
                OnBreakCompleted();
        }

        private void OnFocusCompleted()
        {
            if (_cyclesRemaining > 1)
                ShowOverlayWithAutoDismiss(OverlayState.HaveABreak);
            else
                ShowOverlayWithAutoDismiss(OverlayState.YouDidIt);
        }

        private void OnBreakCompleted()
        {
            _cyclesRemaining--;

            if (_cyclesRemaining > 0)
            {
                var focusDuration = _config.FocusDuration;
                _currentSnapshot = new TimerSnapshot(focusDuration, focusDuration, true);
                SetPhase(PhaseState.Focus);
                SetOverlay(OverlayState.None);
                _timer.Start(focusDuration);
            }
            else
            {
                ShowOverlayWithAutoDismiss(OverlayState.YouDidIt);
            }
        }

        private void ShowOverlayWithAutoDismiss(OverlayState overlay)
        {
            CancelOverlayTimer();
            Volatile.Write(ref _overlayDismissGuard, 0);
            SetOverlay(overlay);

            _overlayDismissCts = new CancellationTokenSource();
            var ct = _overlayDismissCts.Token;
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(_overlayAutoDismiss, ct);
                    if (!ct.IsCancellationRequested)
                        DismissOverlayAndProceed();
                }
                catch (OperationCanceledException) { }
            });
        }

        private void DismissOverlayAndProceed()
        {
            if (Interlocked.CompareExchange(ref _overlayDismissGuard, 1, 0) != 0)
                return;

            var overlay = _currentOverlay;
            if (overlay == OverlayState.None) return;

            CancelOverlayTimer();
            SetOverlay(OverlayState.None);

            if (overlay == OverlayState.HaveABreak)
            {
                var breakDuration = _config.BreakDuration;
                _currentSnapshot = new TimerSnapshot(breakDuration, breakDuration, true);
                SetPhase(PhaseState.Break);
                _timer.Start(breakDuration);
            }
            else if (overlay == OverlayState.YouDidIt)
            {
                ResetSession();
                SessionEnded?.Invoke();
            }
        }

        private void ResetSession()
        {
            _currentOverlay = OverlayState.None;
            _cyclesRemaining = 0;
            _isPaused = false;
            _currentSnapshot = default;

            SetPhase(PhaseState.Idle);
        }

        private void SetPhase(PhaseState phase)
        {
            if (_currentPhase == phase) return;
            _currentPhase = phase;
            PhaseChanged?.Invoke(phase);
        }

        private void SetOverlay(OverlayState overlay)
        {
            if (_currentOverlay == overlay) return;
            _currentOverlay = overlay;
            OverlayChanged?.Invoke(overlay);
        }

        private void CancelOverlayTimer()
        {
            _overlayDismissCts?.Cancel();
            _overlayDismissCts?.Dispose();
            _overlayDismissCts = null;
        }
    }
}
