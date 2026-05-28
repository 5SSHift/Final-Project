using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace Wpf.Converters
{
    /// <summary>
    /// Convertește byte[]? → BitmapImage pentru binding în XAML.
    /// Returnează null dacă array-ul e null sau gol (WPF afișează nimic / fallback).
    /// </summary>
    public class BytesToImageConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not byte[] bytes || bytes.Length == 0)
                return null;

            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource       = new MemoryStream(bytes);
                bitmap.CacheOption        = BitmapCacheOption.OnLoad;
                bitmap.DecodePixelWidth   = 400; // limitează memoria — suficient pentru UI
                bitmap.EndInit();
                bitmap.Freeze();             // thread-safe, optimizare WPF
                return bitmap;
            }
            catch
            {
                return null;
            }
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
