using System;
using System.Collections.Generic;
using System.Linq;

namespace InkStudio.Models;

/// <summary>
/// Entidad que representa a un cliente del estudio de tatuajes.
/// Contiene información personal, de contacto y preferencias.
/// </summary>
/// <remarks>
/// Tabla principal del CRM. Cada cliente puede tener múltiples citas,
/// trabajos realizados y consentimientos firmados.
/// </remarks>
public class Cliente
{
    #region Identificación

    /// <summary>
    /// Identificador único del cliente (clave primaria).
    /// </summary>
    public int Id { get; set; }

    #endregion

    #region Datos Personales

    /// <summary>
    /// Nombre del cliente. Campo obligatorio.
    /// </summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Apellidos del cliente.
    /// </summary>
    public string Apellidos { get; set; } = string.Empty;

    /// <summary>
    /// Número de teléfono. Campo obligatorio y único.
    /// </summary>
    public string Telefono { get; set; } = string.Empty;

    /// <summary>
    /// Dirección de correo electrónico (opcional).
    /// Se usa para enviar confirmaciones de citas.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Documento Nacional de Identidad (opcional).
    /// </summary>
    public string? Dni { get; set; }

    /// <summary>
    /// Fecha de nacimiento (opcional).
    /// Importante para verificar mayoría de edad.
    /// </summary>
    public DateTime? FechaNacimiento { get; set; }

    #endregion

    #region Información Médica y Notas

    /// <summary>
    /// Alergias conocidas del cliente (opcional).
    /// Crítico para seguridad con tintas y materiales.
    /// </summary>
    public string? Alergias { get; set; }

    /// <summary>
    /// Notas adicionales sobre el cliente (opcional).
    /// </summary>
    public string? Notas { get; set; }

    #endregion

    #region Estado y Preferencias

    /// <summary>
    /// Indica si el cliente es VIP (trato preferencial).
    /// </summary>
    public bool EsVip { get; set; } = false;

    /// <summary>
    /// Indica si el cliente está activo.
    /// False = eliminado lógicamente (soft delete).
    /// </summary>
    public bool Activo { get; set; } = true;

    /// <summary>
    /// Fecha y hora de registro del cliente.
    /// </summary>
    public DateTime FechaRegistro { get; set; } = DateTime.Now;

    #endregion

    #region Navegación (Relaciones)

    /// <summary>
    /// Lista de citas del cliente.
    /// </summary>
    public List<Cita> Citas { get; set; } = new();

    /// <summary>
    /// Lista de trabajos realizados al cliente.
    /// </summary>
    public List<Trabajo> Trabajos { get; set; } = new();

    /// <summary>
    /// Lista de consentimientos firmados por el cliente.
    /// </summary>
    public List<Consentimiento> Consentimientos { get; set; } = new();

    #endregion

    #region Propiedades Calculadas

    /// <summary>
    /// Nombre completo del cliente (Nombre + Apellidos).
    /// </summary>
    public string NombreCompleto => $"{Nombre} {Apellidos}";

    /// <summary>
    /// Edad calculada a partir de la fecha de nacimiento.
    /// Retorna null si no hay fecha de nacimiento.
    /// </summary>
    public int? Edad => FechaNacimiento.HasValue
        ? (int)((DateTime.Today - FechaNacimiento.Value).TotalDays / 365.25)
        : null;

    /// <summary>
    /// Fecha de nacimiento formateada con edad entre paréntesis.
    /// Ejemplo: "15/03/1990 (34 años)" o "15/03/1990" si no hay edad.
    /// </summary>
    public string FechaNacimientoConEdad => FechaNacimiento.HasValue
        ? Edad.HasValue
            ? $"{FechaNacimiento.Value:dd/MM/yyyy} ({Edad} años)"
            : FechaNacimiento.Value.ToString("dd/MM/yyyy")
        : "No especificada";

    /// <summary>
    /// Indica si el cliente tiene el consentimiento RGPD firmado.
    /// </summary>
    public bool TieneConsentimientoRGPD => Consentimientos
        .Any(c => c.Tipo == TipoConsentimiento.RGPD && c.Firmado);

    #endregion
}
