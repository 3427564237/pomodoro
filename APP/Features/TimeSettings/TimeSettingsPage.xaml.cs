using APP.Core.Navigation;

namespace APP.Features.TimeSettings
{
    public partial class TimeSettingsPage : ContentPage
    {
        private readonly IAppNavigator _navigator;

        public TimeSettingsPage(TimeSettingsViewModel viewModel, IAppNavigator navigator)
        {
            InitializeComponent();
            BindingContext = viewModel;
            _navigator = navigator;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            // 每次进设置页都从当前配置重载，避免上次没保存的输入留在表单里。
            // Reload from the current config every time the page appears so abandoned edits do not linger in the form.
            ((TimeSettingsViewModel)BindingContext).LoadFromConfig();
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            var vm = (TimeSettingsViewModel)BindingContext;
            // 只有校验通过才返回，错误就留在当前页直接给用户看。
            // Only navigate back after validation passes; otherwise stay here and show the error in place.
            if (vm.TrySave())
                await _navigator.GoBackAsync();
        }

        private async void OnCancelClicked(object sender, EventArgs e)
            => await _navigator.GoBackAsync();
    }
}
