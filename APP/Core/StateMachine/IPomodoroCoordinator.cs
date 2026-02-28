using APP.Core.Config;
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
        RuntimeConfig Config { get; }

        event Action<PhaseState>? PhaseChanged;
        event Action<OverlayState>? OverlayChanged;
        event Action<TimerSnapshot>? TimerUpdated;
        event Action? SessionEnded;
        event Action<RuntimeConfig>? ConfigChanged;

        /// <summary>
        /// Starts a session if the coordinator is idle.
        /// 
        /// 
        bool RequestStartFocus();

        void StartFocus(int cycles, TimeSpan focusDuration, TimeSpan breakDuration);
        void Stop();
        void Pause();
        void Resume();
        void Skip();
        void OverlayTapped();
        void UpdateConfig(int cycles, TimeSpan focusDuration, TimeSpan breakDuration);
    }
}
