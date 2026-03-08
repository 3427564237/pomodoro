namespace APP.Features.Placeholders
{
    [QueryProperty(nameof(PageTitle), "title")]
    public partial class PlaceholderPage : ContentPage
    {
        public string PageTitle
        {
            set
            {
                // Shell 参数一进来，同时更新导航栏标题和页内文案，省得两处状态分开维护。
                // When the Shell query value arrives, update both the nav bar title and the in-page label so they stay in sync.
                Title = value;
                titleLabel.Text = value;
            }
        }

        public PlaceholderPage(PlaceholderViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}
