using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Wpf.Converters
{
    /// <summary>Returns Visibility.Visible when value == parameter string.</summary>
    public class StringEqualsConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var match = string.Equals(value?.ToString(), parameter?.ToString(),
                                      StringComparison.OrdinalIgnoreCase);
            return targetType == typeof(bool)
                ? (object)match
                : match ? Visibility.Visible : Visibility.Collapsed;
        }
        public object ConvertBack(object? v, Type t, object? p, CultureInfo c) => p ?? string.Empty;
    }

    /// <summary>Returns true/Visible when int/decimal > 0.</summary>

    /// <summary>Returns Visibility.Visible when string is non-empty/non-null.</summary>
    public class StringNotEmptyConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => !string.IsNullOrEmpty(value?.ToString()) ? Visibility.Visible : Visibility.Collapsed;
        public object ConvertBack(object? v, Type t, object? p, CultureInfo c)
            => throw new NotImplementedException();
    }
}
