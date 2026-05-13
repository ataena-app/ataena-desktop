using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Ataena.Converters;

/// <summary>
/// Conversor que transforma un booleano en un color (Brush).
/// Usado para mostrar indicadores visuales basados en valores booleanos.
/// </summary>
/// <remarks>
/// - true: Verde (#4CAF50)
/// - false: Naranja/Rojo (#ff9800)
/// </remarks>
public class BoolToBrushConverter : IValueConverter
{
    /// <summary>
    /// Instancia estática para uso en XAML.
    /// </summary>
    public static readonly BoolToBrushConverter Instance = new();

    /// <summary>
    /// Convierte un booleano a su color correspondiente.
    /// </summary>
    /// <param name="value">Valor booleano.</param>
    /// <param name="targetType">Tipo de destino (Brush).</param>
    /// <param name="parameter">Parámetro adicional (no usado).</param>
    /// <param name="culture">Cultura actual.</param>
    /// <returns>SolidColorBrush con el color correspondiente.</returns>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue
                ? new SolidColorBrush(Color.Parse("#2d5a2d"))  // Verde oscuro para éxito
                : new SolidColorBrush(Color.Parse("#5a2d2d")); // Rojo oscuro para advertencia
        }
        return new SolidColorBrush(Color.Parse("#3a3a4e")); // Default: Gris oscuro
    }

    /// <summary>
    /// No implementado (no necesario para este uso).
    /// </summary>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

