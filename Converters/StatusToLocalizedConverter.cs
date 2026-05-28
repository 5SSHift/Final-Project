using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Wpf.Converters
{
    /// <summary>
    /// Convertește statusul intern al comenzii ("Pending", "Processing", etc.)
    /// în textul localizat din ResourceDictionary-ul activ.
    /// Cheile așteptate: OrderStatus_Pending, OrderStatus_Processing, etc.
    /// Dacă cheia lipsește, returnează statusul original netraducat.
    /// </summary>
    public class StatusToLocalizedConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            if (value is not string status || string.IsNullOrEmpty(status))
                return value ?? string.Empty;

            var key = $"OrderStatus_{status}";

            // TryFindResource parcurge MergedDictionaries, deci respectă limba curentă
            if (Application.Current.TryFindResource(key) is string translated)
                return translated;

            return status; // fallback — returnează statusul original
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo? culture)
            => throw new NotImplementedException();
    }
}
