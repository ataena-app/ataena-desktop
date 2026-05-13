using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Serilog;

namespace Ataena.Services;

/// <summary>
/// Servicio para escanear documentos usando el escáner predeterminado de Windows (WIA).
/// Solo funciona en Windows.
/// </summary>
public static class EscannerService
{
    /// <summary>
    /// Indica si el escaneo está disponible (solo Windows).
    /// </summary>
    public static bool EstaDisponible => OperatingSystem.IsWindows();

    /// <summary>
    /// Escanea un documento usando el escáner predeterminado de Windows.
    /// Muestra el diálogo de WIA para que el usuario configure y ejecute el escaneo.
    /// </summary>
    /// <param name="rutaDestino">Ruta donde guardar la imagen escaneada (JPEG).</param>
    /// <returns>True si se escaneó y guardó correctamente.</returns>
    public static async Task<(bool Exito, string Mensaje)> EscanearAsync(string rutaDestino)
    {
        if (!OperatingSystem.IsWindows())
        {
            return (false, "El escaneo solo está disponible en Windows.");
        }

        return await Task.Run(() =>
        {
            try
            {
                // Crear CommonDialog de WIA via COM (solo Windows)
#pragma warning disable CA1416
                var wiaType = Type.GetTypeFromProgID("WIA.CommonDialog");
#pragma warning restore CA1416
                if (wiaType == null)
                {
                    Log.Warning("No se pudo cargar WIA.CommonDialog - ¿Está instalado WIA?");
                    return (false, "No se encontró el componente de escaneo de Windows. Verifica que el escáner esté conectado y configurado como predeterminado.");
                }

                dynamic dlg = Activator.CreateInstance(wiaType)!;

                // WiaDeviceType.ScannerDeviceType = 1
                // WiaImageIntent.ColorIntent = 1
                // WiaImageBias.MinimizeSize = 1
                // FormatID JPEG: {B96B3CA0-0728-11D3-9D7B-0004F79EF0E}
                // AlwaysSelectDevice: false (usar predeterminado)
                // UseCommonUI: true (mostrar interfaz del escáner)
                // CancelError: false (no lanzar si el usuario cancela)
                const int ScannerDeviceType = 1;
                const int ColorIntent = 1;
                const int MinimizeSize = 1;
                const string JpegFormatId = "{B96B3CA0-0728-11D3-9D7B-0004F79EF0E}";

                dynamic? image = dlg.ShowAcquireImage(
                    ScannerDeviceType,
                    ColorIntent,
                    MinimizeSize,
                    JpegFormatId,
                    false, // AlwaysSelectDevice
                    true,  // UseCommonUI
                    false  // CancelError
                );

                if (image == null)
                {
                    return (false, "El usuario canceló el escaneo.");
                }

                // Guardar a archivo
                var directorio = Path.GetDirectoryName(rutaDestino);
                if (!string.IsNullOrEmpty(directorio))
                {
                    Directory.CreateDirectory(directorio);
                }

                image.SaveFile(rutaDestino);
                Log.Information("Documento escaneado guardado en: {Ruta}", rutaDestino);
                return (true, "Documento escaneado correctamente.");
            }
            catch (COMException ex)
            {
                Log.Error(ex, "Error COM al escanear");
                return (false, $"Error del escáner: {ex.Message}. Verifica que esté conectado y configurado como predeterminado en Windows.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error al escanear documento");
                return (false, $"Error al escanear: {ex.Message}");
            }
        });
    }
}
