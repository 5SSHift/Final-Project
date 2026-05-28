using System.Windows.Controls;

namespace Wpf.ViewModels
{
    /// <summary>
    /// Serviciu singleton de navigare între pagini WPF.
    /// MainWindow înregistrează Frame-ul; ViewModele apelează NavigateTo().
    /// </summary>
    public sealed class NavigationService
    {
        private Frame? _frame;

        public void RegisterFrame(Frame frame) => _frame = frame;

        public void NavigateTo(Page page)
        {
            if (_frame is null) throw new InvalidOperationException("Frame neînregistrat.");
            _frame.Navigate(page);
        }

        public void GoBack()
        {
            if (_frame?.CanGoBack == true)
                _frame.GoBack();
        }
    }
}
