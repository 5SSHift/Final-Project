using System.Configuration;
using Wpf.Config;
using Wpf.Services;

namespace Wpf.ViewModels
{
    public sealed class SettingsViewModel : BaseViewModel
    {
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

        // ── Constructor ──────────────────────────────────────────────────────
        public SettingsViewModel()
        {
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
