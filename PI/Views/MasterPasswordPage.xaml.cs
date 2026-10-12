using PI.ViewModels;

namespace PI.Views
{
    public partial class MasterPasswordPage : ContentPage
    {
        private readonly MasterPasswordViewModel _viewModel;

        public MasterPasswordPage(MasterPasswordViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.CheckStatusCommand.ExecuteAsync(null);
        }
    }
}
