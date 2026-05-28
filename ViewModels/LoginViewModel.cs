using System.Windows;
using Wpf.Auth;
using Wpf.Models;

namespace Wpf.ViewModels
{
    public sealed class LoginViewModel : BaseViewModel
    {
        private readonly AuthService _auth;

        // ── Model ────────────────────────────────────────────────
        private LoginModel _model = new();

        public string Username
        {
            get => _model.Username;
            set { _model.Username = value; OnPropertyChanged(); }
        }

        // ── Stare UI ─────────────────────────────────────────────
        private string _errorMessage = string.Empty;
        public string ErrorMessage
        {
            get => _errorMessage;
            set { SetProperty(ref _errorMessage, value); OnPropertyChanged(nameof(HasError)); }
        }
        public bool HasError => !string.IsNullOrEmpty(_errorMessage);

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        // ── Callbacks spre App.xaml.cs ───────────────────────────
        public Action?  OnLoginSuccess    { get; set; }
        public Action?  OnOpenRegister    { get; set; }

        // ── Comenzi ──────────────────────────────────────────────
        public RelayCommand OpenRegisterCommand { get; }

        public LoginViewModel(AuthService auth)
        {
            _auth = auth;
            OpenRegisterCommand = new RelayCommand(_ => OnOpenRegister?.Invoke());
        }

        // ── Login (parola vine din PasswordBox în code-behind) ───
        public async Task LoginAsync(string password)
        {
            ErrorMessage = string.Empty;
            _model.Password = password;

            if (string.IsNullOrWhiteSpace(_model.Username))
            { ErrorMessage = "Introdu username-ul."; return; }

            if (string.IsNullOrWhiteSpace(password))
            { ErrorMessage = "Introdu parola."; return; }

            IsLoading = true;
            try
            {
                // Trim() previne erori de tip space accidental
                var (ok, msg, _) = await _auth.LoginAsync(_model.Username.Trim(), password);

                if (ok)
                {
                    // Verificăm dacă callback-ul există înainte de invocare
                    if (OnLoginSuccess != null)
                    {
                        // Folosim BeginInvoke pentru a lăsa UI-ul să termine task-urile curente
                        Application.Current.Dispatcher.BeginInvoke(OnLoginSuccess);
                    }
                }
                else
                {
                    ErrorMessage = msg;
                }
            }
            catch (Exception ex)
            {
                // Aici prindem erorile de bază de date/rețea
                ErrorMessage = "Eroare de conexiune la server.";
                // Debug.WriteLine(ex.Message); // Recomandat pentru logging
            }
            finally
            {
                IsLoading = false;
            }
        }

        public void Reset()
        {
            _model        = new LoginModel();
            ErrorMessage  = string.Empty;
            OnPropertyChanged(nameof(Username));
        }
    }
}
