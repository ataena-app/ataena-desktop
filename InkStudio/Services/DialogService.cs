using System;
using System.Threading.Tasks;
using FluentAvalonia.UI.Controls;
using Avalonia.Threading;
using Serilog;

namespace InkStudio.Services;

/// <summary>
/// Servicio para mostrar diálogos de confirmación.
/// Utiliza ContentDialog de FluentAvalonia para una experiencia nativa.
/// </summary>
public static class DialogService
{
    /// <summary>
    /// Muestra un diálogo de confirmación para acciones destructivas.
    /// </summary>
    /// <param name="titulo">Título del diálogo (ej: "Eliminar cliente")</param>
    /// <param name="mensaje">Mensaje descriptivo de la acción</param>
    /// <param name="botonConfirmar">Texto del botón de confirmación (ej: "Eliminar")</param>
    /// <param name="esPeligroso">Si es true, el botón de confirmar será rojo</param>
    /// <returns>True si el usuario confirmó, False si canceló</returns>
    public static async Task<bool> ConfirmarAccionAsync(
        string titulo,
        string mensaje,
        string botonConfirmar = "Confirmar",
        bool esPeligroso = true)
    {
        return await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            try
            {
                var dialog = new ContentDialog
                {
                    Title = titulo,
                    Content = mensaje,
                    PrimaryButtonText = botonConfirmar,
                    CloseButtonText = "Cancelar",
                    DefaultButton = ContentDialogButton.Close // Por defecto, el foco está en Cancelar (más seguro)
                };

                var result = await dialog.ShowAsync();
                
                var confirmado = result == ContentDialogResult.Primary;
                Log.Debug("Diálogo '{Titulo}': Usuario seleccionó {Resultado}", titulo, confirmado ? "Confirmar" : "Cancelar");
                
                return confirmado;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error al mostrar diálogo de confirmación: {Titulo}", titulo);
                return false; // En caso de error, no confirmar (seguro)
            }
        });
    }

    /// <summary>
    /// Muestra un diálogo de confirmación para eliminar un elemento.
    /// </summary>
    /// <param name="tipoElemento">Tipo de elemento (ej: "cliente", "trabajo", "cita")</param>
    /// <param name="nombreElemento">Nombre o descripción del elemento a eliminar</param>
    /// <param name="advertenciaAdicional">Texto adicional de advertencia (opcional)</param>
    /// <returns>True si el usuario confirmó la eliminación</returns>
    public static Task<bool> ConfirmarEliminarAsync(
        string tipoElemento,
        string nombreElemento,
        string? advertenciaAdicional = null)
    {
        var mensaje = $"¿Estás seguro de que deseas eliminar {tipoElemento}?\n\n" +
                      $"📌 {nombreElemento}";
        
        if (!string.IsNullOrEmpty(advertenciaAdicional))
        {
            mensaje += $"\n\n⚠️ {advertenciaAdicional}";
        }

        return ConfirmarAccionAsync(
            titulo: $"🗑️ Eliminar {tipoElemento}",
            mensaje: mensaje,
            botonConfirmar: "Eliminar",
            esPeligroso: true
        );
    }

    /// <summary>
    /// Muestra un diálogo de confirmación para restaurar un backup.
    /// </summary>
    /// <param name="nombreBackup">Nombre del archivo de backup</param>
    /// <param name="resumen">Resumen del contenido del backup</param>
    /// <returns>True si el usuario confirmó la restauración</returns>
    public static Task<bool> ConfirmarRestaurarBackupAsync(
        string nombreBackup,
        string resumen)
    {
        var mensaje = $"¿Restaurar este backup?\n\n" +
                      $"📦 {nombreBackup}\n\n" +
                      $"{resumen}\n\n" +
                      $"⚠️ Esta acción reemplazará TODOS los datos actuales.\n" +
                      $"Se creará un backup automático de los datos actuales antes de restaurar.";

        return ConfirmarAccionAsync(
            titulo: "🔄 Restaurar Backup",
            mensaje: mensaje,
            botonConfirmar: "Restaurar",
            esPeligroso: true
        );
    }

    /// <summary>
    /// Muestra un diálogo informativo (solo botón de cerrar).
    /// </summary>
    /// <param name="titulo">Título del diálogo</param>
    /// <param name="mensaje">Mensaje informativo</param>
    public static async Task MostrarInfoAsync(string titulo, string mensaje)
    {
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            try
            {
                var dialog = new ContentDialog
                {
                    Title = titulo,
                    Content = mensaje,
                    CloseButtonText = "Aceptar",
                    DefaultButton = ContentDialogButton.Close
                };

                await dialog.ShowAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error al mostrar diálogo informativo: {Titulo}", titulo);
            }
        });
    }
}

