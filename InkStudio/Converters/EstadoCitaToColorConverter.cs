using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using InkStudio.Models;

namespace InkStudio.Converters;

/// <summary>
/// Conversor que transforma un <see cref="EstadoCita"/> en un color.
/// Usado para mostrar indicadores visuales del estado de las citas.
/// </summary>
/// <remarks>
/// Colores por estado:
/// - Pendiente: Naranja (#ffa726)
/// - Confirmada: Verde (#66bb6a)
/// - EnProceso: Azul (#42a5f5)
/// - Completada: Gris (#78909c)
/// - Cancelada: Rojo (#ef5350)
/// - NoShow: Morado (#ab47bc)
/// </remarks>
public class EstadoCitaToColorConverter : IValueConverter
{
    /// <summary>
    /// Instancia estática para uso en XAML.
    /// </summary>
    public static readonly EstadoCitaToColorConverter Instance = new();

    /// <summary>
    /// Convierte un estado de cita a su color correspondiente.
    /// </summary>
    /// <param name="value">Estado de la cita.</param>
    /// <param name="targetType">Tipo de destino (Brush).</param>
    /// <param name="parameter">Parámetro adicional (no usado).</param>
    /// <param name="culture">Cultura actual.</param>
    /// <returns>SolidColorBrush con el color correspondiente al estado.</returns>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is EstadoCita estado)
        {
            return estado switch
            {
                EstadoCita.Pendiente => new SolidColorBrush(Color.Parse("#ffa726")),    // Naranja
                EstadoCita.Confirmada => new SolidColorBrush(Color.Parse("#66bb6a")),   // Verde
                EstadoCita.EnProceso => new SolidColorBrush(Color.Parse("#42a5f5")),    // Azul
                EstadoCita.Completada => new SolidColorBrush(Color.Parse("#78909c")),   // Gris
                EstadoCita.Cancelada => new SolidColorBrush(Color.Parse("#ef5350")),    // Rojo
                EstadoCita.NoShow => new SolidColorBrush(Color.Parse("#ab47bc")),       // Morado
                _ => new SolidColorBrush(Color.Parse("#9e9e9e"))                        // Default: Gris
            };
        }
        return new SolidColorBrush(Color.Parse("#9e9e9e"));
    }

    /// <summary>
    /// No implementado (no necesario para este uso).
    /// </summary>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
