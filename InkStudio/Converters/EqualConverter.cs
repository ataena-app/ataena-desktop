using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace InkStudio.Converters;

/// <summary>
/// Conversor que compara un valor con un parámetro y devuelve true si son iguales.
/// Usado para la navegación entre vistas.
/// </summary>
public class EqualConverter : IValueConverter
{
    /// <summary>
    /// Instancia estática para uso en XAML.
    /// </summary>
    public static readonly EqualConverter Instance = new();

    /// <summary>
    /// Compara el valor con el parámetro.
    /// </summary>
    /// <param name="value">Valor a comparar (ej: VistaActual)</param>
    /// <param name="targetType">Tipo de destino (bool)</param>
    /// <param name="parameter">Valor a comparar (ej: "Clientes")</param>
    /// <param name="culture">Cultura</param>
    /// <returns>true si son iguales, false si no</returns>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
            return false;

        return value.ToString()?.Equals(parameter.ToString(), StringComparison.OrdinalIgnoreCase) ?? false;
    }

    /// <summary>
    /// No implementado (no necesario para este uso).
    /// </summary>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

