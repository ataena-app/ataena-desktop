using System;
using System.IO;

namespace Ataena.Models;

/// <summary>
/// Información sobre un archivo de backup para mostrar en la UI.
/// </summary>
public class InfoBackup
{
    /// <summary>
    /// Ruta completa al archivo de backup.
    /// </summary>
    public string RutaCompleta { get; set; } = string.Empty;

    /// <summary>
    /// Nombre del archivo (sin ruta).
    /// </summary>
    public string NombreArchivo { get; set; } = string.Empty;

    /// <summary>
    /// Fecha y hora de creación del backup.
    /// </summary>
    public DateTime FechaCreacion { get; set; }

    /// <summary>
    /// Tamaño del archivo en bytes.
    /// </summary>
    public long TamañoBytes { get; set; }

    /// <summary>
    /// Tamaño formateado para mostrar (ej: "45.2 MB").
    /// </summary>
    public string TamañoFormateado { get; set; } = string.Empty;

    /// <summary>
    /// Metadatos del backup (si están disponibles).
    /// </summary>
    public BackupMetadata? Metadata { get; set; }

    /// <summary>
    /// Indica si el backup está sincronizado con la nube.
    /// </summary>
    public bool Sincronizado { get; set; }

    /// <summary>
    /// Indica si el backup es válido y restaurable.
    /// </summary>
    public bool EsValido { get; set; } = true;

    /// <summary>
    /// Crea una instancia de InfoBackup desde un archivo.
    /// </summary>
    public static InfoBackup DesdeArchivo(string rutaArchivo)
    {
        var fileInfo = new FileInfo(rutaArchivo);
        return new InfoBackup
        {
            RutaCompleta = rutaArchivo,
            NombreArchivo = fileInfo.Name,
            FechaCreacion = fileInfo.CreationTime,
            TamañoBytes = fileInfo.Length,
            TamañoFormateado = FormatearTamaño(fileInfo.Length)
        };
    }

    /// <summary>
    /// Formatea el tamaño en bytes a formato legible.
    /// </summary>
    private static string FormatearTamaño(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}

