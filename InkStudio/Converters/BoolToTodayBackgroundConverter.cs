using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace InkStudio.Converters;

/// <summary>
/// Convierte un booleano (EsHoy) a un color de fondo para el día actual.
/// </summary>
public class BoolToTodayBackgroundConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool esHoy && esHoy)
        {
            return new SolidColorBrush(Color.Parse("#3b82f6")); // Azul para hoy
        }
        return new SolidColorBrush(Colors.Transparent);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
