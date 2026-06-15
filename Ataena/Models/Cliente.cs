using System;
using System.Collections.Generic;
using System.Linq;
using Ataena.Services;

namespace Ataena.Models;

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
    /// Apellidos del cliente. Campo obligatorio.
    /// </summary>
    public string Apellidos { get; set; } = string.Empty;

    /// <summary>
    /// Número de teléfono (opcional en alta; se valida el formato si se indica).
    /// </summary>
    public string Telefono { get; set; } = string.Empty;

    /// <summary>
    /// Dirección de correo electrónico (opcional).
    /// Se usa para enviar confirmaciones de citas.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Documento Nacional de Identidad (DNI/NIE/pasaporte). Obligatorio en texto — sin foto.
    /// </summary>
    public string? Dni { get; set; }

    /// <summary>
    /// Fecha de nacimiento. Campo obligatorio (mayoría de edad, menores, consentimientos).
    /// </summary>
    public DateTime? FechaNacimiento { get; set; }

    #endregion

    #region Datos del Tutor (para menores de edad)

    /// <summary>
    /// Nombre del tutor/representante legal (requerido si es menor).
    /// </summary>
    public string? NombreTutor { get; set; }

    /// <summary>
    /// Apellidos del tutor/representante legal.
    /// </summary>
    public string? ApellidosTutor { get; set; }

    /// <summary>
    /// DNI del tutor/representante legal.
    /// </summary>
    public string? DniTutor { get; set; }

    /// <summary>
    /// Teléfono del tutor/representante legal.
    /// </summary>
    public string? TelefonoTutor { get; set; }

    #endregion

    #region Fotos de DNI

    /// <summary>
    /// Ruta a la foto del DNI del cliente.
    /// </summary>
    public string? FotoDniPath { get; set; }

    /// <summary>
    /// Ruta a la foto del DNI del tutor (para menores).
    /// </summary>
    public string? FotoDniTutorPath { get; set; }

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
    /// Si es <c>false</c>, el cliente no acepta fotos antes/después en trabajos ni el flujo de firma del consentimiento de imágenes para ello.
    /// </summary>
    public bool PermiteFotosTrabajo { get; set; } = true;

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
    /// Indica si el cliente tiene el consentimiento RGPD firmado (incluye RGPD_Menor).
    /// </summary>
    public bool TieneConsentimientoRGPD =>
        Consentimiento.TieneConsentimientoRgpdVigente(Consentimientos);

    /// <summary>
    /// Indica si el cliente tiene al menos un consentimiento de uso de imágenes firmado en base de datos.
    /// </summary>
    public bool TieneConsentimientoImagenes =>
        Consentimiento.TieneConsentimientoImagenesVigente(Consentimientos, EsMenorDeEdad);

    /// <summary>Chip en ficha: cliente excluye fotos de trabajo.</summary>
    public bool ImagenesChipSinFotosAcordado => !PermiteFotosTrabajo;

    /// <summary>Chip en ficha: puede imágenes y hay firma vigente.</summary>
    public bool ImagenesChipOk => PermiteFotosTrabajo && TieneConsentimientoImagenes;

    /// <summary>Chip en ficha: permite fotos pero aún falta consentimiento firmado.</summary>
    public bool ImagenesChipPendienteFirma => PermiteFotosTrabajo && !TieneConsentimientoImagenes;

    /// <summary>Mostrar botón firmar uso de imágenes desde la ficha.</summary>
    public bool MostrarAccionFirmarConsentimientoImagenes =>
        PermiteFotosTrabajo && !TieneConsentimientoImagenes;

    /// <summary>
    /// Indica si el cliente es menor de edad (menos de 18 años).
    /// </summary>
    public bool EsMenorDeEdad => Edad.HasValue && Edad.Value < 18;

    /// <summary>
    /// Indica si el cliente menor de edad tiene datos del tutor completos.
    /// </summary>
    public bool TieneDatosTutor => !string.IsNullOrWhiteSpace(NombreTutor) &&
                                   !string.IsNullOrWhiteSpace(ApellidosTutor) &&
                                   !string.IsNullOrWhiteSpace(DniTutor);

    /// <summary>
    /// Indica si el cliente menor requiere datos del tutor (es menor y no los tiene).
    /// </summary>
    public bool RequiereDatosTutor => EsMenorDeEdad && !TieneDatosTutor;

    /// <summary>
    /// Nombre completo del tutor.
    /// </summary>
    public string NombreCompletoTutor => TieneDatosTutor 
        ? $"{NombreTutor} {ApellidosTutor}" 
        : string.Empty;

    /// <summary>
    /// Indica si los datos identificativos del cliente están completos (texto, no foto).
    /// </summary>
    public bool TieneDatosIdentificacionCompletos =>
        !string.IsNullOrWhiteSpace(Dni) && FechaNacimiento.HasValue;

    /// <summary>
    /// Indica si el tutor tiene DNI en texto (menores).
    /// </summary>
    public bool TieneDatosIdentificacionTutor =>
        !string.IsNullOrWhiteSpace(DniTutor);

    /// <summary>
    /// Indica si el cliente tiene foto de su DNI (legado / perfil Madrid en trabajo).
    /// </summary>
    public bool TieneFotoDni =>
        ConsentimientoPathService.RutaFotoDniExistente(Id, FotoDniPath) != null;

    /// <summary>
    /// Indica si el tutor tiene foto de DNI (para menores).
    /// </summary>
    public bool TieneFotoDniTutor =>
        ConsentimientoPathService.RutaFotoDniTutorExistente(Id, FotoDniTutorPath) != null;

    /// <summary>
    /// Indica si el cliente tiene consentimientos que necesitan renovación.
    /// </summary>
    public bool TieneConsentimientosPendientesRenovacion => Consentimientos
        .Any(c => c.Firmado && c.NecesitaRenovacion);

    /// <summary>
    /// Número de consentimientos que necesitan renovación.
    /// </summary>
    public int NumeroConsentimientosPendientesRenovacion => Consentimientos
        .Count(c => c.Firmado && c.NecesitaRenovacion);

    /// <summary>
    /// RGPD firmado, imágenes si aplica, y sin renovaciones pendientes.
    /// </summary>
    public bool TieneConsentimientosFichaCompletos =>
        TieneConsentimientoRGPD &&
        (!PermiteFotosTrabajo || TieneConsentimientoImagenes) &&
        !TieneConsentimientosPendientesRenovacion;

    /// <summary>
    /// Datos identificativos en texto (DNI + fecha; tutor si es menor). Sin foto del documento.
    /// </summary>
    public bool TieneDocumentacionIdentificacionCompleta =>
        TieneDatosIdentificacionCompletos &&
        (!EsMenorDeEdad || (TieneDatosTutor && TieneDatosIdentificacionTutor));

    /// <summary>
    /// Ficha al día: identificación en texto + consentimientos completos (barra verde).
    /// </summary>
    public bool FichaListaCompleta =>
        TieneDocumentacionIdentificacionCompleta && TieneConsentimientosFichaCompletos;

    /// <summary>
    /// Falta RGPD u otro requisito crítico (barra roja).
    /// </summary>
    public bool FichaListaCritica => !TieneConsentimientoRGPD;

    /// <summary>
    /// Tiene RGPD pero le falta algo más: DNI, imágenes, renovación, etc. (barra naranja).
    /// </summary>
    public bool FichaListaParcial => !FichaListaCompleta && !FichaListaCritica;

    #endregion
}
