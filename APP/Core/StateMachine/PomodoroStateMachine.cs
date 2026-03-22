using System.Diagnostics;
using APP.Core.Config;
using APP.Core.Models;
using APP.Core.Navigation;
using APP.Core.Services;

namespace APP.Core.StateMachine
{
    // 番茄钟的核心状态机，管理专注/休息/暂停等状态
    public class PomodoroStateMachine
    {
        private readonly TimerEngine _timer;
        private readonly IHapticsService? _haptics;
        private readonly Func<bool>? _isFaceUpQuery;
        private readonly TimeSpan _overlayAutoDismiss;
        private readonly TimeSpan _putMeDownAutoDismiss;
        private readonly TimeSpan _faceUpGraceDelay;

        private volatile RuntimeConfig _config;
        private PhaseState _currentPhase = PhaseState.Idle;
        private OverlayState _currentOverlay = OverlayState.None;
        private int _cyclesRemaining;
        private TimerSnapshot _currentSnapshot;
        private bool _isPaused;
        private CancellationTokenSource? _overlayDismissCts;
        private CancellationTokenSource? _faceUpGraceCts;
        // 同一个 overlay 可能被点击、自动关闭或被流程切走，这里保证只收口一次。
        // The same overlay can be dismissed by a tap, auto-timeout, or another flow change, so this guard makes sure it only closes once.
        private int _overlayDismissGuard; // 0 = open / 可关闭, 1 = claimed / 已接管
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

        public PomodoroStateMachine(TimerEngine timer)
            : this(timer, null,
                   InteractionTimings.BreakPromptAutoDismiss,
                   InteractionTimings.PutMeDownAutoDismiss)
        { }

        public PomodoroStateMachine(TimerEngine timer, IHapticsService? haptics)
            : this(timer, haptics,
                   InteractionTimings.BreakPromptAutoDismiss,
                   InteractionTimings.PutMeDownAutoDismiss)
        { }

        public PomodoroStateMachine(TimerEngine timer, IHapticsService? haptics,
                                    TimeSpan overlayAutoDismiss,
                                    TimeSpan putMeDownAutoDismiss,
                                    TimeSpan? faceUpGraceDelay = null,
                                    Func<bool>? isFaceUpQuery = null)
        {
            _timer = timer;
            _haptics = haptics;
            _isFaceUpQuery = isFaceUpQuery;
            _overlayAutoDismiss = overlayAutoDismiss;
            _putMeDownAutoDismiss = putMeDownAutoDismiss;
            _faceUpGraceDelay = faceUpGraceDelay ?? InteractionTimings.BackToFocusFaceUpGrace;
            _config = new RuntimeConfig(
                InteractionTimings.DefaultCycles,
                InteractionTimings.DefaultFocusDuration,
                InteractionTimings.DefaultBreakDuration);
            // 计时器事件只订一份，后面靠状态机自己决定这次 tick 属于哪一段流程。
            // Timer events are subscribed once here; the state machine decides which phase each tick belongs to.
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
                await Shell.Current.GoToAsync(Routes.Countdown);
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

            // 开新一轮时先把上一轮残留的异步任务清掉，避免旧回调晚到把界面又改回去。
            // Clear async leftovers from the previous run first, otherwise a late callback can flip the UI back unexpectedly.
            CancelFaceUpGraceCheck();
            CancelOverlayTimer();
            Volatile.Write(ref _overlayDismissGuard, 1);
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

            CancelFaceUpGraceCheck();
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

            CancelFaceUpGraceCheck();
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

            TryShowPutMeDownForCurrentOrientation();
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

        public void OnFlipUpDetected()
        {
            _lastFlipWasUp = true;
            TryShowPutMeDownForCurrentOrientation();
        }

        public void OnFlipDownDetected()
        {
            _lastFlipWasUp = false;
            _putMeDownShownSinceLastDown = false;

            if (_currentOverlay != OverlayState.PutMeDown) return;
            DismissPutMeDown();
        }

        public void PutMeDownTapped()
        {
            if (_currentOverlay != OverlayState.PutMeDown) return;
            DismissPutMeDown();
        }

        public void BackToFocusTapped()
        {
            if (_currentOverlay != OverlayState.BackToFocus) return;
            DismissOverlayAndProceed();
        }

        private void ShowPutMeDown()
        {
            _putMeDownShownSinceLastDown = true;
            CancelFaceUpGraceCheck();
            CancelOverlayTimer();
            // PutMeDown 和别的 overlay 会抢时机，先把“当前这次关闭权”占出来。
            // PutMeDown can race with other overlays, so claim the dismissal slot before showing it.
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
        }

        private void OnTimerTick(TimerSnapshot snapshot)
        {
            _currentSnapshot = snapshot;
            TimerUpdated?.Invoke(snapshot);
        }

        private void OnTimerCompleted()
        {
            _isPaused = false;

            // 到点时如果还停在 PutMeDown，先收干净，不然后面的 break / end overlay 会被挡住。
            // If time runs out while PutMeDown is still visible, clear it first so the next break/end overlay can show.
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
                _timer.Start(focusDuration);
                _haptics?.PlayShortBuzz();
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
            CancelOverlayTimer();
            // Have a break / You did it / Back to focus 都走同一套自动收起逻辑。
            // Have a break, You did it, and Back to focus all share the same auto-dismiss flow.
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

            // overlay 只是过渡层，真正进入下一步的动作都集中在这里接。
            // Overlays are only transition layers; the actual next-step actions all converge here.
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
            else if (overlay == OverlayState.BackToFocus)
            {
                StartFaceUpGraceCheck();
            }
        }

        private void ResetSession()
        {
            _currentOverlay = OverlayState.None;
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

        private void CancelOverlayTimer()
        {
            _overlayDismissCts?.Cancel();
            _overlayDismissCts?.Dispose();
            _overlayDismissCts = null;
        }

        private void StartFaceUpGraceCheck()
        {
            CancelFaceUpGraceCheck();
            if (!_config.StrictModeEnabled) return;
            if (_currentPhase != PhaseState.Focus) return;
            if (_isPaused) return;

            // 刚回到 focus 时留一点缓冲，不然用户翻面动作还没放稳就会马上再弹提示。
            // Give focus a small grace window when it resumes, otherwise the user can get reminded again before the phone settles.
            _faceUpGraceCts = new CancellationTokenSource();
            var ct = _faceUpGraceCts.Token;
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(_faceUpGraceDelay, ct);
                    if (ct.IsCancellationRequested) return;

                    if (IsFaceUp()
                        && _config.StrictModeEnabled
                        && _currentPhase == PhaseState.Focus
                        && _currentOverlay == OverlayState.None
                        && !_isPaused
                        && !_putMeDownShownSinceLastDown)
                    {
                        ShowPutMeDown();
                    }
                }
                catch (OperationCanceledException) { }
            });
        }

        private void CancelFaceUpGraceCheck()
        {
            _faceUpGraceCts?.Cancel();
            _faceUpGraceCts?.Dispose();
            _faceUpGraceCts = null;
        }

        private void TryShowPutMeDownForCurrentOrientation()
        {
            if (!_config.StrictModeEnabled) return;
            if (_currentPhase != PhaseState.Focus) return;
            if (_isPaused) return;
            if (_putMeDownShownSinceLastDown) return;
            if (!IsFaceUp()) return;
            // BackToFocus 这层还允许继续补 PutMeDown，因为它本质上还是在提醒回到专注状态。
            // BackToFocus still allows PutMeDown to appear, because both overlays are nudging the user back into focus.
            if (_currentOverlay != OverlayState.None && _currentOverlay != OverlayState.BackToFocus) return;

            ShowPutMeDown();
        }

        private bool IsFaceUp() => _isFaceUpQuery?.Invoke() ?? _lastFlipWasUp;
    }
}
