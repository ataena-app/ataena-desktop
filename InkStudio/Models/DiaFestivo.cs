using System;
using System.ComponentModel.DataAnnotations;

namespace InkStudio.Models;

/// <summary>
/// Representa un día festivo en el calendario.
/// Puede ser nacional, autonómico o local.
/// </summary>
public class DiaFestivo
{
    /// <summary>
    /// Identificador único del festivo.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Fecha del día festivo.
    /// </summary>
    [Required]
    public DateTime Fecha { get; set; }

    /// <summary>
    /// Nombre del festivo en español.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Nombre del festivo en inglés (opcional, de la API).
    /// </summary>
    [MaxLength(100)]
    public string? NombreIngles { get; set; }

    /// <summary>
    /// Tipo de festivo (Nacional, Autonómico, Local).
    /// </summary>
    public TipoFestivo Tipo { get; set; } = TipoFestivo.Nacional;

    /// <summary>
    /// Indica si el festivo está activo/habilitado.
    /// </summary>
    public bool Activo { get; set; } = true;

    /// <summary>
    /// Indica si es un festivo personalizado (añadido manualmente).
    /// Los festivos de API tienen este campo en false.
    /// </summary>
    public bool EsPersonalizado { get; set; } = false;

    /// <summary>
    /// Código de subdivisión (ej: "CM" para Castilla-La Mancha).
    /// Null para festivos nacionales.
    /// </summary>
    [MaxLength(10)]
    public string? CodigoSubdivision { get; set; }

    /// <summary>
    /// Año del festivo (para búsquedas rápidas).
    /// </summary>
    public int Anio { get; set; }

    /// <summary>
    /// Indica si es un día festivo fijo (misma fecha cada año) o variable (ej: Semana Santa).
    /// </summary>
    public bool EsFijo { get; set; } = true;

    /// <summary>
    /// Color de fondo para mostrar en el calendario (hex, ej: "#ef4444").
    /// </summary>
    [MaxLength(10)]
    public string ColorFondo { get; set; } = "#ef4444";

    /// <summary>
    /// Notas adicionales sobre el festivo.
    /// </summary>
    [MaxLength(500)]
    public string? Notas { get; set; }
}

/// <summary>
/// Tipo de día festivo según su ámbito.
/// </summary>
public enum TipoFestivo
{
    /// <summary>
    /// Festivo nacional (toda España).
    /// </summary>
    Nacional = 0,

    /// <summary>
    /// Festivo autonómico (Castilla-La Mancha).
    /// </summary>
    Autonomico = 1,

    /// <summary>
    /// Festivo local (Guadalajara ciudad).
    /// </summary>
    Local = 2
}
