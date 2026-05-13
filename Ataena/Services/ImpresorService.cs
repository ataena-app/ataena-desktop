using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Serilog;

namespace Ataena.Services;

/// <summary>
/// Servicio para imprimir documentos usando la impresora predeterminada de Windows.
/// </summary>
public static class ImpresorService
{
    /// <summary>
    /// Envía un archivo PDF a la impresora predeterminada de Windows.
    /// </summary>
    /// <param name="rutaPdf">Ruta completa al archivo PDF.</param>
    /// <returns>True si se envió correctamente a imprimir.</returns>
    public static async Task<(bool Exito, string Mensaje)> ImprimirPdfAsync(string rutaPdf)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(rutaPdf) || !File.Exists(rutaPdf))
                {
                    return (false, "El archivo PDF no existe.");
                }

                if (!OperatingSystem.IsWindows())
                {
                    return (false, "La impresión directa solo está disponible en Windows.");
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = rutaPdf,
                    UseShellExecute = true,
                    Verb = "print",
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                Process.Start(startInfo);
                Log.Information("PDF enviado a imprimir: {Ruta}", rutaPdf);
                return (true, "Documento enviado a la impresora predeterminada.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error al imprimir PDF");
                return (false, $"Error al imprimir: {ex.Message}");
            }
        });
    }
}
