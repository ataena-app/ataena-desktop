using System;
using Avalonia.Threading;
using Serilog;

namespace Ataena.Services;

/// <summary>
/// Muestra un aviso centrado por encima de modales secundarios.
/// El gestor visual se registra desde <see cref="ViewModels.MainWindowViewModel"/>.
/// </summary>
public static class OverlayNotificationService
{
    private static readonly object Sync = new();
    private static Action<string, OverlayNotificationKind, string?>? _handler;

    /// <summary>
    /// Registra el callback que mostrará el overlay (solo la ventana principal).
    /// </summary>
    public static void Registrar(Action<string, OverlayNotificationKind, string?> mostrarOverlay)
    {
        lock (Sync)
            _handler = mostrarOverlay;
    }

    /// <summary>
    /// Quita el registro al cerrar la app (opcional).
    /// </summary>
    public static void Desregistrar()
    {
        lock (Sync)
            _handler = null;
    }

    /// <summary>
    /// Muestra un mensaje en el centro de la ventana principal, sobre el contenido actual.
    /// Ejecutado en el hilo UI.
    /// </summary>
    /// <param name="titulo">Si no es null, sustituye el encabezado por defecto del tipo.</param>
    public static void Mostrar(
        string mensaje,
        OverlayNotificationKind tipo = OverlayNotificationKind.Information,
        string? titulo = null)
    {
        if (string.IsNullOrWhiteSpace(mensaje))
            return;

        void Invocar()
        {
            try
            {
                lock (Sync)
                {
                    _handler?.Invoke(mensaje.Trim(), tipo, titulo);
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "OverlayNotificationService: error al invocar handler");
            }
        }

        if (Dispatcher.UIThread.CheckAccess())
            Invocar();
        else
            Dispatcher.UIThread.Post(Invocar, DispatcherPriority.Normal);
    }
}
