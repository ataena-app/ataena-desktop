using System;

namespace InkStudio.Models;

/// <summary>
/// Entidad que representa una cita en el estudio.
/// Puede ser para tatuaje, piercing, consulta o retoque.
/// </summary>
/// <remarks>
/// Las citas están vinculadas a un cliente y pueden generar un trabajo.
/// El sistema envía emails de confirmación cuando está configurado.
/// </remarks>
public class Cita
{
    #region Identificación

    /// <summary>
    /// Identificador único de la cita (clave primaria).
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// ID del cliente asociado (clave foránea).
    /// </summary>
    public int ClienteId { get; set; }

    #endregion

    #region Fecha y Hora

    /// <summary>
    /// Fecha de la cita (solo fecha, sin hora).
    /// </summary>
    public DateTime Fecha { get; set; }

    /// <summary>
    /// Hora de inicio de la cita.
    /// </summary>
    public TimeSpan HoraInicio { get; set; }

    /// <summary>
    /// Duración estimada de la cita en minutos.
    /// Valor por defecto: 60 minutos.
    /// </summary>
    public int DuracionMinutos { get; set; } = 60;

    #endregion

    #region Detalles de la Cita

    /// <summary>
    /// Tipo de cita (Tatuaje, Piercing, Consulta, Retoque).
    /// </summary>
    public TipoCita TipoCita { get; set; } = TipoCita.Tatuaje;

    /// <summary>
    /// Descripción del trabajo a realizar (opcional).
    /// </summary>
    public string? Descripcion { get; set; }

    /// <summary>
    /// Estado actual de la cita.
    /// </summary>
    public EstadoCita Estado { get; set; } = EstadoCita.Pendiente;

    /// <summary>
    /// Notas adicionales sobre la cita (opcional).
    /// </summary>
    public string? Notas { get; set; }

    #endregion

    #region Email de Confirmación

    /// <summary>
    /// Indica si se ha enviado email de confirmación.
    /// </summary>
    public bool EmailEnviado { get; set; } = false;

    /// <summary>
    /// Fecha y hora en que se envió el email (si aplica).
    /// </summary>
    public DateTime? FechaEmailEnviado { get; set; }

    #endregion

    #region Auditoría

    /// <summary>
    /// Fecha y hora de creación del registro.
    /// </summary>
    public DateTime FechaCreacion { get; set; } = DateTime.Now;

    #endregion

    #region Navegación (Relaciones)

    /// <summary>
    /// Cliente asociado a la cita.
    /// </summary>
    public Cliente Cliente { get; set; } = null!;

    /// <summary>
    /// Trabajo generado por esta cita (si existe).
    /// </summary>
    public Trabajo? Trabajo { get; set; }

    #endregion

    #region Propiedades Calculadas

    /// <summary>
    /// Fecha y hora de inicio completa.
    /// </summary>
    public DateTime FechaHoraInicio => Fecha.Date + HoraInicio;

    /// <summary>
    /// Fecha y hora de fin estimada.
    /// </summary>
    public DateTime FechaHoraFin => FechaHoraInicio.AddMinutes(DuracionMinutos);

    /// <summary>
    /// Indica si la cita ya pasó.
    /// </summary>
    public bool EsPasada => FechaHoraInicio < DateTime.Now;

    /// <summary>
    /// Indica si la cita es hoy.
    /// </summary>
    public bool EsHoy => Fecha.Date == DateTime.Today;

    /// <summary>
    /// Hora de inicio formateada (HH:mm en formato 24 horas).
    /// </summary>
    public string HoraInicioFormateada => $"{HoraInicio.Hours:D2}:{HoraInicio.Minutes:D2}";

    /// <summary>
    /// Duración formateada legible (ej: "2h 30m").
    /// </summary>
    public string DuracionFormateada => DuracionMinutos >= 60
        ? $"{DuracionMinutos / 60}h {DuracionMinutos % 60}m".TrimEnd('0', 'm', ' ')
        : $"{DuracionMinutos}m";

    /// <summary>
    /// Emoji representativo del tipo de cita.
    /// </summary>
    public string IconoTipo => TipoCita switch
    {
        TipoCita.Tatuaje => "🎨",
        TipoCita.Piercing => "💎",
        TipoCita.Consulta => "📋",
        TipoCita.Retoque => "🔄",
        _ => "📅"
    };

    #endregion
}
