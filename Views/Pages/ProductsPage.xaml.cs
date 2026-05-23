using System.Windows.Controls;
using Wpf.ViewModels;

namespace Wpf.Views.Pages
{
    public partial class ProductsPage : Page
    {
        public ProductsPage(ProductsViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
            Loaded += async (_, _) => await vm.LoadAsync();
        }
    }
}
