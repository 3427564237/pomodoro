using APP.Core.Models;

namespace APP.Core.StateMachine
{
    public interface IPomodoroCoordinator
    {
        PhaseState CurrentPhase { get; }
        OverlayState CurrentOverlay { get; }
        int CyclesRemaining { get; }
        TimerSnapshot CurrentSnapshot { get; }
        bool IsPaused { get; }
        bool HasActiveSession { get; }

        event Action<PhaseState>? PhaseChanged;
        event Action<OverlayState>? OverlayChanged;
        event Action<TimerSnapshot>? TimerUpdated;
        event Action? SessionEnded;

        void StartFocus(int cycles, TimeSpan focusDuration, TimeSpan breakDuration);
        void Stop();
        void Pause();
        void Resume();
        void Skip();
        void OverlayTapped();
    }
}
