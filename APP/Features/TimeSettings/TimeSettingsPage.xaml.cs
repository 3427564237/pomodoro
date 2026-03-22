using APP.Core.StateMachine;
using Microsoft.Extensions.DependencyInjection;

namespace APP.Features.TimeSettings
{
    public partial class TimeSettingsPage : ContentPage
    {
        private readonly PomodoroStateMachine _stateMachine;

        // 输入框绑定的属性
        public string CyclesText { get; set; } = "2";
        public string FocusMinutesText { get; set; } = "25";
        public string BreakMinutesText { get; set; } = "5";
        public string ErrorMessage { get; set; } = "";
        public bool HasError { get; set; } = false;

        public TimeSettingsPage()
            : this(MauiProgram.Services.GetRequiredService<PomodoroStateMachine>())
        {
        }

        public TimeSettingsPage(PomodoroStateMachine stateMachine)
        {
            InitializeComponent();
            BindingContext = this;
            _stateMachine = stateMachine;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadFromConfig();
        }

        private void LoadFromConfig()
        {
            var config = _stateMachine.Config;
            CyclesText = config.Cycles.ToString();
            FocusMinutesText = ((int)config.FocusDuration.TotalMinutes).ToString();
            BreakMinutesText = ((int)config.BreakDuration.TotalMinutes).ToString();
            ErrorMessage = "";
            HasError = false;

            OnPropertyChanged(nameof(CyclesText));
            OnPropertyChanged(nameof(FocusMinutesText));
            OnPropertyChanged(nameof(BreakMinutesText));
            OnPropertyChanged(nameof(ErrorMessage));
            OnPropertyChanged(nameof(HasError));
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            // 验证输入
            if (!int.TryParse(CyclesText, out int cycles) || cycles < 1)
            {
                ShowError("Cycles must be at least 1");
                return;
            }

            if (!int.TryParse(FocusMinutesText, out int focusMin) || focusMin < 1)
            {
                ShowError("Focus time must be at least 1 minute");
                return;
            }

            if (!int.TryParse(BreakMinutesText, out int breakMin) || breakMin < 1)
            {
                ShowError("Break time must be at least 1 minute");
                return;
            }

            // 保存到状态机
            _stateMachine.UpdateConfig(
                cycles,
                TimeSpan.FromMinutes(focusMin),
                TimeSpan.FromMinutes(breakMin));

            await Shell.Current.GoToAsync("..");
        }

        private void ShowError(string message)
        {
            ErrorMessage = message;
            HasError = true;
            OnPropertyChanged(nameof(ErrorMessage));
            OnPropertyChanged(nameof(HasError));
        }

        private async void OnCancelClicked(object sender, EventArgs e)
            => await Shell.Current.GoToAsync("..");
    }
}
