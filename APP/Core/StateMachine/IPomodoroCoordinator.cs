using APP.Core.Config;
using APP.Core.Models;

namespace APP.Core.StateMachine
{
    // 这是 UI 层唯一该依赖的会话入口，页面只提请求，真正的状态收口都在 coordinator 里。
    // This is the single session entry point the UI should depend on; pages make requests, and the coordinator owns the real state transitions.
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

        bool RequestStartFocus();

        void StartFocus(int cycles, TimeSpan focusDuration, TimeSpan breakDuration);
        void Stop();
        void Pause();
        void Resume();
        void Skip();
        void OverlayTapped();
        void UpdateConfig(int cycles, TimeSpan focusDuration, TimeSpan breakDuration);

        void UpdateStrictMode(bool enabled);

        void OnFlipUpDetected();

        void OnFlipDownDetected();

        void PutMeDownTapped();

        void BackToFocusTapped();
    }
}
