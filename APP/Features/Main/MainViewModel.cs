using System.ComponentModel;
using System.Runtime.CompilerServices;
using APP.Core.Config;
using APP.Core.StateMachine;

namespace APP.Features.Main
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly PomodoroStateMachine _coordinator;
        private string _timerDisplay = string.Empty;
        private string _configSummary = string.Empty;
        private bool _isStartButtonVisible;
        private string _startHintText = "Face down to start";

        public event PropertyChangedEventHandler? PropertyChanged;

        public string TimerDisplay
        {
            get => _timerDisplay;
            private set { if (_timerDisplay != value) { _timerDisplay = value; OnPropertyChanged(); } }
        }

        public string ConfigSummary
        {
            get => _configSummary;
            private set { if (_configSummary != value) { _configSummary = value; OnPropertyChanged(); } }
        }

        public bool IsStartButtonVisible
        {
            get => _isStartButtonVisible;
            private set { if (_isStartButtonVisible != value) { _isStartButtonVisible = value; OnPropertyChanged(); } }
        }

        public string StartHintText
        {
            get => _startHintText;
            private set { if (_startHintText != value) { _startHintText = value; OnPropertyChanged(); } }
        }

        public MainViewModel(PomodoroStateMachine coordinator)
        {
            _coordinator = coordinator;
            RefreshFromConfig();
        }

        public void Activate()
        {
            _coordinator.ConfigChanged -= OnConfigChanged;
            _coordinator.ConfigChanged += OnConfigChanged;
            RefreshFromConfig();
        }

        public void Deactivate()
        {
            _coordinator.ConfigChanged -= OnConfigChanged;
        }

        private void OnConfigChanged(RuntimeConfig _)
        {
            MainThread.BeginInvokeOnMainThread(RefreshFromConfig);
        }

        private void RefreshFromConfig()
        {
            var config = _coordinator.Config;
            var totalMinutes = (int)config.FocusDuration.TotalMinutes;
            // The home screen only shows the focus length for one round to keep the entry view light.
            TimerDisplay = $"{totalMinutes:D2}:00";
            ConfigSummary = config.Cycles == 1
                ? $"1 cycle · {(int)config.BreakDuration.TotalMinutes} min break"
                : $"{config.Cycles} cycles · {(int)config.BreakDuration.TotalMinutes} min break";
            IsStartButtonVisible = !config.StrictModeEnabled;
            StartHintText = config.StrictModeEnabled
                ? "Face down to start"
                : "Face down or tap Start";
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
