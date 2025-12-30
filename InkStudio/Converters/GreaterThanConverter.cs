using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace InkStudio.Converters;

/// <summary>
/// Convierte un valor numérico a bool: true si es mayor que el parámetro especificado.
/// </summary>
public class GreaterThanConverter : IValueConverter
{
    /// <summary>
    /// Instancia que compara si el valor es mayor que 1.
    /// </summary>
    public static readonly GreaterThanConverter One = new() { Threshold = 1 };

    /// <summary>
    /// Umbral para la comparación.
    /// </summary>
    public int Threshold { get; set; } = 1;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int intValue)
        {
            return intValue > Threshold;
        }
        if (value is long longValue)
        {
            return longValue > Threshold;
        }
        if (value is double doubleValue)
        {
            return doubleValue > Threshold;
        }
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

