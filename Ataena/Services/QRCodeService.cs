using System;
using System.IO;
using Avalonia.Media.Imaging;
using QRCoder;
using Serilog;

namespace Ataena.Services;

/// <summary>
/// Servicio para generar códigos QR.
/// </summary>
public static class QRCodeService
{
    /// <summary>
    /// Genera un código QR a partir de una URL o texto.
    /// </summary>
    /// <param name="url">URL o texto a codificar en el QR.</param>
    /// <param name="tamañoPixels">Tamaño del QR en píxeles (por defecto 300).</param>
    /// <returns>Bitmap de Avalonia con el código QR, o null si hay error.</returns>
    public static Bitmap? GenerarQRCode(string url, int tamañoPixels = 300)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            Log.Warning("Intento de generar QR con URL vacía");
            return null;
        }

        try
        {
            using var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
            
            using var qrCode = new PngByteQRCode(qrCodeData);
            var qrCodeBytes = qrCode.GetGraphic(20); // 20 píxeles por módulo
            
            using var stream = new MemoryStream(qrCodeBytes);
            stream.Position = 0;
            return new Bitmap(stream);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al generar código QR para URL: {URL}", url);
            return null;
        }
    }

    /// <summary>
    /// Genera un código QR con tamaño personalizado y nivel de corrección de errores.
    /// </summary>
    /// <param name="url">URL o texto a codificar.</param>
    /// <param name="tamañoPixels">Tamaño deseado del QR en píxeles.</param>
    /// <param name="nivelCorreccion">Nivel de corrección de errores (L, M, Q, H).</param>
    /// <param name="pixelesPorModulo">Píxeles por módulo del QR (por defecto 20).</param>
    /// <returns>Bitmap de Avalonia con el código QR, o null si hay error.</returns>
    public static Bitmap? GenerarQRCodePersonalizado(
        string url, 
        int tamañoPixels = 300, 
        QRCodeGenerator.ECCLevel nivelCorreccion = QRCodeGenerator.ECCLevel.Q,
        int pixelesPorModulo = 20)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            Log.Warning("Intento de generar QR con URL vacía");
            return null;
        }

        try
        {
            using var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(url, nivelCorreccion);
            
            using var qrCode = new PngByteQRCode(qrCodeData);
            var qrCodeBytes = qrCode.GetGraphic(pixelesPorModulo);
            
            using var stream = new MemoryStream(qrCodeBytes);
            stream.Position = 0;
            return new Bitmap(stream);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al generar código QR personalizado para URL: {URL}", url);
            return null;
        }
    }

    /// <summary>
    /// Genera un código QR y lo guarda en un archivo.
    /// </summary>
    /// <param name="url">URL o texto a codificar.</param>
    /// <param name="rutaArchivo">Ruta completa donde guardar el archivo PNG.</param>
    /// <param name="pixelesPorModulo">Píxeles por módulo del QR (por defecto 20).</param>
    /// <returns>True si se guardó correctamente, False en caso contrario.</returns>
    public static bool GenerarYGuardarQRCode(string url, string rutaArchivo, int pixelesPorModulo = 20)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            Log.Warning("Intento de generar QR con URL vacía");
            return false;
        }

        try
        {
            using var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
            
            using var qrCode = new PngByteQRCode(qrCodeData);
            var qrCodeBytes = qrCode.GetGraphic(pixelesPorModulo);
            
            // Guardar en archivo
            File.WriteAllBytes(rutaArchivo, qrCodeBytes);
            
            Log.Information("Código QR guardado en: {Ruta}", rutaArchivo);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al guardar código QR en: {Ruta}", rutaArchivo);
            return false;
        }
    }

    /// <summary>
    /// Genera un código QR como array de bytes (PNG).
    /// </summary>
    /// <param name="url">URL o texto a codificar.</param>
    /// <param name="pixelesPorModulo">Píxeles por módulo del QR (por defecto 20).</param>
    /// <returns>Array de bytes del PNG, o null si hay error.</returns>
    public static byte[]? GenerarQRCodeComoBytes(string url, int pixelesPorModulo = 20)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            Log.Warning("Intento de generar QR con URL vacía");
            return null;
        }

        try
        {
            using var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
            
            using var qrCode = new PngByteQRCode(qrCodeData);
            return qrCode.GetGraphic(pixelesPorModulo);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al generar código QR como bytes para URL: {URL}", url);
            return null;
        }
    }
}

