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
        private bool _isPaused;
        private bool _areControlsEnabled;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string RemainingText
        {
            get => _remainingText;
            private set
            {
                if (_remainingText == value) return;
                _remainingText = value;
                OnPropertyChanged();
            }
        }

        public string PauseButtonText
        {
            get => _pauseButtonText;
            private set
            {
                if (_pauseButtonText == value) return;
                _pauseButtonText = value;
                OnPropertyChanged();
            }
        }

        public bool AreControlsEnabled
        {
            get => _areControlsEnabled;
            private set
            {
                if (_areControlsEnabled == value) return;
                _areControlsEnabled = value;
                OnPropertyChanged();
            }
        }

        public CountdownViewModel(IPomodoroCoordinator coordinator)
        {
            _coordinator = coordinator;
        }

        public void Activate()
        {
            _coordinator.TimerUpdated -= OnTimerUpdated;
            _coordinator.TimerCompleted -= OnTimerCompleted;
            _coordinator.TimerUpdated += OnTimerUpdated;
            _coordinator.TimerCompleted += OnTimerCompleted;

            var hasSession = _coordinator.HasActiveSession;
            AreControlsEnabled = hasSession;

            var snapshot = _coordinator.CurrentSnapshot;
            _isPaused = _coordinator.IsPaused;
            PauseButtonText = _isPaused ? "Resume" : "Pause";
            RemainingText = hasSession ? FormatTime(snapshot.Remaining) : "00:00";
        }

        public void Deactivate()
        {
            _coordinator.TimerUpdated -= OnTimerUpdated;
            _coordinator.TimerCompleted -= OnTimerCompleted;
        }

        public void TogglePause()
        {
            if (!_coordinator.HasActiveSession) return;
            if (_isPaused)
            {
                _coordinator.ResumeTimer();
                _isPaused = false;
                PauseButtonText = "Pause";
            }
            else
            {
                _coordinator.PauseTimer();
                _isPaused = true;
                PauseButtonText = "Resume";
            }
        }

        public void RequestStop()
        {
            _coordinator.StopTimer();
            AreControlsEnabled = false;
        }

        public void RequestSkip()
        {
            if (!_coordinator.HasActiveSession) return;
            _coordinator.SkipTimer();
            AreControlsEnabled = false;
        }

        private void OnTimerUpdated(TimerSnapshot snapshot)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                RemainingText = FormatTime(snapshot.Remaining);
            });
        }

        private void OnTimerCompleted()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                RemainingText = "00:00";
                PauseButtonText = "Pause";
                _isPaused = false;
                AreControlsEnabled = false;
            });
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
