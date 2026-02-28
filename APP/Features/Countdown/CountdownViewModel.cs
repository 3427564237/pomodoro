using System.ComponentModel;
using System.Runtime.CompilerServices;
using APP.Core.Models;
using APP.Core.StateMachine;

namespace APP.Features.Countdown
{
    public class CountdownViewModel : INotifyPropertyChanged
    {
        private readonly IPomodoroCoordinator _coordinator;
        private string _remainingText = "00:00";
        private string _pauseButtonText = "Pause";
        private string _phaseLabel = "Focus";
        private bool _isPaused;
        private bool _areControlsEnabled;
        private bool _isOverlayVisible;
        private string _overlayText = string.Empty;

        public event PropertyChangedEventHandler? PropertyChanged;
        public event Action? NavigateToMainRequested;

        public string RemainingText
        {
            get => _remainingText;
            private set { if (_remainingText != value) { _remainingText = value; OnPropertyChanged(); } }
        }

        public string PauseButtonText
        {
            get => _pauseButtonText;
            private set { if (_pauseButtonText != value) { _pauseButtonText = value; OnPropertyChanged(); } }
        }

        public string PhaseLabel
        {
            get => _phaseLabel;
            private set { if (_phaseLabel != value) { _phaseLabel = value; OnPropertyChanged(); } }
        }

        public bool AreControlsEnabled
        {
            get => _areControlsEnabled;
            private set { if (_areControlsEnabled != value) { _areControlsEnabled = value; OnPropertyChanged(); } }
        }

        public bool IsOverlayVisible
        {
            get => _isOverlayVisible;
            private set { if (_isOverlayVisible != value) { _isOverlayVisible = value; OnPropertyChanged(); } }
        }

        public string OverlayText
        {
            get => _overlayText;
            private set { if (_overlayText != value) { _overlayText = value; OnPropertyChanged(); } }
        }

        public CountdownViewModel(IPomodoroCoordinator coordinator)
        {
            _coordinator = coordinator;
        }

        public void Activate()
        {
            _coordinator.TimerUpdated -= OnTimerUpdated;
            _coordinator.PhaseChanged -= OnPhaseChanged;
            _coordinator.OverlayChanged -= OnOverlayChanged;
            _coordinator.SessionEnded -= OnSessionEnded;

            _coordinator.TimerUpdated += OnTimerUpdated;
            _coordinator.PhaseChanged += OnPhaseChanged;
            _coordinator.OverlayChanged += OnOverlayChanged;
            _coordinator.SessionEnded += OnSessionEnded;

            SyncFromCoordinator();
        }

        public void Deactivate()
        {
            _coordinator.TimerUpdated -= OnTimerUpdated;
            _coordinator.PhaseChanged -= OnPhaseChanged;
            _coordinator.OverlayChanged -= OnOverlayChanged;
            _coordinator.SessionEnded -= OnSessionEnded;
        }

        public void TogglePause()
        {
            if (!_coordinator.HasActiveSession) return;

            if (_isPaused)
                _coordinator.Resume();
            else
                _coordinator.Pause();

            // Sync from SoT — coordinator may reject the request (e.g. during overlay)
            _isPaused = _coordinator.IsPaused;
            PauseButtonText = _isPaused ? "Resume" : "Pause";
        }

        public void RequestStop()
        {
            _coordinator.Stop();
        }

        public void RequestSkip()
        {
            if (!_coordinator.HasActiveSession) return;
            _coordinator.Skip();
        }

        public void RequestOverlayTap()
        {
            _coordinator.OverlayTapped();
        }

        private void SyncFromCoordinator()
        {
            var hasSession = _coordinator.HasActiveSession;
            var overlay = _coordinator.CurrentOverlay;

            _isPaused = _coordinator.IsPaused;
            PauseButtonText = _isPaused ? "Resume" : "Pause";
            PhaseLabel = _coordinator.CurrentPhase == PhaseState.Break ? "Break" : "Focus";
            AreControlsEnabled = hasSession && overlay == OverlayState.None;
            RemainingText = hasSession ? FormatTime(_coordinator.CurrentSnapshot.Remaining) : "00:00";
            ApplyOverlayState(overlay);
        }

        private void OnTimerUpdated(TimerSnapshot snapshot)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                RemainingText = FormatTime(snapshot.Remaining);
            });
        }

        private void OnPhaseChanged(PhaseState phase)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                PhaseLabel = phase == PhaseState.Break ? "Break" : "Focus";
                AreControlsEnabled = phase != PhaseState.Idle
                                     && _coordinator.CurrentOverlay == OverlayState.None;
            });
        }

        private void OnOverlayChanged(OverlayState overlay)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                ApplyOverlayState(overlay);
                AreControlsEnabled = _coordinator.HasActiveSession
                                     && overlay == OverlayState.None;
            });
        }

        private void OnSessionEnded()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                AreControlsEnabled = false;
                IsOverlayVisible = false;
                NavigateToMainRequested?.Invoke();
            });
        }

        private void ApplyOverlayState(OverlayState overlay)
        {
            switch (overlay)
            {
                case OverlayState.HaveABreak:
                    OverlayText = "Have a break";
                    IsOverlayVisible = true;
                    break;
                case OverlayState.YouDidIt:
                    OverlayText = "You did it";
                    IsOverlayVisible = true;
                    break;
                default:
                    IsOverlayVisible = false;
                    OverlayText = string.Empty;
                    break;
            }
        }

        private static string FormatTime(TimeSpan ts)
        {
            if (ts.TotalHours >= 1)
                return ts.ToString(@"h\:mm\:ss");
            return ts.ToString(@"mm\:ss");
        }

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
