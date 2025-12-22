using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Input;

namespace InkStudio.Converters;

/// <summary>
/// Conversor que convierte un booleano a un cursor según un parámetro con formato "TrueCursor|FalseCursor".
/// </summary>
public class BoolToCursorConverter : IValueConverter
{
    /// <summary>
    /// Instancia estática para uso en XAML.
    /// </summary>
    public static readonly BoolToCursorConverter Instance = new();

    /// <summary>
    /// Convierte un booleano a un cursor.
    /// </summary>
    /// <param name="value">Valor booleano a convertir</param>
    /// <param name="targetType">Tipo de destino (Cursor)</param>
    /// <param name="parameter">Formato "TrueCursor|FalseCursor" (ej: "Ibeam|Arrow")</param>
    /// <param name="culture">Cultura</param>
    /// <returns>Cursor correspondiente al valor booleano</returns>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not bool boolValue)
            return new Cursor(StandardCursorType.Arrow);

        if (parameter is not string param || string.IsNullOrEmpty(param))
            return new Cursor(StandardCursorType.Arrow);

        var partes = param.Split('|');
        if (partes.Length == 2)
        {
            var cursorName = boolValue ? partes[0] : partes[1];
            return cursorName switch
            {
                "Ibeam" => new Cursor(StandardCursorType.Ibeam),
                "Arrow" => new Cursor(StandardCursorType.Arrow),
                "Hand" => new Cursor(StandardCursorType.Hand),
                "Wait" => new Cursor(StandardCursorType.Wait),
                "Cross" => new Cursor(StandardCursorType.Cross),
                "SizeAll" => new Cursor(StandardCursorType.SizeAll),
                "SizeNorthSouth" => new Cursor(StandardCursorType.SizeNorthSouth),
                "SizeWestEast" => new Cursor(StandardCursorType.SizeWestEast),
                _ => new Cursor(StandardCursorType.Arrow)
            };
        }

        return new Cursor(StandardCursorType.Arrow);
    }

    /// <summary>
    /// No implementado (no necesario para este uso).
    /// </summary>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

