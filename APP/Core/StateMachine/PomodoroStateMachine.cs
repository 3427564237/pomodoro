using APP.Core.Config;
using APP.Core.Models;
using APP.Core.Services;

namespace APP.Core.StateMachine
{
    public class PomodoroStateMachine : IPomodoroCoordinator
    {
        private readonly ITimerEngine _timer;
        private readonly TimeSpan _overlayAutoDismiss;

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
            : this(timer, InteractionTimings.BreakPromptAutoDismiss) { }

        public PomodoroStateMachine(ITimerEngine timer, TimeSpan overlayAutoDismiss)
        {
            _timer = timer;
            _overlayAutoDismiss = overlayAutoDismiss;
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

            _config = new RuntimeConfig(cycles, focusDuration, breakDuration);
            ConfigChanged?.Invoke(_config);
        }

        public void StartFocus()
        {
            var cfg = _config;
            StartFocusInternal(cfg.Cycles, cfg.FocusDuration);
        }

        public void StartFocus(int cycles, TimeSpan focusDuration, TimeSpan breakDuration)
        {
            UpdateConfig(cycles, focusDuration, breakDuration);
            StartFocus();
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
            DismissOverlayAndProceed();
        }

        private void OnTimerTick(TimerSnapshot snapshot)
        {
            _currentSnapshot = snapshot;
            TimerUpdated?.Invoke(snapshot);
        }

        private void OnTimerCompleted()
        {
            _isPaused = false;

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
