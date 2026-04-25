using System.ComponentModel;
using System.Runtime.CompilerServices;
using APP.Core.Config;
using APP.Core.StateMachine;
using Microsoft.Maui.Graphics;

namespace APP.Features.Settings
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        private readonly PomodoroStateMachine _coordinator;
        private bool _strictModeEnabled;
        private bool _vibrationEnabled;
        private bool _keepScreenOnEnabled;
        private FlipTheme _theme;
        private FlipTheme? _pressedTheme;

        public event PropertyChangedEventHandler? PropertyChanged;
        public Color TropicalDotColor => ThemeService.GetPalette(FlipTheme.TropicalSunrise).FocusPrimary;
        public Color VioletDotColor => ThemeService.GetPalette(FlipTheme.Violet).FocusPrimary;
        public Color TropicalOptionBackground => IsTropicalSelected
            ? ThemeService.GetPalette(FlipTheme.TropicalSunrise).FocusSoft
            : IsTropicalPressed
                ? CurrentPalette.FocusSoft
            : Color.FromArgb("#F7F7F7");
        public Color VioletOptionBackground => IsVioletSelected
            ? ThemeService.GetPalette(FlipTheme.Violet).FocusSoft
            : IsVioletPressed
                ? CurrentPalette.FocusSoft
            : Color.FromArgb("#F7F7F7");
        public Color TropicalOptionStroke => IsTropicalSelected
            ? ThemeService.GetPalette(FlipTheme.TropicalSunrise).FocusPrimary
            : IsTropicalPressed
                ? CurrentPalette.FocusPrimary
            : Color.FromArgb("#E1E1E1");
        public Color VioletOptionStroke => IsVioletSelected
            ? ThemeService.GetPalette(FlipTheme.Violet).FocusPrimary
            : IsVioletPressed
                ? CurrentPalette.FocusPrimary
            : Color.FromArgb("#E1E1E1");
        public Color TropicalOptionTextColor => IsTropicalSelected
            ? ThemeService.GetPalette(FlipTheme.TropicalSunrise).FocusPrimary
            : IsTropicalPressed
                ? CurrentPalette.FocusPrimary
            : Color.FromArgb("#6E6E6E");
        public Color VioletOptionTextColor => IsVioletSelected
            ? ThemeService.GetPalette(FlipTheme.Violet).FocusPrimary
            : IsVioletPressed
                ? CurrentPalette.FocusPrimary
            : Color.FromArgb("#6E6E6E");
        private FlipThemePalette CurrentPalette => ThemeService.GetPalette(_theme);
        private bool IsTropicalSelected => _theme == FlipTheme.TropicalSunrise;
        private bool IsVioletSelected => _theme == FlipTheme.Violet;
        private bool IsTropicalPressed => _pressedTheme == FlipTheme.TropicalSunrise;
        private bool IsVioletPressed => _pressedTheme == FlipTheme.Violet;

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

        public bool VibrationEnabled
        {
            get => _vibrationEnabled;
            set
            {
                if (_vibrationEnabled == value) return;
                _vibrationEnabled = value;
                _coordinator.UpdateVibrationEnabled(value);
                OnPropertyChanged();
            }
        }

        public bool KeepScreenOnEnabled
        {
            get => _keepScreenOnEnabled;
            set
            {
                if (_keepScreenOnEnabled == value) return;
                _keepScreenOnEnabled = value;
                _coordinator.UpdateKeepScreenOnEnabled(value);
                OnPropertyChanged();
            }
        }

        public SettingsViewModel(PomodoroStateMachine coordinator)
        {
            _coordinator = coordinator;
            _strictModeEnabled = coordinator.Config.StrictModeEnabled;
            _vibrationEnabled = coordinator.Config.VibrationEnabled;
            _keepScreenOnEnabled = coordinator.Config.KeepScreenOnEnabled;
            _theme = coordinator.Config.Theme;
        }

        public void Activate()
        {
            _coordinator.ConfigChanged -= OnConfigChanged;
            _coordinator.ConfigChanged += OnConfigChanged;
            SyncFromConfig(_coordinator.Config);
        }

        public void Deactivate()
        {
            _coordinator.ConfigChanged -= OnConfigChanged;
            SetPressedTheme(null);
        }

        public void SelectTheme(FlipTheme theme)
        {
            if (_theme != theme)
            {
                _theme = theme;
                NotifyThemeOptionProperties();
            }

            _coordinator.UpdateTheme(theme);
            NotifyThemeOptionProperties();
        }

        public void SetPressedTheme(FlipTheme? theme)
        {
            if (_pressedTheme == theme) return;

            _pressedTheme = theme;
            NotifyThemeOptionProperties();
        }

        private void OnConfigChanged(RuntimeConfig config)
        {
            MainThread.BeginInvokeOnMainThread(() => SyncFromConfig(config));
        }

        private void SyncFromConfig(RuntimeConfig config)
        {
            if (_strictModeEnabled != config.StrictModeEnabled)
            {
                _strictModeEnabled = config.StrictModeEnabled;
                OnPropertyChanged(nameof(StrictModeEnabled));
            }

            if (_vibrationEnabled != config.VibrationEnabled)
            {
                _vibrationEnabled = config.VibrationEnabled;
                OnPropertyChanged(nameof(VibrationEnabled));
            }

            if (_keepScreenOnEnabled != config.KeepScreenOnEnabled)
            {
                _keepScreenOnEnabled = config.KeepScreenOnEnabled;
                OnPropertyChanged(nameof(KeepScreenOnEnabled));
            }

            if (_theme != config.Theme)
                _theme = config.Theme;

            NotifyThemeOptionProperties();
        }

        private void NotifyThemeOptionProperties()
        {
            OnPropertyChanged(nameof(TropicalOptionBackground));
            OnPropertyChanged(nameof(VioletOptionBackground));
            OnPropertyChanged(nameof(TropicalOptionStroke));
            OnPropertyChanged(nameof(VioletOptionStroke));
            OnPropertyChanged(nameof(TropicalOptionTextColor));
            OnPropertyChanged(nameof(VioletOptionTextColor));
        }

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
