using System.Windows.Controls;
using Wpf.ViewModels;

namespace Wpf.Views.Pages
{
    public partial class CartPage : Page
    {
        public CartPage(CartViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }
    }
}
