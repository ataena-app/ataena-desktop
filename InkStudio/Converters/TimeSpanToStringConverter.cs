using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace InkStudio.Converters;

/// <summary>
/// Convierte TimeSpan a string (HH:mm) y viceversa.
/// </summary>
public class TimeSpanToStringConverter : IValueConverter
{
    /// <summary>
    /// Instancia estática para uso en XAML.
    /// </summary>
    public static readonly TimeSpanToStringConverter Instance = new();

    /// <summary>
    /// Convierte TimeSpan a string (HH:mm).
    /// </summary>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TimeSpan timeSpan)
        {
            return timeSpan.ToString(@"hh\:mm");
        }
        return string.Empty;
    }

    /// <summary>
    /// Convierte string (HH:mm) a TimeSpan.
    /// </summary>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string str && !string.IsNullOrWhiteSpace(str))
        {
            if (TimeSpan.TryParse(str, out var timeSpan))
            {
                return timeSpan;
            }
            
            // Intentar parsear formato HH:mm
            var parts = str.Split(':');
            if (parts.Length == 2 && 
                int.TryParse(parts[0], out var hours) && 
                int.TryParse(parts[1], out var minutes))
            {
                return new TimeSpan(hours, minutes, 0);
            }
        }
        return new TimeSpan(10, 0, 0); // Default: 10:00
    }
}

