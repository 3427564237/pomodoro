using System.ComponentModel;
using System.Runtime.CompilerServices;
using APP.Core.StateMachine;

namespace APP.Features.Settings
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        private readonly IPomodoroCoordinator _coordinator;
        private bool _strictModeEnabled;

        public event PropertyChangedEventHandler? PropertyChanged;

        public bool StrictModeEnabled
        {
            get => _strictModeEnabled;
            set
            {
                if (_strictModeEnabled == value) return;
                _strictModeEnabled = value;
                // 这个开关没有单独的“保存”按钮，用户一切换就立刻写回流程层。
                // This toggle has no separate save step, so every change is written straight back to the flow layer.
                _coordinator.UpdateStrictMode(value);
                OnPropertyChanged();
            }
        }

        public SettingsViewModel(IPomodoroCoordinator coordinator)
        {
            _coordinator = coordinator;
            _strictModeEnabled = coordinator.Config.StrictModeEnabled;
        }

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
