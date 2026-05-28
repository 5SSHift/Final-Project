using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Wpf.Converters
{
    /// <summary>
    /// Converts boolean values to a Color or Brush for status indicators.
    /// Parameters: "TrueColor|FalseColor" (e.g., "Green|Red" or "#28A745|#DC3545")
    /// Automatically returns a Color struct when the binding target expects Color
    /// (e.g. SolidColorBrush.Color), or a SolidColorBrush when it expects a Brush
    /// (e.g. TextBlock.Foreground).
    /// </summary>
    public class BooleanToColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            if (value is not bool boolValue || parameter is not string colors)
                return GetFallback(targetType);

            var colorParts = colors.Split('|');
            if (colorParts.Length != 2)
                return GetFallback(targetType);

            var colorString = boolValue ? colorParts[0] : colorParts[1];

            try
            {
                Color color;

                if (colorString.StartsWith('#'))
                {
                    color = (Color)ColorConverter.ConvertFromString(colorString);
                }
                else
                {
                    var colorProperty = typeof(Colors).GetProperty(colorString);
                    if (colorProperty == null)
                        return GetFallback(targetType);

                    color = (Color)colorProperty.GetValue(null)!;
                }

                // Return Color struct when the target property is Color (e.g. SolidColorBrush.Color),
                // otherwise return a SolidColorBrush (e.g. for Foreground, Fill, Background).
                if (targetType == typeof(Color))
                    return color;

                return new SolidColorBrush(color);
            }
            catch
            {
                return GetFallback(targetType);
            }
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo? culture)
            => throw new NotImplementedException();

        private static object GetFallback(Type targetType)
            => targetType == typeof(Color) ? Colors.Gray : Brushes.Gray;
    }
}
