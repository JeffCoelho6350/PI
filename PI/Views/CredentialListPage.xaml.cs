using PI.ViewModels;

namespace PI.Views
{
    public partial class CredentialListPage : ContentPage
    {
        private readonly CredentialListViewModel _viewModel;

        public CredentialListPage(CredentialListViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.LoadCredentialsCommand.ExecuteAsync(null);
        }
    }
}
