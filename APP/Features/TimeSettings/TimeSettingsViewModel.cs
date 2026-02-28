using System.ComponentModel;
using System.Runtime.CompilerServices;
using APP.Core.StateMachine;

namespace APP.Features.TimeSettings
{
    public class TimeSettingsViewModel : INotifyPropertyChanged
    {
        private readonly IPomodoroCoordinator _coordinator;
        private string _cyclesText = "";
        private string _focusMinutesText = "";
        private string _breakMinutesText = "";
        private string _errorMessage = string.Empty;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string CyclesText
        {
            get => _cyclesText;
            set { if (_cyclesText != value) { _cyclesText = value; OnPropertyChanged(); ClearError(); } }
        }

        public string FocusMinutesText
        {
            get => _focusMinutesText;
            set { if (_focusMinutesText != value) { _focusMinutesText = value; OnPropertyChanged(); ClearError(); } }
        }

        public string BreakMinutesText
        {
            get => _breakMinutesText;
            set { if (_breakMinutesText != value) { _breakMinutesText = value; OnPropertyChanged(); ClearError(); } }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            private set
            {
                if (_errorMessage != value)
                {
                    _errorMessage = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HasError));
                }
            }
        }

        public bool HasError => !string.IsNullOrEmpty(_errorMessage);

        public TimeSettingsViewModel(IPomodoroCoordinator coordinator)
        {
            _coordinator = coordinator;
            LoadFromConfig();
        }

        public void LoadFromConfig()
        {
            var config = _coordinator.Config;
            _cyclesText = config.Cycles.ToString();
            _focusMinutesText = ((int)config.FocusDuration.TotalMinutes).ToString();
            _breakMinutesText = ((int)config.BreakDuration.TotalMinutes).ToString();
            _errorMessage = string.Empty;

            OnPropertyChanged(nameof(CyclesText));
            OnPropertyChanged(nameof(FocusMinutesText));
            OnPropertyChanged(nameof(BreakMinutesText));
            OnPropertyChanged(nameof(ErrorMessage));
            OnPropertyChanged(nameof(HasError));
        }

        public bool TrySave()
        {
            if (!int.TryParse(_cyclesText, out var cycles) || cycles < 1)
            {
                ErrorMessage = "Cycles must be at least 1";
                return false;
            }

            if (!int.TryParse(_focusMinutesText, out var focusMinutes) || focusMinutes < 1)
            {
                ErrorMessage = "Focus must be at least 1 minute";
                return false;
            }

            if (!int.TryParse(_breakMinutesText, out var breakMinutes) || breakMinutes < 1)
            {
                ErrorMessage = "Break must be at least 1 minute";
                return false;
            }

            _coordinator.UpdateConfig(
                cycles,
                TimeSpan.FromMinutes(focusMinutes),
                TimeSpan.FromMinutes(breakMinutes));

            ErrorMessage = string.Empty;
            return true;
        }

        private void ClearError()
        {
            if (HasError) ErrorMessage = string.Empty;
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
