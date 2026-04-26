using Microsoft.Extensions.DependencyInjection;

namespace APP.Features.TimeSettings
{
    public partial class TimeSettingsPage : ContentPage
    {
        private const int MinuteStep = 5;

        private readonly TimeSettingsViewModel _viewModel;

        public TimeSettingsPage()
            : this(MauiProgram.Services.GetRequiredService<TimeSettingsViewModel>())
        {
        }

        public TimeSettingsPage(TimeSettingsViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
            _viewModel = viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _viewModel.LoadFromConfig();
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            if (!_viewModel.TrySave())
                return;

            await Shell.Current.GoToAsync("..");
        }

        private void OnDecreaseFocusClicked(object sender, EventArgs e)
            => _viewModel.AdjustFocusMinutes(-MinuteStep);

        private void OnIncreaseFocusClicked(object sender, EventArgs e)
            => _viewModel.AdjustFocusMinutes(MinuteStep);

        private void OnDecreaseBreakClicked(object sender, EventArgs e)
            => _viewModel.AdjustBreakMinutes(-MinuteStep);

        private void OnIncreaseBreakClicked(object sender, EventArgs e)
            => _viewModel.AdjustBreakMinutes(MinuteStep);

        private void OnDecreaseCyclesClicked(object sender, EventArgs e)
            => _viewModel.AdjustCycles(-1);

        private void OnIncreaseCyclesClicked(object sender, EventArgs e)
            => _viewModel.AdjustCycles(1);

        private async void OnFocusValueTapped(object sender, TappedEventArgs e)
            => await PromptForPositiveIntAsync(
                "Focus",
                "Minutes",
                _viewModel.FocusMinutesText,
                _viewModel.TrySetFocusMinutes);

        private async void OnBreakValueTapped(object sender, TappedEventArgs e)
            => await PromptForPositiveIntAsync(
                "Break",
                "Minutes",
                _viewModel.BreakMinutesText,
                _viewModel.TrySetBreakMinutes);

        private async void OnCyclesValueTapped(object sender, TappedEventArgs e)
            => await PromptForPositiveIntAsync(
                "Cycles",
                "Rounds",
                _viewModel.CyclesText,
                _viewModel.TrySetCycles);

        private async Task PromptForPositiveIntAsync(
            string title,
            string message,
            string initialValue,
            Func<string, bool> applyValue)
        {
            var value = await DisplayPromptAsync(
                title,
                message,
                "Save",
                "Cancel",
                keyboard: Keyboard.Numeric,
                initialValue: initialValue);

            if (value is null)
            {
                return;
            }

            if (!applyValue(value.Trim()))
            {
                await DisplayAlertAsync(title, "Enter a number greater than 0.", "OK");
            }
        }

        private async void OnCancelClicked(object sender, EventArgs e)
            => await Shell.Current.GoToAsync("..");
    }
}
