using System.Windows.Controls;
using Wpf.ViewModels;

namespace Wpf.Views.Pages
{
    public partial class DashboardPage : Page
    {
        private readonly DashboardViewModel _vm;
        public DashboardPage(DashboardViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            DataContext = vm;
            Loaded += async (_, _) => await _vm.LoadStatsAsync();
        }
    }
}
