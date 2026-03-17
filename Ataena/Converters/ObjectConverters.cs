using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Ataena.Converters;

/// <summary>
/// Conversores para objetos (null checks).
/// </summary>
public static class ObjectConverters
{
    /// <summary>
    /// Conversor que devuelve true si el objeto no es null.
    /// </summary>
    public static readonly IValueConverter IsNotNull = new IsNotNullConverter();

    /// <summary>
    /// Conversor que devuelve true si el objeto es null.
    /// </summary>
    public static readonly IValueConverter IsNull = new IsNullConverter();

    private class IsNotNullConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value != null;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    private class IsNullConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value == null;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

