using APP.Core.Config;
using APP.Core.Models;
using APP.Core.Navigation;
using APP.Core.Services;

namespace APP.Core.StateMachine
{
    // 番茄钟的核心状态机，管理专注/休息等状态转换
    public class PomodoroStateMachine
    {
        private readonly TimerEngine _timer;
        private readonly Func<bool>? _isFaceUpQuery;

        private RuntimeConfig _config;
        private PhaseState _currentPhase = PhaseState.Idle;
        private OverlayState _currentOverlay = OverlayState.None;
        private int _cyclesRemaining;
        private TimerSnapshot _currentSnapshot;
        private bool _isPaused;

        private IDispatcherTimer? _overlayDismissTimer;
        private IDispatcherTimer? _faceUpGraceTimer;

        private bool _lastFlipWasUp;
        private bool _putMeDownShownSinceLastDown;

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

        public PomodoroStateMachine(TimerEngine timer, Func<bool>? isFaceUpQuery = null)
        {
            _timer = timer;
            _isFaceUpQuery = isFaceUpQuery;
            _config = new RuntimeConfig(
                Constants.DefaultCycles,
                Constants.DefaultFocusDuration,
                Constants.DefaultBreakDuration);

            _timer.Tick += OnTimerTick;
            _timer.Completed += OnTimerCompleted;
        }

        public void UpdateConfig(int cycles, TimeSpan focusDuration, TimeSpan breakDuration)
        {
            if (cycles < 1 || focusDuration <= TimeSpan.Zero || breakDuration <= TimeSpan.Zero)
                return;

            _config.Cycles = cycles;
            _config.FocusDuration = focusDuration;
            _config.BreakDuration = breakDuration;
            ConfigChanged?.Invoke(_config);
        }

        public void UpdateStrictMode(bool enabled)
        {
            _config.StrictModeEnabled = enabled;
            if (!enabled && _currentOverlay == OverlayState.PutMeDown)
                DismissPutMeDown();
            ConfigChanged?.Invoke(_config);
        }

        public void UpdateVibrationEnabled(bool enabled)
        {
            if (_config.VibrationEnabled == enabled) return;

            _config.VibrationEnabled = enabled;
            ConfigChanged?.Invoke(_config);
        }

        public bool RequestStartFocus()
        {
            if (HasActiveSession) return false;

            StartFocusInternal(_config.Cycles, _config.FocusDuration);
            try
            {
                Shell.Current.GoToAsync(Routes.Countdown);
            }
            catch { }
            return true;
        }

        public void StartFocus(int cycles, TimeSpan focusDuration, TimeSpan breakDuration)
        {
            UpdateConfig(cycles, focusDuration, breakDuration);
            StartFocusInternal(_config.Cycles, _config.FocusDuration);
        }

        private void StartFocusInternal(int cycles, TimeSpan focusDuration)
        {
            CancelAllTimers();
            _timer.Stop();

            _cyclesRemaining = cycles;
            _isPaused = false;
            _putMeDownShownSinceLastDown = false;
            _currentSnapshot = new TimerSnapshot(focusDuration, focusDuration, true);

            SetPhase(PhaseState.Focus);
            SetOverlay(OverlayState.None);
            _timer.Start(focusDuration);

            TryShowPutMeDownForCurrentOrientation();
        }

        public void Stop()
        {
            if (_currentPhase == PhaseState.Idle) return;

            CancelAllTimers();
            _timer.Stop();
            ResetSession();
            SessionEnded?.Invoke();
        }

        public void Pause()
        {
            if (_currentPhase == PhaseState.Idle || _currentOverlay != OverlayState.None) return;

            _timer.Pause();
            _isPaused = true;
            _currentSnapshot = new TimerSnapshot(_currentSnapshot.Total, _timer.Remaining, false);
            TimerUpdated?.Invoke(_currentSnapshot);
        }

        public void Resume()
        {
            if (_currentPhase == PhaseState.Idle || !_isPaused) return;

            _timer.Resume();
            _isPaused = false;
            TryShowPutMeDownForCurrentOrientation();
        }

        public void Skip()
        {
            if (_currentPhase == PhaseState.Idle || _currentOverlay != OverlayState.None) return;

            _isPaused = false;
            _timer.Skip();
        }

        public void OverlayTapped()
        {
            if (_currentOverlay == OverlayState.None) return;

            if (_currentOverlay == OverlayState.PutMeDown)
                DismissPutMeDown();
            else
                DismissOverlayAndProceed();
        }

        public void OnFlipUpDetected()
        {
            _lastFlipWasUp = true;
            TryShowPutMeDownForCurrentOrientation();
        }

        public void OnFlipDownDetected()
        {
            _lastFlipWasUp = false;
            _putMeDownShownSinceLastDown = false;

            if (_currentOverlay == OverlayState.PutMeDown)
                DismissPutMeDown();
        }

        public void PutMeDownTapped()
        {
            if (_currentOverlay == OverlayState.PutMeDown)
                DismissPutMeDown();
        }

        public void BackToFocusTapped()
        {
            if (_currentOverlay == OverlayState.BackToFocus)
                DismissOverlayAndProceed();
        }

        private void ShowPutMeDown()
        {
            _putMeDownShownSinceLastDown = true;
            CancelAllTimers();

            SetOverlay(OverlayState.PutMeDown);

            _overlayDismissTimer = Dispatcher.GetForCurrentThread().CreateTimer();
            _overlayDismissTimer.Interval = Constants.PutMeDownDisplayTime;
            _overlayDismissTimer.Tick += OnPutMeDownDismissTimeout;
            _overlayDismissTimer.Start();
        }

        private void DismissPutMeDown()
        {
            CancelDismissTimer();
            SetOverlay(OverlayState.None);
        }

        private void OnTimerTick(TimerSnapshot snapshot)
        {
            _currentSnapshot = snapshot;
            TimerUpdated?.Invoke(snapshot);
        }

        private void OnTimerCompleted()
        {
            _isPaused = false;

            if (_currentOverlay == OverlayState.PutMeDown)
            {
                CancelDismissTimer();
                SetOverlay(OverlayState.None);
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
                _timer.Start(focusDuration);
                ShowOverlayWithAutoDismiss(OverlayState.BackToFocus);

                TryShowPutMeDownForCurrentOrientation();
            }
            else
            {
                ShowOverlayWithAutoDismiss(OverlayState.YouDidIt);
            }
        }

        private void ShowOverlayWithAutoDismiss(OverlayState overlay)
        {
            CancelDismissTimer();
            SetOverlay(overlay);

            _overlayDismissTimer = Dispatcher.GetForCurrentThread().CreateTimer();
            _overlayDismissTimer.Interval = Constants.OverlayDisplayTime;
            _overlayDismissTimer.Tick += OnOverlayDismissTimeout;
            _overlayDismissTimer.Start();
        }

        private void DismissOverlayAndProceed()
        {
            var overlay = _currentOverlay;
            if (overlay == OverlayState.None) return;

            CancelDismissTimer();
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
            else if (overlay == OverlayState.BackToFocus)
            {
                StartFaceUpGraceCheck();
            }
        }

        private void StartFaceUpGraceCheck()
        {
            if (!_config.StrictModeEnabled || _currentPhase != PhaseState.Focus || _isPaused)
                return;

            CancelFaceUpGraceTimer();
            _faceUpGraceTimer = Dispatcher.GetForCurrentThread().CreateTimer();
            _faceUpGraceTimer.Interval = Constants.FaceUpGraceDelay;
            _faceUpGraceTimer.Tick += OnFaceUpGraceTimeout;
            _faceUpGraceTimer.Start();
        }

        private void TryShowPutMeDownForCurrentOrientation()
        {
            if (!_config.StrictModeEnabled || _currentPhase != PhaseState.Focus || _isPaused
                || _putMeDownShownSinceLastDown || !IsFaceUp())
                return;

            if (_currentOverlay != OverlayState.None && _currentOverlay != OverlayState.BackToFocus)
                return;

            ShowPutMeDown();
        }

        private void ResetSession()
        {
            SetOverlay(OverlayState.None);
            _cyclesRemaining = 0;
            _isPaused = false;
            _putMeDownShownSinceLastDown = false;
            _lastFlipWasUp = false;
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

        private void CancelDismissTimer()
        {
            if (_overlayDismissTimer != null)
            {
                _overlayDismissTimer.Stop();
                _overlayDismissTimer = null;
            }
        }

        private void CancelFaceUpGraceTimer()
        {
            if (_faceUpGraceTimer != null)
            {
                _faceUpGraceTimer.Stop();
                _faceUpGraceTimer = null;
            }
        }

        private void CancelAllTimers()
        {
            CancelDismissTimer();
            CancelFaceUpGraceTimer();
        }

        private bool IsFaceUp() => _isFaceUpQuery?.Invoke() ?? _lastFlipWasUp;

        private void OnPutMeDownDismissTimeout(object? sender, EventArgs e)
        {
            _overlayDismissTimer?.Stop();
            DismissPutMeDown();
        }

        private void OnOverlayDismissTimeout(object? sender, EventArgs e)
        {
            _overlayDismissTimer?.Stop();
            DismissOverlayAndProceed();
        }

        private void OnFaceUpGraceTimeout(object? sender, EventArgs e)
        {
            _faceUpGraceTimer?.Stop();

            if (IsFaceUp() && _config.StrictModeEnabled && _currentPhase == PhaseState.Focus
                && _currentOverlay == OverlayState.None && !_isPaused && !_putMeDownShownSinceLastDown)
            {
                ShowPutMeDown();
            }
        }
    }
}
