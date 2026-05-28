using System.Windows;
using Wpf.ViewModels;

namespace Wpf.Views
{
    public partial class RegisterWindow : Window
    {
        private readonly RegisterViewModel _vm;

        public RegisterWindow(RegisterViewModel vm)
        {
            InitializeComponent();
            _vm         = vm;
            DataContext = vm;
        }

        private async void BtnRegister_Click(object sender, RoutedEventArgs e)
            => await _vm.RegisterAsync(PbxRegPass.Password, PbxConfirm.Password);
    }
}
