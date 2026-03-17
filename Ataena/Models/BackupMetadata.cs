using System;

namespace Ataena.Models;

/// <summary>
/// Metadatos de un backup para almacenar información sobre la copia de seguridad.
/// Se serializa en JSON dentro del archivo ZIP del backup.
/// </summary>
public class BackupMetadata
{
    /// <summary>
    /// Fecha y hora en que se creó el backup.
    /// </summary>
    public DateTime FechaCreacion { get; set; }

    /// <summary>
    /// Versión de la aplicación que creó el backup.
    /// </summary>
    public string VersionApp { get; set; } = string.Empty;

    /// <summary>
    /// Tamaño total de los datos respaldados en bytes.
    /// </summary>
    public long TamañoTotalBytes { get; set; }

    /// <summary>
    /// Número de clientes en el backup.
    /// </summary>
    public int NumeroClientes { get; set; }

    /// <summary>
    /// Número de citas en el backup.
    /// </summary>
    public int NumeroCitas { get; set; }

    /// <summary>
    /// Número de trabajos en el backup.
    /// </summary>
    public int NumeroTrabajos { get; set; }

    /// <summary>
    /// Número de consentimientos en el backup.
    /// </summary>
    public int NumeroConsentimientos { get; set; }

    /// <summary>
    /// Checksum del archivo ZIP (opcional, para validación de integridad).
    /// </summary>
    public string? Checksum { get; set; }

    /// <summary>
    /// Notas adicionales sobre el backup (opcional).
    /// </summary>
    public string? Notas { get; set; }
}

