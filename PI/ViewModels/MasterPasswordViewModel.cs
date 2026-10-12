using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PI.Services;

namespace PI.ViewModels
{
    public partial class MasterPasswordViewModel : ObservableObject
    {
        private readonly ICryptoService _cryptoService;

        [ObservableProperty]
        private bool _isFirstSetup;

        [ObservableProperty]
        private string _password = string.Empty;

        [ObservableProperty]
        private string _confirmPassword = string.Empty;

        [ObservableProperty]
        private bool _isPasswordHidden = true;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotBusy))]
        private bool _isBusy;

        public bool IsNotBusy => !IsBusy;

        [ObservableProperty]
        private string _pageTitle = "Cofre Digital";

        [ObservableProperty]
        private string _subtitle = "Informe a senha-mestra para acessar suas credenciais";

        [ObservableProperty]
        private string _buttonText = "Desbloquear";

        public MasterPasswordViewModel(ICryptoService cryptoService)
        {
            _cryptoService = cryptoService;
        }

        [RelayCommand]
        public async Task CheckStatusAsync()
        {
            try
            {
                IsBusy = true;
                bool hasMaster = await _cryptoService.HasMasterPasswordAsync();

                IsFirstSetup = !hasMaster;
                Password = string.Empty;
                ConfirmPassword = string.Empty;

                if (IsFirstSetup)
                {
                    PageTitle = "Criar Senha-Mestra";
                    Subtitle = "Defina uma senha-mestra forte para proteger todo o seu cofre. Esta senha não pode ser recuperada.";
                    ButtonText = "Criar Cofre";
                }
                else
                {
                    PageTitle = "Cofre Bloqueado";
                    Subtitle = "Digite sua senha-mestra para desbloquear o cofre.";
                    ButtonText = "Desbloquear";
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
        public async Task SubmitAsync()
        {
            if (IsBusy)
                return;

            if (string.IsNullOrWhiteSpace(Password))
            {
                await Shell.Current.DisplayAlertAsync("Atenção", "Por favor, digite a senha-mestra.", "OK");
                return;
            }

            try
            {
                IsBusy = true;

                if (IsFirstSetup)
                {
                    if (Password.Length < 6)
                    {
                        await Shell.Current.DisplayAlertAsync("Atenção", "A senha-mestra deve ter pelo menos 6 caracteres.", "OK");
                        return;
                    }

                    if (Password != ConfirmPassword)
                    {
                        await Shell.Current.DisplayAlertAsync("Atenção", "As senhas não coincidem.", "OK");
                        return;
                    }

                    bool created = await _cryptoService.SetupMasterPasswordAsync(Password);
                    if (created)
                    {
                        Password = string.Empty;
                        ConfirmPassword = string.Empty;
                        await Shell.Current.GoToAsync("//CredentialListPage");
                    }
                    else
                    {
                        await Shell.Current.DisplayAlertAsync("Erro", "Não foi possível criar o cofre.", "OK");
                    }
                }
                else
                {
                    bool unlocked = await _cryptoService.UnlockAsync(Password);
                    Password = string.Empty;

                    if (unlocked)
                    {
                        await Shell.Current.GoToAsync("//CredentialListPage");
                    }
                    else
                    {
                        await Shell.Current.DisplayAlertAsync("Acesso Negado", "Senha-mestra incorreta. Tente novamente.", "OK");
                    }
                }
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
