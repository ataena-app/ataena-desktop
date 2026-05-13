namespace Ataena.Models;

/// <summary>
/// Información sobre un servicio de nube detectado o configurado.
/// </summary>
public class InfoServicioNube
{
    /// <summary>
    /// Tipo de servicio de nube.
    /// </summary>
    public ServicioNube Tipo { get; set; }

    /// <summary>
    /// Nombre del servicio (para mostrar en UI).
    /// </summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Ruta completa a la carpeta de backups en la nube.
    /// </summary>
    public string RutaCarpeta { get; set; } = string.Empty;

    /// <summary>
    /// Indica si el servicio está detectado/instalado.
    /// </summary>
    public bool Detectado { get; set; }

    /// <summary>
    /// Indica si la carpeta está sincronizada con la nube.
    /// </summary>
    public bool Sincronizado { get; set; }

    /// <summary>
    /// Mensaje de estado (para mostrar en UI).
    /// </summary>
    public string MensajeEstado { get; set; } = string.Empty;
}

/// <summary>
/// Tipos de servicios de nube soportados.
/// </summary>
public enum ServicioNube
{
    /// <summary>
    /// Microsoft OneDrive.
    /// </summary>
    OneDrive,

    /// <summary>
    /// Google Drive.
    /// </summary>
    GoogleDrive,

    /// <summary>
    /// Dropbox.
    /// </summary>
    Dropbox,

    /// <summary>
    /// Otra carpeta (seleccionada manualmente).
    /// </summary>
    Otro
}

