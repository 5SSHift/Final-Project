using System.Windows;
using System.Windows.Controls;
using Wpf.ViewModels;

namespace Wpf
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel     _vm;
        private readonly NavigationService _nav;

        public MainWindow(MainViewModel vm, NavigationService nav)
        {
            InitializeComponent();
            _vm  = vm;
            _nav = nav;
            DataContext = vm;
            _nav.RegisterFrame(MainFrame);

            Loaded += (_, _) => UpdateNavStyles();
            _vm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(MainViewModel.CurrentPage))
                    UpdateNavStyles();
            };

            // Actualizează iconița butonului Max/Restore la schimbarea stării
            StateChanged += (_, _) =>
            {
                BtnMaxRestore.Content  = WindowState == WindowState.Maximized ? "🗗" : "🗖";
                BtnMaxRestore.ToolTip  = WindowState == WindowState.Maximized ? "Restaurează" : "Maximizează";
            };
        }

        // ── Butoane bară de titlu ─────────────────────────────────
        private void OnMinimize(object sender, RoutedEventArgs e)
            => WindowState = WindowState.Minimized;

        private void OnMaximizeRestore(object sender, RoutedEventArgs e)
            => WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;

        private void OnClose(object sender, RoutedEventArgs e)
            => Close();

        // ── Stiluri navigare active ───────────────────────────────
        private void UpdateNavStyles()
        {
            var active   = (Style)Resources["NavBtnActive"];
            var inactive = (Style)Resources["NavBtn"];

            BtnAdminDash.Style      = _vm.CurrentPage == "AdminDashboard"   ? active : inactive;
            BtnDashboard.Style      = _vm.CurrentPage == "Dashboard"        ? active : inactive;
            BtnClientProducts.Style = _vm.CurrentPage == "ClientProducts"   ? active : inactive;
            BtnCart.Style           = _vm.CurrentPage == "Cart"             ? active : inactive;
            BtnProducts.Style       = _vm.CurrentPage == "Products"         ? active : inactive;
            BtnOrders.Style         = _vm.CurrentPage == "Orders"           ? active : inactive;
            BtnUsers.Style          = _vm.CurrentPage == "Users"            ? active : inactive;
            BtnSettings.Style       = _vm.CurrentPage == "Settings"         ? active : inactive;
        }
    }
}
