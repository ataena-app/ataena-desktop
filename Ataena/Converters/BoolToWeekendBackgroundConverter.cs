using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Ataena.Converters;

/// <summary>
/// Conversor que devuelve un color de fondo más claro para los fines de semana.
/// </summary>
public class BoolToWeekendBackgroundConverter : IValueConverter
{
    /// <summary>
    /// Instancia estática para uso en XAML.
    /// </summary>
    public static readonly BoolToWeekendBackgroundConverter Instance = new();

    /// <summary>
    /// Si es true (fin de semana), devuelve un color más claro (#2d3748).
    /// Si es false, devuelve el color normal (#111827).
    /// </summary>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool esFinDeSemana && esFinDeSemana)
        {
            return new SolidColorBrush(Color.Parse("#2d3748"));
        }
        return new SolidColorBrush(Color.Parse("#111827"));
    }

    /// <summary>
    /// No implementado (no necesario para este uso).
    /// </summary>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

