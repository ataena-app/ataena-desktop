using System;
using System.Collections.Generic;
using System.Text.Json;

namespace InkStudio.Models;

/// <summary>
/// Entidad que representa un trabajo realizado (tatuaje o piercing).
/// Almacena detalles técnicos, precio y fotos del resultado.
/// </summary>
/// <remarks>
/// Un trabajo está vinculado a un cliente y opcionalmente a una cita.
/// Las fotos se almacenan como rutas en formato JSON.
/// </remarks>
public class Trabajo
{
    #region Identificación

    /// <summary>
    /// Identificador único del trabajo (clave primaria).
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// ID del cliente asociado (clave foránea).
    /// </summary>
    public int ClienteId { get; set; }

    #endregion

    #region Detalles del Trabajo

    /// <summary>
    /// Tipo de trabajo (Tatuaje o Piercing).
    /// </summary>
    public TipoTrabajo Tipo { get; set; } = TipoTrabajo.Tatuaje;

    /// <summary>
    /// Descripción del trabajo realizado.
    /// </summary>
    public string Descripcion { get; set; } = string.Empty;

    /// <summary>
    /// Zona del cuerpo donde se realizó el trabajo.
    /// </summary>
    public string ZonaCuerpo { get; set; } = string.Empty;

    /// <summary>
    /// Estilo del tatuaje (opcional, ej: "Realismo", "Old School").
    /// </summary>
    public string? Estilo { get; set; }

    /// <summary>
    /// Tamaño del trabajo (opcional, ej: "10x15cm").
    /// </summary>
    public string? Tamano { get; set; }

    /// <summary>
    /// Indica si el trabajo tiene colores (para tatuajes).
    /// </summary>
    public bool Colores { get; set; } = false;

    #endregion

    #region Precio y Tiempo

    /// <summary>
    /// Precio cobrado por el trabajo.
    /// </summary>
    public decimal Precio { get; set; }

    /// <summary>
    /// Fecha en que se realizó el trabajo.
    /// </summary>
    public DateTime Fecha { get; set; } = DateTime.Now;

    /// <summary>
    /// Duración estimada del trabajo en minutos (lo que planifica el artista).
    /// </summary>
    public int? DuracionEstimadaMinutos { get; set; }

    /// <summary>
    /// Duración real total del trabajo en minutos (suma de las citas realizadas).
    /// </summary>
    public int? DuracionRealMinutos { get; set; }

    #endregion

    #region Estado

    /// <summary>
    /// Estado/fase actual del trabajo (Diseño, En progreso, Finalizado).
    /// </summary>
    public EstadoTrabajo Estado { get; set; } = EstadoTrabajo.Diseno;

    #endregion

    #region Fotos y Notas

    /// <summary>
    /// Rutas de las fotos en formato JSON (uso general futuro).
    /// Usar la propiedad <see cref="Fotos"/> para acceder.
    /// </summary>
    public string? FotosJson { get; set; }

    /// <summary>
    /// Ruta de la foto "antes" del trabajo (si existe).
    /// </summary>
    public string? FotoAntesPath { get; set; }

    /// <summary>
    /// Ruta de la foto "después" del trabajo (si existe).
    /// </summary>
    public string? FotoDespuesPath { get; set; }

    /// <summary>
    /// Notas adicionales sobre el trabajo (opcional).
    /// </summary>
    public string? Notas { get; set; }

    #endregion

    #region Auditoría

    /// <summary>
    /// Fecha y hora de creación del registro.
    /// </summary>
    public DateTime FechaCreacion { get; set; } = DateTime.Now;

    #endregion

    #region Navegación (Relaciones)

    /// <summary>
    /// Cliente al que se realizó el trabajo.
    /// </summary>
    public Cliente Cliente { get; set; } = null!;

    /// <summary>
    /// Citas/sesiones en las que se ha realizado este trabajo.
    /// Un trabajo puede abarcar varias citas a lo largo del tiempo.
    /// </summary>
    public List<Cita> Citas { get; set; } = new();

    /// <summary>
    /// Consentimiento asociado al trabajo (si existe).
    /// </summary>
    public Consentimiento? Consentimiento { get; set; }

    #endregion

    #region Propiedades Calculadas

    /// <summary>
    /// Lista de rutas de fotos del trabajo.
    /// Deserializa/serializa automáticamente desde/hacia FotosJson.
    /// </summary>
    public List<string> Fotos
    {
        get => string.IsNullOrEmpty(FotosJson)
            ? new List<string>()
            : JsonSerializer.Deserialize<List<string>>(FotosJson) ?? new();
        set => FotosJson = JsonSerializer.Serialize(value);
    }

    /// <summary>
    /// Emoji representativo del tipo de trabajo.
    /// </summary>
    public string IconoTipo => Tipo switch
    {
        TipoTrabajo.Tatuaje => "🎨",
        TipoTrabajo.Piercing => "💎",
        _ => "💼"
    };

    /// <summary>
    /// Indica si el trabajo tiene consentimiento firmado.
    /// </summary>
    public bool TieneConsentimiento => Consentimiento != null && Consentimiento.Firmado;

    /// <summary>
    /// Duración estimada en formato TimeSpan (si está informada).
    /// </summary>
    public TimeSpan? DuracionEstimada =>
        DuracionEstimadaMinutos.HasValue
            ? TimeSpan.FromMinutes(DuracionEstimadaMinutos.Value)
            : null;

    /// <summary>
    /// Duración real en formato TimeSpan (si está informada).
    /// </summary>
    public TimeSpan? DuracionReal =>
        DuracionRealMinutos.HasValue
            ? TimeSpan.FromMinutes(DuracionRealMinutos.Value)
            : null;

    #endregion
}
