using APP.Core.Models;

namespace APP.Core.Services
{
    // 这里抽象的是底层计时能力，不包含 pomodoro 的轮次和提示逻辑。
    // This interface abstracts raw timer behavior only; pomodoro rounds and overlays live above it.
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
