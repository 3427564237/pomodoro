namespace APP.Features.Settings
{
    public partial class SettingsPage : ContentPage
    {
        public SettingsPage(SettingsViewModel viewModel)
        {
            InitializeComponent();
            // 这页现在只是轻量设置面板，直接绑一个简单 view model 就够了。
            // This page is just a light settings panel for now, so a simple bound view model is enough.
            BindingContext = viewModel;
        }
    }
}
