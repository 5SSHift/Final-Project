using System;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Wpf.Auth;
using Wpf.Config;
using Wpf.Data.Repositories;
using Wpf.Services;
using Wpf.ViewModels;
using Wpf.Views;

namespace Wpf.Views.AppManager
{
    public partial class App : Application
    {
        private readonly ServiceProvider _sp;
        private MainWindow? _mainWindow;

        public App()
        {
            this.DispatcherUnhandledException += (sender, e) =>
            {
                MessageBox.Show($"A apărut o eroare critică de sistem (UI):\n{e.Exception.Message}",
                                "Eroare neprevăzută", MessageBoxButton.OK, MessageBoxImage.Error);
                e.Handled = true;
            };

            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Eroare asincronă de fundal (Task):\n{e.Exception.InnerException?.Message ?? e.Exception.Message}",
                                    "Eroare Background Task", MessageBoxButton.OK, MessageBoxImage.Warning);
                });
                e.SetObserved();
            };

            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                if (e.ExceptionObject is Exception ex)
                {
                    MessageBox.Show($"Eroare fatală în aplicație:\n{ex.Message}\nAplicația se va închide.",
                                    "Eroare Fatală", MessageBoxButton.OK, MessageBoxImage.Stop);
                }
            };

            var svc = new ServiceCollection();
            AppInstanceRunner.ConfigureServices(svc);
            _sp = svc.BuildServiceProvider();

            ShutdownMode = ShutdownMode.OnExplicitShutdown;
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Restore persisted language before any window is shown
            LanguageService.Initialize();

            try { await DatabaseConfig.InitializeDatabaseAsync(); } catch { }

            if (SessionStore.WasExplicitlyLoggedOut())
            {
                ShowLoginWindow();
                return;
            }

            // Încearcă auto-login prin device — dar nu pentru Administrator
            var auth   = _sp.GetRequiredService<AuthService>();
            var device = _sp.GetRequiredService<DeviceAuthService>();
            try
            {
                var (deviceOk, userId, _) = await device.AuthenticateDeviceAsync();
                if (deviceOk)
                {
                    // Verifică preferința ÎNAINTE de auto-login
                    if (LoginPreferenceService.RequiresManualLogin(userId))
                    {
                        auth.Logout();
                        ShowLoginWindow();
                        return;
                    }

                    var (loginOk, _, token) = await auth.LoginByUserIdAsync(userId);
                    // Admin-ul trebuie să introducă mereu credențialele manual
                    if (loginOk && token?.Role != "Administrator")
                    {
                        ShowMain();
                        return;
                    }
                    // Dacă e admin, logout și cere credențiale
                    auth.Logout();
                }
            }
            catch { }

            ShowLoginWindow();
        }

        // ── LOGIN ─────────────────────────────────────────────────
        private void ShowLoginWindow()
        {
            var vm  = _sp.GetRequiredService<LoginViewModel>();
            vm.Reset();
            var win = new LoginWindow(vm);
            MainWindow = win;

            win.Closed += (_, _) =>
            {
                if (!_sp.GetRequiredService<AuthService>().IsLoggedIn && _mainWindow == null)
                    Shutdown();
            };

            vm.OnLoginSuccess = async () =>
            {
                var auth   = _sp.GetRequiredService<AuthService>();
                var device = _sp.GetRequiredService<DeviceAuthService>();

                try { await auth.FinalizeLoginAsync(); } catch { }

                SessionStore.ClearLoggedOut();
                // Nu înregistrăm device pentru admin
                if (!auth.IsAdministrator)
                    try { await device.RegisterDeviceAsync(auth.CurrentUserId, Environment.MachineName); } catch { }

                win.Close();
                ShowMain();
            };

            vm.OnOpenRegister = () =>
            {
                win.Hide();
                ShowRegisterWindow(onBack: () => { win.Show(); vm.Reset(); });
            };

            win.Show();
        }

        // ── REGISTER ──────────────────────────────────────────────
        private void ShowRegisterWindow(Action? onBack = null)
        {
            var vm  = _sp.GetRequiredService<RegisterViewModel>();
            vm.Reset();
            var win = new RegisterWindow(vm);

            vm.OnRegisterSuccess = async () =>
            {
                win.Hide();
                var auth = _sp.GetRequiredService<AuthService>();
                var (ok, msg) = await auth.RegisterAsync(
                    vm.PendingUsername, vm.Email, vm.PendingPassword, vm.SelectedRole);
                if (ok) { win.Close(); onBack?.Invoke(); }
                else    { vm.ErrorMessage = msg; win.Show(); }
            };

            vm.OnOpenLogin = () => { win.Close(); onBack?.Invoke(); };
            win.Show();
        }

        // ── MAIN ──────────────────────────────────────────────────
        private async void ShowMain()
        {
            var vm   = _sp.GetRequiredService<MainViewModel>();
            var nav  = _sp.GetRequiredService<NavigationService>();
            var auth = _sp.GetRequiredService<AuthService>();

            _mainWindow?.Close();
            _mainWindow = new MainWindow(vm, nav);
            MainWindow  = _mainWindow;

            _mainWindow.Closed += (_, _) =>
            {
                // Dacă _mainWindow e null înseamnă că am închis-o noi din logout — nu oprim aplicația
                if (_mainWindow == null) return;

                // X pe fereastră → închide aplicația complet
                if (auth.IsLoggedIn)
                {
                    auth.Logout();
                    vm.ResetSession();
                }
                Shutdown();
            };

            vm.OnLogout = async () =>
            {
                var device = _sp.GetRequiredService<DeviceAuthService>();
                var userId = auth.CurrentUserId;

                SessionStore.MarkLoggedOut();
                auth.Logout();
                vm.ResetSession();

                if (userId > 0)
                    try { await device.UnregisterDeviceAsync(userId); } catch { }

                var old = _mainWindow;
                _mainWindow = null;
                ShowLoginWindow();
                old?.Close();
            };

            _mainWindow.Show();
            await vm.AutoConnectAsync();
            vm.NavigateToHome();
        }
    }
}
