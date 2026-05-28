using System;
using System.Globalization;
using System.Windows.Data;

namespace Wpf.Converters
{
    public class EqualityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // Verificăm siguranța datelor de intrare
            if (values == null || values.Length < 2 || values[0] == null || values[1] == null)
                return false;

            // Comparăm categoria butonului cu categoria selectată în ViewModel
            return values[0].ToString() == values[1].ToString();
        }

        // AICI era eroarea: trebuie să fie Type[] targetTypes (cu paranteze pătrate)
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}