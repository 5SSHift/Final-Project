using System;
using System.Windows;
using Wpf.ViewModels;

namespace Wpf.Views
{
    public partial class LoginWindow : Window
    {
        private readonly LoginViewModel _vm;

        public LoginWindow(LoginViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            DataContext = _vm;

            // Opțional: Te poți abona la un eveniment din VM pentru a închide fereastra la succes
            _vm.OnLoginSuccess += () => this.Close();
        }

        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Acum 'btnLogin' va fi recunoscut pentru că i-am pus x:Name în XAML
                btnLogin.IsEnabled = false;

                // Verifică dacă PbxPassword există în XAML cu x:Name="PbxPassword"
                if (PbxPassword != null)
                {
                    await _vm.LoginAsync(PbxPassword.Password);
                }
            }
            catch (Exception ex)
            {
                // Aici vei vedea eroarea reală care provoacă crash-ul (ex: conexiunea la DB)
                MessageBox.Show($"Eroare la logare: {ex.Message}", "Eroare Critică",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Ne asigurăm că butonul se reactivează dacă nu s-a schimbat pagina
                if (btnLogin != null)
                    btnLogin.IsEnabled = true;
            }
        }
    }
}