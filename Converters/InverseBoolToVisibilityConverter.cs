using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Wpf.Converters
{
    /// <summary>
    /// Inversul lui BooleanToVisibilityConverter:
    /// true  → Collapsed
    /// false → Visible
    /// </summary>
    public class InverseBoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is true ? Visibility.Collapsed : Visibility.Visible;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is Visibility.Collapsed;
    }
}
