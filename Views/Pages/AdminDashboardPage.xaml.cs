using System;
using System.Windows;
using System.Windows.Controls;
using Wpf.ViewModels;

namespace Wpf.Views.Pages
{
    public partial class AdminDashboardPage : Page
    {
        private readonly AdminDashboardViewModel _vm;

        public AdminDashboardPage(AdminDashboardViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            DataContext = vm;

            // Golim PasswordBox-urile când VM-ul resetează formularul
            _vm.UserFormClosed += ClearPasswordBoxes;

            // Folosim Loaded cu un bloc try-catch dedicat pentru a preveni crash-ul fatal async-void
            Loaded += AdminDashboardPage_Loaded;
        }

        private async void AdminDashboardPage_Loaded(object sender, RoutedEventArgs e)
        {
            // Dezactivăm abonarea temporară pentru a nu rula logica de mai multe ori la re-navigare
            Loaded -= AdminDashboardPage_Loaded;

            try
            {
                await _vm.InitializeAsync();
            }
            catch (Exception ex)
            {
                // Prindem eroarea local și o trimitem spre interfață/sistemul de status
                _vm.StatusMessage = $"Eroare la inițializarea paginii de administrare: {ex.Message}";

                // Opțional, poți lăsa o notificare vizuală controlată:
                MessageBox.Show($"Nu s-au putut încărca datele administrative:\n{ex.Message}",
                                "Eroare Încărcare Date", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Tab_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
                _vm.ActiveTab = btn.Tag?.ToString() ?? "Products";
        }

        private void DataGrid_MouseDoubleClick(object sender,
            System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_vm.SelectedProduct != null)
                _vm.LoadProductFormFromSelection();
        }

        private void DataGrid_Users_MouseDoubleClick(object sender,
            System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_vm.SelectedUser != null)
                _vm.LoadUserFormFromSelection();
        }

        // ── PasswordBox handlers ─────────────────────────────────
        // PasswordBox nu suportă binding direct — preluăm valoarea din code-behind.

        private void PbxUserPassword_Changed(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox pb)
                _vm.UserPassword = pb.Password;
        }

        private void PbxUserPasswordConfirm_Changed(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox pb)
                _vm.UserPasswordConfirm = pb.Password;
        }

        // Golește câmpurile PasswordBox când VM resetează formularul
        private void ClearPasswordBoxes()
        {
            if (PbxUserPassword != null)        PbxUserPassword.Password = string.Empty;
            if (PbxUserPasswordConfirm != null) PbxUserPasswordConfirm.Password = string.Empty;
        }
    }
}