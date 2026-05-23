using System.Windows.Controls;
using Wpf.ViewModels;

namespace Wpf.Views.Pages
{
    public partial class OrdersPage : Page
    {
        public OrdersPage(OrdersViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
            Loaded += async (_, _) => await vm.LoadOrdersAsync();
        }
    }
}
