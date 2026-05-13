using System;

namespace Ataena.Models;

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

    /// <summary>
    /// Edad del cliente en el momento de firmar.
    /// Se usa para detectar si necesita renovación al cumplir 18.
    /// </summary>
    public int? EdadClienteAlFirmar { get; set; }

    #endregion

    #region Datos de firma para menores (doble firma)

    /// <summary>
    /// Indica si el consentimiento fue firmado por un menor (requiere firma del tutor).
    /// </summary>
    public bool EsConsentimientoMenor { get; set; } = false;

    /// <summary>
    /// Firma del menor en formato Base64 (para consentimientos de menores).
    /// </summary>
    public string? FirmaMenorBase64 { get; set; }

    /// <summary>
    /// Firma del tutor en formato Base64 (para consentimientos de menores).
    /// </summary>
    public string? FirmaTutorBase64 { get; set; }

    /// <summary>
    /// Nombre completo del tutor que firmó.
    /// </summary>
    public string? NombreTutorFirmante { get; set; }

    /// <summary>
    /// DNI del tutor que firmó.
    /// </summary>
    public string? DniTutorFirmante { get; set; }

    #endregion

    #region Renovación y versiones

    /// <summary>
    /// Indica si este consentimiento ha sido reemplazado por una renovación.
    /// </summary>
    public bool Renovado { get; set; } = false;

    /// <summary>
    /// ID del consentimiento que reemplaza a este (si fue renovado).
    /// </summary>
    public int? ConsentimientoRenovacionId { get; set; }

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
    /// Días transcurridos desde la firma.
    /// </summary>
    public int AntiguedadDias => Firmado ? (int)(DateTime.Now - FechaFirma).TotalDays : 0;

    /// <summary>
    /// Antigüedad en formato legible (Hace X días/meses/años).
    /// </summary>
    public string AntiguedadTexto
    {
        get
        {
            if (!Firmado) return "No firmado";
            
            var dias = AntiguedadDias;
            if (dias == 0) return "Hoy";
            if (dias == 1) return "Ayer";
            if (dias < 7) return $"Hace {dias} días";
            if (dias < 30) return $"Hace {dias / 7} semana{(dias / 7 > 1 ? "s" : "")}";
            if (dias < 365) return $"Hace {dias / 30} mes{(dias / 30 > 1 ? "es" : "")}";
            return $"Hace {dias / 365} año{(dias / 365 > 1 ? "s" : "")}";
        }
    }

    /// <summary>
    /// Indica si el consentimiento es antiguo (más de 2 años).
    /// </summary>
    public bool EsAntiguo => AntiguedadDias > 730; // 2 años

    /// <summary>
    /// Indica si el consentimiento necesita renovación.
    /// Casos: firmado como menor y ahora es mayor, o es muy antiguo, o fue renovado.
    /// </summary>
    public bool NecesitaRenovacion
    {
        get
        {
            if (!Firmado || Renovado) return false;
            
            // Si fue firmado como menor y ahora es mayor de edad
            if (EsConsentimientoMenor && Cliente != null && !Cliente.EsMenorDeEdad)
                return true;
            
            // Si es muy antiguo (más de 2 años)
            if (EsAntiguo)
                return true;
            
            return false;
        }
    }

    /// <summary>
    /// Indica si es un tipo de consentimiento para menores.
    /// </summary>
    public bool EsTipoMenor => Tipo == TipoConsentimiento.RGPD_Menor || 
                               Tipo == TipoConsentimiento.Trabajo_Menor;

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
                TipoConsentimiento.RGPD_Menor => "RGPD - Protección de datos (Menor)",
                TipoConsentimiento.Imagenes => "Consentimiento de uso de imágenes",
                TipoConsentimiento.Trabajo => GetNombreConsentimientoTrabajo(),
                TipoConsentimiento.Trabajo_Menor => GetNombreConsentimientoTrabajo() + " (Menor)",
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
