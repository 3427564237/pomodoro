using APP.Core.Models;
using APP.Core.Services;

namespace APP.Core.StateMachine
{
    public class PomodoroStateMachine : IPomodoroCoordinator
    {
        private readonly ITimerEngine _timer;
        private TimerSnapshot _currentSnapshot;
        private bool _isPaused;

        public event Action<TimerSnapshot>? TimerUpdated;
        public event Action? TimerCompleted;

        public TimerSnapshot CurrentSnapshot => _currentSnapshot;
        public bool IsPaused => _isPaused;
        public bool HasActiveSession => _hasActiveSession;

        private bool _hasActiveSession;

        public PomodoroStateMachine(ITimerEngine timer)
        {
            _timer = timer;
            _timer.Tick += OnTimerTick;
            _timer.Completed += OnTimerCompleted;
        }

        public void StartTimer(TimeSpan duration)
        {
            _isPaused = false;
            _hasActiveSession = true;
            _currentSnapshot = new TimerSnapshot(duration, duration, true);
            _timer.Start(duration);
        }

        public void PauseTimer()
        {
            if (!_hasActiveSession) return;
            _timer.Pause();
            _isPaused = true;
            _currentSnapshot = new TimerSnapshot(
                _currentSnapshot.Total, _timer.Remaining, false);
            TimerUpdated?.Invoke(_currentSnapshot);
        }

        public void ResumeTimer()
        {
            if (!_hasActiveSession) return;
            _timer.Resume();
            _isPaused = false;
        }

        public void StopTimer()
        {
            _timer.Stop();
            _isPaused = false;
            _hasActiveSession = false;
            _currentSnapshot = default;
        }

        public void SkipTimer()
        {
            if (!_hasActiveSession) return;
            _isPaused = false;
            _hasActiveSession = false;
            _timer.Skip();
        }

        private void OnTimerTick(TimerSnapshot snapshot)
        {
            _currentSnapshot = snapshot;
            TimerUpdated?.Invoke(snapshot);
        }

        private void OnTimerCompleted()
        {
            _isPaused = false;
            _hasActiveSession = false;
            TimerCompleted?.Invoke();
        }
    }
}
