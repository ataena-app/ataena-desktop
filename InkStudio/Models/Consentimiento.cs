using System;

namespace InkStudio.Models;

/// <summary>
/// Entidad que representa un consentimiento firmado por un cliente.
/// Existen tres tipos: RGPD, uso de imágenes y por trabajo.
/// </summary>
/// <remarks>
/// Los consentimientos son documentos legales importantes para:
/// - RGPD: Cumplimiento de protección de datos
/// - Imágenes: Permiso para usar fotos en redes sociales
/// - Trabajo: Consentimiento informado para cada tatuaje/piercing
/// </remarks>
public class Consentimiento
{
    #region Identificación

    /// <summary>
    /// Identificador único del consentimiento (clave primaria).
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// ID del cliente que firma (clave foránea).
    /// </summary>
    public int ClienteId { get; set; }

    /// <summary>
    /// ID del trabajo asociado (solo para tipo Trabajo).
    /// </summary>
    public int? TrabajoId { get; set; }

    #endregion

    #region Detalles del Consentimiento

    /// <summary>
    /// Tipo de consentimiento (RGPD, Imágenes o Trabajo).
    /// </summary>
    public TipoConsentimiento Tipo { get; set; }

    /// <summary>
    /// Fecha y hora de la firma.
    /// </summary>
    public DateTime FechaFirma { get; set; } = DateTime.Now;

    /// <summary>
    /// Ruta al documento PDF firmado (opcional).
    /// </summary>
    public string? RutaDocumento { get; set; }

    /// <summary>
    /// Indica si el consentimiento ha sido firmado.
    /// </summary>
    public bool Firmado { get; set; } = false;

    /// <summary>
    /// Notas adicionales (opcional).
    /// </summary>
    public string? Notas { get; set; }

    #endregion

    #region Navegación (Relaciones)

    /// <summary>
    /// Cliente que firmó el consentimiento.
    /// </summary>
    public Cliente Cliente { get; set; } = null!;

    /// <summary>
    /// Trabajo asociado (solo para consentimientos de tipo Trabajo).
    /// </summary>
    public Trabajo? Trabajo { get; set; }

    #endregion

    #region Propiedades Calculadas

    /// <summary>
    /// Indica si existe un documento PDF asociado.
    /// </summary>
    public bool TieneDocumento => !string.IsNullOrEmpty(RutaDocumento);

    /// <summary>
    /// Nombre legible del consentimiento, con más contexto para los de trabajo.
    /// </summary>
    public string NombreTipo
    {
        get
        {
            return Tipo switch
            {
                TipoConsentimiento.RGPD => "RGPD - Protección de datos",
                TipoConsentimiento.Imagenes => "Consentimiento de uso de imágenes",
                TipoConsentimiento.Trabajo => GetNombreConsentimientoTrabajo(),
                _ => "Consentimiento desconocido"
            };
        }
    }

    /// <summary>
    /// Genera un nombre más descriptivo para los consentimientos de trabajo,
    /// incluyendo información del tipo de trabajo y una breve descripción/zona.
    /// </summary>
    private string GetNombreConsentimientoTrabajo()
    {
        if (Trabajo == null)
            return "Consentimiento de trabajo";

        // Tipo de trabajo legible
        var tipoTrabajo = Trabajo.Tipo switch
        {
            TipoTrabajo.Tatuaje => "Tatuaje",
            TipoTrabajo.Piercing => "Piercing",
            _ => "Trabajo"
        };

        // Usar descripción si existe, si no la zona del cuerpo
        var detalle = !string.IsNullOrWhiteSpace(Trabajo.Descripcion)
            ? Trabajo.Descripcion.Trim()
            : Trabajo.ZonaCuerpo?.Trim() ?? string.Empty;

        // Limitar longitud para que no rompa el layout de la ficha
        const int maxLongitud = 40;
        if (!string.IsNullOrEmpty(detalle) && detalle.Length > maxLongitud)
        {
            detalle = detalle.Substring(0, maxLongitud) + "…";
        }

        return string.IsNullOrEmpty(detalle)
            ? $"Consentimiento de {tipoTrabajo}"
            : $"Trabajo de {tipoTrabajo}: {detalle}";
    }

    #endregion
}
