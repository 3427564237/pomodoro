using System.Diagnostics;
using APP.Core.Config;
using APP.Core.Models;

namespace APP.Core.Services
{
    // 简单的计时器，每隔一段时间触发Tick事件，倒计时结束时触发Completed事件
    public class TimerEngine
    {
        private readonly TimeSpan _tickInterval;
        private readonly Stopwatch _stopwatch = new();

        private TimeSpan _total;
        private TimeSpan _accumulated;
        private bool _isRunning;
        private bool _hasStarted;
        private CancellationTokenSource? _cts;

        public event Action<TimerSnapshot>? Tick;
        public event Action? Completed;

        public TimerEngine() : this(InteractionTimings.TimerTickInterval) { }

        public TimerEngine(TimeSpan tickInterval)
        {
            _tickInterval = tickInterval;
        }

        public bool IsRunning => _isRunning;

        public TimeSpan Remaining
        {
            get
            {
                var elapsed = _accumulated + (_isRunning ? _stopwatch.Elapsed : TimeSpan.Zero);
                var remaining = _total - elapsed;
                return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
            }
        }

        public void Start(TimeSpan duration)
        {
            if (duration <= TimeSpan.Zero) return;

            // 停掉之前的计时
            StopTickLoop();

            _total = duration;
            _accumulated = TimeSpan.Zero;
            _hasStarted = true;
            _isRunning = true;
            _stopwatch.Restart();

            // 开始新的tick循环
            _cts = new CancellationTokenSource();
            _ = RunTickLoopAsync(_cts.Token);
        }

        public void Pause()
        {
            if (!_isRunning || !_hasStarted) return;

            _accumulated += _stopwatch.Elapsed;
            _stopwatch.Stop();
            _isRunning = false;
            StopTickLoop();
        }

        public void Resume()
        {
            if (_isRunning || !_hasStarted) return;

            _isRunning = true;
            _stopwatch.Restart();
            _cts = new CancellationTokenSource();
            _ = RunTickLoopAsync(_cts.Token);
        }

        public void Stop()
        {
            StopTickLoop();
            ResetState();
        }

        public void Skip()
        {
            if (!_hasStarted) return;

            var total = _total;
            StopTickLoop();
            ResetState();

            // 先发一个00:00的tick，再触发completed
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Tick?.Invoke(new TimerSnapshot(total, TimeSpan.Zero, false));
                Completed?.Invoke();
            });
        }

        private void ResetState()
        {
            _isRunning = false;
            _hasStarted = false;
            _stopwatch.Stop();
            _stopwatch.Reset();
            _total = TimeSpan.Zero;
            _accumulated = TimeSpan.Zero;
        }

        private void StopTickLoop()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        private async Task RunTickLoopAsync(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    await Task.Delay(_tickInterval, ct);
                    if (ct.IsCancellationRequested) break;

                    var total = _total;
                    var remaining = Remaining;

                    // 倒计时结束
                    if (remaining <= TimeSpan.Zero)
                    {
                        ResetState();

                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            Tick?.Invoke(new TimerSnapshot(total, TimeSpan.Zero, false));
                            Completed?.Invoke();
                        });
                        return;
                    }

                    // 正常tick
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        Tick?.Invoke(new TimerSnapshot(total, remaining, true));
                    });
                }
            }
            catch (OperationCanceledException)
            {
                // 正常取消，不用处理
            }
        }
    }
}
