using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Wpf.Converters
{
    /// <summary>Returns true/Visible when int/decimal > 0.</summary>
    public class GreaterThanZeroConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var result = value switch
            {
                int i => i > 0,
                decimal d => d > 0,
                _ => int.TryParse(value?.ToString(), out var p) && p > 0
            };
            return targetType == typeof(Visibility)
                ? (result ? Visibility.Visible : Visibility.Collapsed)
                : (object)result;
        }
        public object ConvertBack(object? v, Type t, object? p, CultureInfo c)
            => throw new NotImplementedException();
    }
}

    