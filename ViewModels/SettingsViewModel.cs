using System.Configuration;
using Wpf.Config;
using Wpf.Services;
using Wpf.Auth;
namespace Wpf.ViewModels
{
    public sealed class SettingsViewModel : BaseViewModel
    {
        private readonly AuthService? _auth;

        // ── Database ─────────────────────────────────────────────────────────
        private string _connectionInfo = "—";
        public string ConnectionInfo { get => _connectionInfo; set => SetProperty(ref _connectionInfo, value); }

        private string _statusMessage = string.Empty;
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

        private bool _isLoading;
        public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }

        public RelayCommand TestConnectionCommand { get; }

        // ── Language ─────────────────────────────────────────────────────────
        public bool IsRomanian => LanguageService.CurrentLanguage == "ro-RO";
        public bool IsEnglish  => LanguageService.CurrentLanguage == "en-US";

        public RelayCommand SetLanguageROCommand { get; }
        public RelayCommand SetLanguageENCommand { get; }

        // ── Login preference (Employee / Client only) ─────────────────────────
        /// <summary>
        /// Vizibil doar pentru Employee și Client — Admin-ul se autentifică mereu manual.
        /// </summary>
        public bool ShowLoginPreference =>
            _auth != null && (_auth.IsEmployee || _auth.IsClient);

        /// <summary>
        /// True  = aplicația cere parola la fiecare pornire.
        /// False = aplicația se deschide direct (auto-login prin amprentă dispozitiv).
        /// </summary>
        public bool RequireManualLogin
        {
            get => _auth != null && LoginPreferenceService.RequiresManualLogin(_auth.CurrentUserId);
            set
            {
                if (_auth == null) return;
                LoginPreferenceService.SetRequireManualLogin(_auth.CurrentUserId, value);
                OnPropertyChanged();
                OnPropertyChanged(nameof(AutoLogin));
            }
        }

        /// <summary>
        /// Inversul lui RequireManualLogin — folosit pentru binding pe RadioButton-ul "auto-login".
        /// </summary>
        public bool AutoLogin
        {
            get => !RequireManualLogin;
            set
            {
                // Setarea AutoLogin=true înseamnă RequireManualLogin=false
                RequireManualLogin = !value;
            }
        }

        // ── Constructor ──────────────────────────────────────────────────────
        public SettingsViewModel() : this(null) { }

        public SettingsViewModel(AuthService? auth)
        {
            _auth = auth;

            TestConnectionCommand = new RelayCommand(_ => TestAsync(), _ => !IsLoading);

            SetLanguageROCommand = new RelayCommand(_ =>
            {
                LanguageService.SwitchLanguage("ro-RO");
                RefreshLanguageProperties();
            });

            SetLanguageENCommand = new RelayCommand(_ =>
            {
                LanguageService.SwitchLanguage("en-US");
                RefreshLanguageProperties();
            });

            try
            {
                var cs = ConfigurationManager.ConnectionStrings["DefaultConnection"]?.ConnectionString ?? "—";
                var b  = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(cs);
                if (!string.IsNullOrEmpty(b.Password)) b.Password = "***";
                ConnectionInfo = b.ConnectionString;
            }
            catch { ConnectionInfo = "Nu s-a putut citi App.config"; }
        }

        // ── Helpers ──────────────────────────────────────────────────────────
        private void RefreshLanguageProperties()
        {
            OnPropertyChanged(nameof(IsRomanian));
            OnPropertyChanged(nameof(IsEnglish));
        }

        private async void TestAsync()
        {
            IsLoading     = true;
            StatusMessage = "Se testează...";
            try
            {
                var (ok, msg, _) = await DatabaseConfig.TestConnectionAsync();
                StatusMessage = ok ? $"✓ {msg}" : msg;
            }
            finally { IsLoading = false; }
        }
    }
}
