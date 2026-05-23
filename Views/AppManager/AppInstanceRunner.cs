using System;
using System.Threading;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Wpf.Auth;
using Wpf.Config;
using Wpf.Data.Repositories;
using Wpf.ViewModels;

namespace Wpf.Views.AppManager
{
    /// <summary>
    /// Pornește o instanță completă a aplicației (Login → Main) pe un thread STA dedicat.
    /// Fiecare instanță are propriul container DI, propriul Dispatcher și propriile ferestre —
    /// complet izolate una față de alta.
    /// </summary>
    public sealed class AppInstanceRunner
    {
        private static int _instanceCount = 0;

        public static void Launch()
        {
            var runner = new AppInstanceRunner();
            runner.StartOnNewThread();
        }

        private void StartOnNewThread()
        {
            Interlocked.Increment(ref _instanceCount);

            var thread = new Thread(() => RunInstance())
            {
                Name         = $"AppInstance-{_instanceCount}",
                IsBackground = true
            };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }

        private static void RunInstance()
        {
            var svc = new ServiceCollection();
            ConfigureServices(svc);
            var sp = svc.BuildServiceProvider();

            var dispatcher = Dispatcher.CurrentDispatcher;

            dispatcher.BeginInvoke(async () =>
            {
                try { await DatabaseConfig.InitializeDatabaseAsync(); } catch { }

                // Încearcă auto-login prin device — dar nu pentru Administrator
                if (!SessionStore.WasExplicitlyLoggedOut())
                {
                    var auth   = sp.GetRequiredService<AuthService>();
                    var device = sp.GetRequiredService<DeviceAuthService>();
                    try
                    {
                        var (deviceOk, userId, _) = await device.AuthenticateDeviceAsync();
                        if (deviceOk)
                        {
                            var (loginOk, _, token) = await auth.LoginByUserIdAsync(userId);
                            // Admin-ul trebuie să introducă mereu credențialele manual
                            if (loginOk && token?.Role != "Administrator")
                            {
                                ShowMainWindow(sp, dispatcher);
                                return;
                            }
                            // Dacă e admin, logout și cere credențiale
                            auth.Logout();
                        }
                    }
                    catch { }
                }

                ShowLoginWindow(sp, dispatcher);
            });

            Dispatcher.Run();

            Interlocked.Decrement(ref _instanceCount);
            sp.Dispose();
        }

        // ── Servicii — același set ca în App.xaml.cs ─────────────
        internal static void ConfigureServices(ServiceCollection svc)
        {
            svc.AddSingleton<TokenService>();
            svc.AddSingleton<AuthService>();
            svc.AddSingleton<DeviceAuthService>();
            svc.AddSingleton<NavigationService>();
            svc.AddSingleton<IProductRepository, ProductRepository>();
            svc.AddSingleton<IOrderRepository, OrderRepository>();
            svc.AddTransient<LoginViewModel>();
            svc.AddTransient<RegisterViewModel>();
            svc.AddSingleton<ProductsViewModel>();
            svc.AddSingleton<DashboardViewModel>();
            svc.AddSingleton<SettingsViewModel>();
            svc.AddSingleton<UsersViewModel>();
            svc.AddSingleton<OrdersViewModel>();
            svc.AddSingleton<ClientProductsViewModel>();
            svc.AddSingleton<CartViewModel>();
            svc.AddSingleton<AdminDashboardViewModel>();
            svc.AddSingleton<MainViewModel>();
        }

        // ── LOGIN ─────────────────────────────────────────────────
        private static void ShowLoginWindow(ServiceProvider sp, Dispatcher dispatcher)
        {
            var vm  = sp.GetRequiredService<LoginViewModel>();
            vm.Reset();
            var win = new LoginWindow(vm);

            win.Closed += (_, _) =>
            {
                if (!sp.GetRequiredService<AuthService>().IsLoggedIn)
                    dispatcher.BeginInvokeShutdown(DispatcherPriority.Normal);
            };

            vm.OnLoginSuccess = async () =>
            {
                var auth   = sp.GetRequiredService<AuthService>();
                var device = sp.GetRequiredService<DeviceAuthService>();

                try { await auth.FinalizeLoginAsync(); } catch { }

                SessionStore.ClearLoggedOut();
                // Nu înregistrăm device pentru admin (nu vrem auto-login la repornire)
                if (!auth.IsAdministrator)
                    try { await device.RegisterDeviceAsync(auth.CurrentUserId, Environment.MachineName); } catch { }

                win.Close();
                ShowMainWindow(sp, dispatcher);
            };

            vm.OnOpenRegister = () =>
            {
                win.Hide();
                ShowRegisterWindow(sp, dispatcher, onBack: () => { win.Show(); vm.Reset(); });
            };

            win.Show();
        }

        // ── REGISTER ──────────────────────────────────────────────
        private static void ShowRegisterWindow(
            ServiceProvider sp, Dispatcher dispatcher, Action? onBack = null)
        {
            var vm  = sp.GetRequiredService<RegisterViewModel>();
            vm.Reset();
            var win = new RegisterWindow(vm);

            vm.OnRegisterSuccess = async () =>
            {
                win.Hide();
                var auth = sp.GetRequiredService<AuthService>();
                var (ok, msg) = await auth.RegisterAsync(
                    vm.PendingUsername, vm.Email, vm.PendingPassword, vm.SelectedRole);
                if (ok) { win.Close(); onBack?.Invoke(); }
                else    { vm.ErrorMessage = msg; win.Show(); }
            };

            vm.OnOpenLogin = () => { win.Close(); onBack?.Invoke(); };
            win.Show();
        }

        // ── MAIN ──────────────────────────────────────────────────
        private static async void ShowMainWindow(ServiceProvider sp, Dispatcher dispatcher)
        {
            var vm   = sp.GetRequiredService<MainViewModel>();
            var nav  = sp.GetRequiredService<NavigationService>();
            var auth = sp.GetRequiredService<AuthService>();

            var win  = new MainWindow(vm, nav);

            win.Closed += (_, _) =>
            {
                // X pe fereastră → oprește thread-ul acestei instanțe
                if (auth.IsLoggedIn)
                {
                    auth.Logout();
                    vm.ResetSession();
                }
                dispatcher.BeginInvokeShutdown(DispatcherPriority.Normal);
            };

            vm.OnLogout = async () =>
            {
                var device = sp.GetRequiredService<DeviceAuthService>();
                var userId = auth.CurrentUserId;

                SessionStore.MarkLoggedOut();
                auth.Logout();
                vm.ResetSession();

                if (userId > 0)
                    try { await device.UnregisterDeviceAsync(userId); } catch { }

                var oldWin = win;
                ShowLoginWindow(sp, dispatcher);
                dispatcher.BeginInvoke(() => oldWin.Close(), DispatcherPriority.Background);
            };

            win.Show();
            await vm.AutoConnectAsync();
            vm.NavigateToHome();
        }
    }
}
