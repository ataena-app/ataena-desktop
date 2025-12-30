using System;
using System.Threading.Tasks;
using Avalonia.Threading;
using Serilog;

namespace InkStudio.Services;

/// <summary>
/// Servicio para gestionar confirmaciones de acciones destructivas.
/// Usa un sistema de callback simple que cada ViewModel implementa con su propio overlay.
/// </summary>
public static class DialogService
{
    /// <summary>
    /// Evento que se dispara cuando se solicita una confirmación.
    /// Los ViewModels pueden suscribirse para mostrar su propio diálogo.
    /// </summary>
    public static event Func<ConfirmacionInfo, Task<bool>>? OnConfirmacionRequerida;

    /// <summary>
    /// Solicita confirmación al usuario para una acción.
    /// Si no hay handler registrado, devuelve true (permite la acción).
    /// </summary>
    public static async Task<bool> ConfirmarAccionAsync(
        string titulo,
        string mensaje,
        string botonConfirmar = "Confirmar",
        bool esPeligroso = true)
    {
        var info = new ConfirmacionInfo
        {
            Titulo = titulo,
            Mensaje = mensaje,
            BotonConfirmar = botonConfirmar,
            EsPeligroso = esPeligroso
        };

        Log.Debug("Solicitando confirmación: {Titulo}", titulo);

        if (OnConfirmacionRequerida != null)
        {
            try
            {
                return await OnConfirmacionRequerida(info);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error al mostrar diálogo de confirmación");
                return false; // En caso de error, no confirmar
            }
        }

        // Si no hay handler, permitir la acción (fallback)
        Log.Warning("No hay handler de confirmación registrado, permitiendo acción por defecto");
        return true;
    }

    /// <summary>
    /// Solicita confirmación para eliminar un elemento.
    /// </summary>
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
    /// Solicita confirmación para restaurar un backup.
    /// </summary>
    public static Task<bool> ConfirmarRestaurarBackupAsync(
        string nombreBackup,
        string resumen)
    {
        var mensaje = $"¿Restaurar este backup?\n\n" +
                      $"📦 {nombreBackup}\n\n" +
                      $"{resumen}\n\n" +
                      $"⚠️ Esta acción reemplazará TODOS los datos actuales.\n" +
                      $"Se creará un backup automático antes de restaurar.";

        return ConfirmarAccionAsync(
            titulo: "🔄 Restaurar Backup",
            mensaje: mensaje,
            botonConfirmar: "Restaurar",
            esPeligroso: true
        );
    }

    /// <summary>
    /// Información para el diálogo de confirmación.
    /// </summary>
    public class ConfirmacionInfo
    {
        public string Titulo { get; set; } = "";
        public string Mensaje { get; set; } = "";
        public string BotonConfirmar { get; set; } = "Confirmar";
        public bool EsPeligroso { get; set; } = true;
    }
}
