using System.Diagnostics;
using System.Threading.Channels;
using APP.Core.Config;
using APP.Core.Models;

namespace APP.Core.Services
{
    public class TimerEngine : ITimerEngine
    {
        private abstract class PublishItem { }

        private sealed class TickItem(long generation, TimerSnapshot snapshot) : PublishItem
        {
            public readonly long Generation = generation;
            public readonly TimerSnapshot Snapshot = snapshot;
        }

        private sealed class CompletedItem(long generation) : PublishItem
        {
            public readonly long Generation = generation;
        }

        private sealed class BarrierItem(TaskCompletionSource tcs) : PublishItem
        {
            public readonly TaskCompletionSource Tcs = tcs;
        }

        private readonly TimeSpan _tickInterval;
        private readonly object _lock = new();
        private readonly Stopwatch _stopwatch = new();
        // 所有事件都先进同一个单读通道，能把 Tick / Completed 的顺序稳定下来。
        // Push every event through one single-reader channel so Tick and Completed stay in a stable order.
        private readonly Channel<PublishItem> _channel =
            Channel.CreateUnbounded<PublishItem>(
                new UnboundedChannelOptions { SingleReader = true });

        private TimeSpan _total;
        private TimeSpan _accumulated;
        private bool _isRunning;
        private bool _hasStarted;
        private long _generation;
        private CancellationTokenSource? _tickCts;

        public event Action<TimerSnapshot>? Tick;
        public event Action? Completed;

        public TimerEngine() : this(InteractionTimings.TimerTickInterval) { }

        public TimerEngine(TimeSpan tickInterval)
        {
            _tickInterval = tickInterval;
            _ = Task.Run(PublisherLoopAsync);
        }

        public bool IsRunning
        {
            get { lock (_lock) return _isRunning; }
        }

        public TimeSpan Remaining
        {
            get { lock (_lock) return GetRemaining(); }
        }

        public void Start(TimeSpan duration)
        {
            if (duration <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(duration));

            long gen;
            CancellationTokenSource cts;
            lock (_lock)
            {
                CancelTickLoop();
                _total = duration;
                _accumulated = TimeSpan.Zero;
                _hasStarted = true;
                _isRunning = true;
                _generation++;
                gen = _generation;
                _stopwatch.Restart();
                _tickCts = new CancellationTokenSource();
                cts = _tickCts;
            }

            _ = RunTickLoopAsync(gen, cts.Token);
        }

        public void Pause()
        {
            lock (_lock)
            {
                if (!_isRunning || !_hasStarted) return;
                _accumulated += _stopwatch.Elapsed;
                _stopwatch.Stop();
                _isRunning = false;
                _generation++;
                CancelTickLoop();
            }
        }

        public void Resume()
        {
            long gen;
            CancellationTokenSource? cts = null;
            lock (_lock)
            {
                if (_isRunning || !_hasStarted) return;
                _isRunning = true;
                _generation++;
                gen = _generation;
                _stopwatch.Restart();
                _tickCts = new CancellationTokenSource();
                cts = _tickCts;
            }

            if (cts != null)
                _ = RunTickLoopAsync(gen, cts.Token);
        }

        public void Stop()
        {
            var barrier = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);
            lock (_lock)
            {
                CancelTickLoop();
                _generation++;
                ResetState();
            }
            // 等发布队列把前面的消息处理完再返回，调用方读到的状态才是一致的。
            // Wait until the publish queue drains before returning, so callers see a consistent state.
            _channel.Writer.TryWrite(new BarrierItem(barrier));
            barrier.Task.Wait();
        }

        public void Skip()
        {
            bool shouldComplete;
            TimeSpan total;
            long gen;
            var barrier = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);
            lock (_lock)
            {
                shouldComplete = _hasStarted;
                total = _total;
                CancelTickLoop();
                _generation++;
                gen = _generation;
                ResetState();
            }

            if (shouldComplete)
            {
                _channel.Writer.TryWrite(
                    new TickItem(gen, new TimerSnapshot(total, TimeSpan.Zero, false)));
                _channel.Writer.TryWrite(new CompletedItem(gen));
            }
            // Skip 也等一手，避免 UI 先跳页、后收到旧一轮的 tick。
            // Skip waits as well, so the UI does not navigate away and then receive a stale tick from the old run.
            _channel.Writer.TryWrite(new BarrierItem(barrier));
            barrier.Task.Wait();
        }

        private TimeSpan GetRemaining()
        {
            var elapsed = _accumulated + (_isRunning ? _stopwatch.Elapsed : TimeSpan.Zero);
            var remaining = _total - elapsed;
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
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

        private void CancelTickLoop()
        {
            _tickCts?.Cancel();
            _tickCts?.Dispose();
            _tickCts = null;
        }

        private async Task RunTickLoopAsync(long myGeneration, CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    await Task.Delay(_tickInterval, ct).ConfigureAwait(false);
                    if (ct.IsCancellationRequested) break;

                    lock (_lock)
                    {
                        if (_generation != myGeneration || !_isRunning)
                            return;

                        var total = _total;
                        var remaining = GetRemaining();

                        if (remaining <= TimeSpan.Zero)
                        {
                            _isRunning = false;
                            _hasStarted = false;
                            _stopwatch.Stop();
                            _stopwatch.Reset();
                            _accumulated = TimeSpan.Zero;
                            _total = TimeSpan.Zero;

                            // 到点时先补一帧 00:00，再通知 completed，界面观感会自然一点。
                            // Emit one last 00:00 frame before Completed so the UI lands more naturally.
                            _channel.Writer.TryWrite(
                                new TickItem(myGeneration,
                                    new TimerSnapshot(total, TimeSpan.Zero, false)));
                            _channel.Writer.TryWrite(
                                new CompletedItem(myGeneration));
                            return;
                        }

                        _channel.Writer.TryWrite(
                            new TickItem(myGeneration,
                                new TimerSnapshot(total, remaining, true)));
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        private async Task PublisherLoopAsync()
        {
            while (await _channel.Reader.WaitToReadAsync().ConfigureAwait(false))
            {
                while (_channel.Reader.TryRead(out var item))
                {
                    switch (item)
                    {
                        case TickItem t:
                            // generation 就是“这一轮计时的编号”，旧任务晚到的消息直接丢掉。
                            // Generation is the run id; any late message from an old run gets ignored.
                            if (t.Generation == Interlocked.Read(ref _generation))
                                Tick?.Invoke(t.Snapshot);
                            break;

                        case CompletedItem c:
                            if (c.Generation == Interlocked.Read(ref _generation))
                                Completed?.Invoke();
                            break;

                        case BarrierItem b:
                            b.Tcs.TrySetResult();
                            break;
                    }
                }
            }
        }
    }
}
