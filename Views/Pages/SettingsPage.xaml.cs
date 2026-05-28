using System.Windows.Controls;
using Wpf.ViewModels;

namespace Wpf.Views.Pages
{
    public partial class SettingsPage : Page
    {
        public SettingsPage(SettingsViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }
    }
}
