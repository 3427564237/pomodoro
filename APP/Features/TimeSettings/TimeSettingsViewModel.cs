using System.ComponentModel;
using System.Runtime.CompilerServices;
using APP.Core.StateMachine;

namespace APP.Features.TimeSettings
{
    public class TimeSettingsViewModel : INotifyPropertyChanged
    {
        private readonly PomodoroStateMachine _coordinator;
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

        public TimeSettingsViewModel(PomodoroStateMachine coordinator)
        {
            _coordinator = coordinator;
            LoadFromConfig();
        }

        public void LoadFromConfig()
        {
            var config = _coordinator.Config;
            // 这里直接回填字段，避免加载默认值时也走一遍“用户正在输入”的那套逻辑。
            // Fill the backing fields directly here so loading defaults does not trigger the same path as live user typing.
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
                // 先把报错写得直一点，页面上看一眼就知道该改哪个字段。
                // Keep the validation message blunt so the user can tell which field to fix at a glance.
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

        public void AdjustFocusMinutes(int delta)
            => FocusMinutesText = AdjustPositiveInt(FocusMinutesText, delta).ToString();

        public void AdjustBreakMinutes(int delta)
            => BreakMinutesText = AdjustPositiveInt(BreakMinutesText, delta).ToString();

        public void AdjustCycles(int delta)
            => CyclesText = AdjustPositiveInt(CyclesText, delta).ToString();

        public bool TrySetFocusMinutes(string value)
            => TrySetPositiveInt(value, text => FocusMinutesText = text);

        public bool TrySetBreakMinutes(string value)
            => TrySetPositiveInt(value, text => BreakMinutesText = text);

        public bool TrySetCycles(string value)
            => TrySetPositiveInt(value, text => CyclesText = text);

        private void ClearError()
        {
            if (HasError) ErrorMessage = string.Empty;
        }

        private static int AdjustPositiveInt(string value, int delta)
        {
            if (!int.TryParse(value, out var current) || current < 1)
            {
                current = 1;
            }

            if (delta > 1 && current < delta)
            {
                return delta;
            }

            return Math.Max(1, current + delta);
        }

        private static bool TrySetPositiveInt(string value, Action<string> setValue)
        {
            if (!int.TryParse(value, out var parsed) || parsed < 1)
            {
                return false;
            }

            setValue(parsed.ToString());
            return true;
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
