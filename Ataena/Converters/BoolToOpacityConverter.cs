using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Ataena.Converters;

/// <summary>
/// Convierte un booleano a opacidad (1.0 si true, 0.4 si false).
/// Usado para mostrar días del mes actual más visibles que los de otros meses.
/// </summary>
public class BoolToOpacityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isVisible && isVisible)
        {
            return 1.0;
        }
        return 0.4;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
