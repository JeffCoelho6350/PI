using PI.ViewModels;

namespace PI.Views
{
    public partial class CredentialDetailPage : ContentPage
    {
        public CredentialDetailPage(CredentialDetailViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}
