using APP.Core.Models;

namespace APP.Core.Services
{
    public interface ITimerEngine
    {
        event Action<TimerSnapshot>? Tick;
        event Action? Completed;
        void Start(TimeSpan duration);
        void Pause();
        void Resume();
        void Stop();
        void Skip();
        bool IsRunning { get; }
        TimeSpan Remaining { get; }
    }
}
