using System;
using System.Globalization;
using Ataena.Services;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Ataena.Converters;

/// <summary>
/// Devuelve un color de acento para la barra lateral del aviso superpuesto.
/// </summary>
public class OverlayNotificationKindToAccentBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not OverlayNotificationKind kind)
            return new SolidColorBrush(Color.Parse("#38bdf8"));

        return kind switch
        {
            OverlayNotificationKind.Success => new SolidColorBrush(Color.Parse("#10b981")),
            OverlayNotificationKind.Warning => new SolidColorBrush(Color.Parse("#f97316")),
            OverlayNotificationKind.Error => new SolidColorBrush(Color.Parse("#ef4444")),
            _ => new SolidColorBrush(Color.Parse("#38bdf8"))
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
