using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Ataena.Converters;

/// <summary>
/// Conversor que devuelve true si el objeto es null.
/// </summary>
public class IsNullConverter : IValueConverter
{
    public static readonly IsNullConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value == null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

