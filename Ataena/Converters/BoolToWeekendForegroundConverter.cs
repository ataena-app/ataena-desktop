using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Ataena.Converters;

/// <summary>
/// Conversor que devuelve un color de texto más claro para los fines de semana.
/// </summary>
public class BoolToWeekendForegroundConverter : IValueConverter
{
    /// <summary>
    /// Instancia estática para uso en XAML.
    /// </summary>
    public static readonly BoolToWeekendForegroundConverter Instance = new();

    /// <summary>
    /// Si es true (fin de semana), devuelve un color más claro (#cbd5e1).
    /// Si es false, devuelve el color normal (White).
    /// </summary>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool esFinDeSemana && esFinDeSemana)
        {
            return new SolidColorBrush(Color.Parse("#cbd5e1"));
        }
        return new SolidColorBrush(Colors.White);
    }

    /// <summary>
    /// No implementado (no necesario para este uso).
    /// </summary>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

