using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Data.Converters;

namespace Ataena.Converters;

/// <summary>
/// Conversor que devuelve true si alguno de los valores es true.
/// Usado para mostrar TrabajosView cuando está en la vista de Trabajos O cuando el modal está abierto.
/// </summary>
public class OrConverter : IMultiValueConverter
{
    /// <summary>
    /// Instancia estática para uso en XAML.
    /// </summary>
    public static readonly OrConverter Instance = new();

    /// <summary>
    /// Devuelve true si alguno de los valores es true.
    /// </summary>
    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values == null || values.Count == 0)
            return false;

        return values.Any(v => v is bool b && b);
    }

    /// <summary>
    /// No implementado (no necesario para este uso).
    /// </summary>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

