using APP.Core.Config;
using APP.Core.Models;

namespace APP.Core.Services
{
    // 简单的计时器
    public class TimerEngine
    {
        private IDispatcherTimer? _tickTimer;
        private DateTime _startTime;
        private TimeSpan _totalDuration;
        private TimeSpan _pausedTime;
        private bool _isRunning;

        public event Action<TimerSnapshot>? Tick;
        public event Action? Completed;

        public bool IsRunning => _isRunning;

        public TimeSpan Remaining
        {
            get
            {
                if (!IsRunning && _pausedTime > TimeSpan.Zero)
                    return _totalDuration - _pausedTime;

                if (!IsRunning)
                    return TimeSpan.Zero;

                var elapsed = DateTime.UtcNow - _startTime + _pausedTime;
                var remaining = _totalDuration - elapsed;
                return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
            }
        }

        public void Start(TimeSpan duration)
        {
            if (duration <= TimeSpan.Zero) return;

            Stop();

            _totalDuration = duration;
            _pausedTime = TimeSpan.Zero;
            _isRunning = true;
            _startTime = DateTime.UtcNow;

            _tickTimer = Dispatcher.GetForCurrentThread().CreateTimer();
            _tickTimer.Interval = TimeSpan.FromMilliseconds(250);
            _tickTimer.Tick += OnTimerTick;
            _tickTimer.Start();
        }

        public void Pause()
        {
            if (!_isRunning) return;

            _pausedTime += DateTime.UtcNow - _startTime;
            _isRunning = false;
            _tickTimer?.Stop();
        }

        public void Resume()
        {
            if (_isRunning) return;

            _isRunning = true;
            _startTime = DateTime.UtcNow;
            _tickTimer?.Start();
        }

        public void Stop()
        {
            if (_tickTimer != null)
            {
                _tickTimer.Tick -= OnTimerTick;
                _tickTimer.Stop();
                _tickTimer = null;
            }

            _isRunning = false;
            _pausedTime = TimeSpan.Zero;
        }

        public void Skip()
        {
            if (_totalDuration <= TimeSpan.Zero) return;

            Stop();
            Tick?.Invoke(new TimerSnapshot(_totalDuration, TimeSpan.Zero, false));
            Completed?.Invoke();
        }

        private void OnTimerTick(object? sender, EventArgs e)
        {
            var remaining = Remaining;

            if (remaining <= TimeSpan.Zero)
            {
                Stop();
                Tick?.Invoke(new TimerSnapshot(_totalDuration, TimeSpan.Zero, false));
                Completed?.Invoke();
            }
            else
            {
                Tick?.Invoke(new TimerSnapshot(_totalDuration, remaining, true));
            }
        }
    }
}
