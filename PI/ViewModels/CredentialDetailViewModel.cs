using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PI.Models;
using PI.Services;

namespace PI.ViewModels
{
    [QueryProperty(nameof(CredentialId), "CredentialId")]
    public partial class CredentialDetailViewModel : ObservableObject
    {
        private readonly ICredentialRepository _repository;
        private Credential? _existingCredential;

        [ObservableProperty]
        private int _credentialId;

        [ObservableProperty]
        private string _title = string.Empty;

        [ObservableProperty]
        private string _username = string.Empty;

        [ObservableProperty]
        private string _password = string.Empty;

        [ObservableProperty]
        private string _notes = string.Empty;

        [ObservableProperty]
        private string _pageTitle = "Nova Credencial";

        [ObservableProperty]
        private bool _isPasswordHidden = true;

        [ObservableProperty]
        private bool _isBusy;

        public CredentialDetailViewModel(ICredentialRepository repository)
        {
            _repository = repository;
        }

        async partial void OnCredentialIdChanged(int value)
        {
            if (value > 0)
            {
                PageTitle = "Editar Credencial";
                await LoadCredentialAsync(value);
            }
            else
            {
                PageTitle = "Nova Credencial";
                _existingCredential = null;
                Title = string.Empty;
                Username = string.Empty;
                Password = string.Empty;
                Notes = string.Empty;
            }
        }

        private async Task LoadCredentialAsync(int id)
        {
            try
            {
                IsBusy = true;
                _existingCredential = await _repository.GetByIdAsync(id);
                if (_existingCredential is not null)
                {
                    Title = _existingCredential.Title;
                    Username = _existingCredential.Username;
                    Password = _existingCredential.Password;
                    Notes = _existingCredential.Notes;
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private void TogglePasswordVisibility()
        {
            IsPasswordHidden = !IsPasswordHidden;
        }

        [RelayCommand]
        public async Task SaveAsync()
        {
            if (string.IsNullOrWhiteSpace(Title))
            {
                await Shell.Current.DisplayAlertAsync("Atenção", "Por favor, informe o título ou serviço.", "OK");
                return;
            }

            try
            {
                IsBusy = true;

                var credential = _existingCredential ?? new Credential();
                credential.Title = Title.Trim();
                credential.Username = Username.Trim();
                credential.Password = Password;
                credential.Notes = Notes;

                await _repository.SaveAsync(credential);
                await Shell.Current.GoToAsync("..");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task CancelAsync()
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
