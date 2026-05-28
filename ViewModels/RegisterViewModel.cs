using Wpf.Auth;
using Wpf.Models;

namespace Wpf.ViewModels
{
    public sealed class RegisterViewModel : BaseViewModel
    {
        // ── Model ────────────────────────────────────────────────
        private RegisterModel _model = new();

        public string Username
        {
            get => _model.Username;
            set { _model.Username = value; OnPropertyChanged(); }
        }

        public string Email
        {
            get => _model.Email;
            set { _model.Email = value; OnPropertyChanged(); }
        }

        public string SelectedRole
        {
            get => _model.Role;
            set { _model.Role = value; OnPropertyChanged(); }
        }

        public string PendingUsername
        {
            get => Username;
        }

        public string PendingPassword
        {
            get => _model.Password;
        }

        public string[] AvailableRoles => _model.AvailableRoles;

        // ── Stare UI ─────────────────────────────────────────────
        private string _errorMessage = string.Empty;
        public string ErrorMessage
        {
            get => _errorMessage;
            set { SetProperty(ref _errorMessage, value); OnPropertyChanged(nameof(HasError)); }
        }
        public bool HasError => !string.IsNullOrEmpty(_errorMessage);

        private string _successMessage = string.Empty;
        public string SuccessMessage
        {
            get => _successMessage;
            set { SetProperty(ref _successMessage, value); OnPropertyChanged(nameof(HasSuccess)); }
        }
        public bool HasSuccess => !string.IsNullOrEmpty(_successMessage);

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        // ── Callbacks spre App.xaml.cs ───────────────────────────
        public Action? OnRegisterSuccess { get; set; }
        public Action? OnOpenLogin       { get; set; }

        // ── Comenzi ──────────────────────────────────────────────
        public RelayCommand OpenLoginCommand { get; }

        public RegisterViewModel()
        {
            OpenLoginCommand = new RelayCommand(_ => OnOpenLogin?.Invoke());
        }

        // ── Register (parolele vin din PasswordBox în code-behind) ─
        public async Task RegisterAsync(string password, string confirmPassword)
        {
            ErrorMessage   = string.Empty;
            SuccessMessage = string.Empty;

            _model.Password        = password;
            _model.ConfirmPassword = confirmPassword;

            if (!_model.IsValid(out var validationError))
            { ErrorMessage = validationError; return; }

            IsLoading = true;
            try
            {
                // Create account directly without OTP verification
                OnRegisterSuccess?.Invoke();
                SuccessMessage = "Înregistrare reușită!";
            }
            finally { IsLoading = false; }
        }

        public void Reset()
        {
            _model         = new RegisterModel();
            ErrorMessage   = string.Empty;
            SuccessMessage = string.Empty;
            OnPropertyChanged(nameof(Username));
            OnPropertyChanged(nameof(Email));
            OnPropertyChanged(nameof(SelectedRole));
        }
    }
}
