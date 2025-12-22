using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace InkStudio.Converters;

/// <summary>
/// Conversor que convierte un booleano a texto según un parámetro con formato "TrueText|FalseText".
/// </summary>
public class BoolToTextConverter : IValueConverter
{
    /// <summary>
    /// Instancia estática para uso en XAML.
    /// </summary>
    public static readonly BoolToTextConverter Instance = new();

    /// <summary>
    /// Convierte un booleano a texto.
    /// </summary>
    /// <param name="value">Valor booleano a convertir</param>
    /// <param name="targetType">Tipo de destino (string)</param>
    /// <param name="parameter">Formato "TrueText|FalseText"</param>
    /// <param name="culture">Cultura</param>
    /// <returns>Texto correspondiente al valor booleano</returns>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not bool boolValue)
            return string.Empty;

        if (parameter is not string param || string.IsNullOrEmpty(param))
            return boolValue ? "Sí" : "No";

        var partes = param.Split('|');
        if (partes.Length == 2)
        {
            return boolValue ? partes[0] : partes[1];
        }

        return boolValue ? "Sí" : "No";
    }

    /// <summary>
    /// No implementado (no necesario para este uso).
    /// </summary>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

