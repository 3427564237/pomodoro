using APP.Core.Models;

namespace APP.Core.StateMachine
{
    public interface IPomodoroCoordinator
    {
        void StartTimer(TimeSpan duration);
        void PauseTimer();
        void ResumeTimer();
        void StopTimer();
        void SkipTimer();

        event Action<TimerSnapshot>? TimerUpdated;
        event Action? TimerCompleted;

        TimerSnapshot CurrentSnapshot { get; }
        bool IsPaused { get; }
        bool HasActiveSession { get; }
    }
}
