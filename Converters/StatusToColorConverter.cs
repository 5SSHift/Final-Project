using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Wpf.Converters
{
    /// <summary>
    /// Convertește statusul unei comenzi ("Pending", "Processing", etc.)
    /// într-un SolidColorBrush folosit pentru badge-urile colorate.
    /// </summary>
    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            var color = (value as string) switch
            {
                "Pending"    => "#F59E0B",   // amber
                "Processing" => "#2563EB",   // blue
                "Shipped"    => "#7C3AED",   // violet
                "Delivered"  => "#16A34A",   // green
                "Cancelled"  => "#DC2626",   // red
                _            => "#6B7280"    // gray fallback
            };

            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo? culture)
            => throw new NotImplementedException();
    }
}
