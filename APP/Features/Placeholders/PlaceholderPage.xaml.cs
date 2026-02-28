namespace APP.Features.Placeholders
{
    [QueryProperty(nameof(PageTitle), "title")]
    public partial class PlaceholderPage : ContentPage
    {
        public string PageTitle
        {
            set
            {
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
