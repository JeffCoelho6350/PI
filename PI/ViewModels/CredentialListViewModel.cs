using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PI.Models;
using PI.Services;

namespace PI.ViewModels
{
    public partial class CredentialListViewModel : ObservableObject
    {
        private readonly ICredentialRepository _repository;
        private readonly ICryptoService _cryptoService;

        [ObservableProperty]
        private ObservableCollection<Credential> _credentials = new();

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private bool _isEmpty;

        public CredentialListViewModel(ICredentialRepository repository, ICryptoService cryptoService)
        {
            _repository = repository;
            _cryptoService = cryptoService;
        }

        [RelayCommand]
        public async Task LoadCredentialsAsync()
        {
            if (!_cryptoService.IsUnlocked)
            {
                await Shell.Current.GoToAsync("//MasterPasswordPage");
                return;
            }

            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                var list = await _repository.GetAllAsync();

                Credentials.Clear();
                foreach (var item in list)
                {
                    Credentials.Add(item);
                }

                IsEmpty = Credentials.Count == 0;
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task LockVaultAsync()
        {
            _cryptoService.Lock();
            Credentials.Clear();
            await Shell.Current.GoToAsync("//MasterPasswordPage");
        }

        [RelayCommand]
        public async Task AddNewCredentialAsync()
        {
            await Shell.Current.GoToAsync("CredentialDetailPage");
        }

        [RelayCommand]
        public async Task EditCredentialAsync(Credential? credential)
        {
            if (credential is null)
                return;

            var parameters = new Dictionary<string, object>
            {
                { "CredentialId", credential.Id }
            };

            await Shell.Current.GoToAsync("CredentialDetailPage", parameters);
        }

        [RelayCommand]
        public async Task DeleteCredentialAsync(Credential? credential)
        {
            if (credential is null)
                return;

            bool confirm = await Shell.Current.DisplayAlertAsync(
                "Confirmar Exclusão",
                $"Deseja realmente excluir a credencial '{credential.Title}'?",
                "Sim",
                "Não");

            if (confirm)
            {
                await _repository.DeleteAsync(credential);
                Credentials.Remove(credential);
                IsEmpty = Credentials.Count == 0;
            }
        }
    }
}
