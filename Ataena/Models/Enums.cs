namespace Ataena.Models;

/// <summary>
/// Tipo de cita que se puede agendar en el estudio.
/// </summary>
public enum TipoCita
{
    /// <summary>
    /// Cita para realizar un tatuaje.
    /// </summary>
    Tatuaje = 0,

    /// <summary>
    /// Cita para realizar un piercing.
    /// </summary>
    Piercing = 1,

    /// <summary>
    /// Consulta inicial o de diseño (sin trabajo).
    /// </summary>
    Consulta = 2,

    /// <summary>
    /// Retoque de un trabajo existente.
    /// </summary>
    Retoque = 3
}

/// <summary>
/// Estado del ciclo de vida de una cita.
/// </summary>
public enum EstadoCita
{
    /// <summary>
    /// Cita creada, pendiente de confirmación.
    /// </summary>
    Pendiente = 0,

    /// <summary>
    /// Cita confirmada por el cliente.
    /// </summary>
    Confirmada = 1,

    /// <summary>
    /// El trabajo está en curso.
    /// </summary>
    EnProceso = 2,

    /// <summary>
    /// Cita completada satisfactoriamente.
    /// </summary>
    Completada = 3,

    /// <summary>
    /// Cita cancelada (por cliente o estudio).
    /// </summary>
    Cancelada = 4,

    /// <summary>
    /// El cliente no se presentó (No Show).
    /// </summary>
    NoShow = 5
}

/// <summary>
/// Tipo de trabajo realizado en el estudio.
/// </summary>
public enum TipoTrabajo
{
    /// <summary>
    /// Trabajo de tatuaje.
    /// </summary>
    Tatuaje = 0,

    /// <summary>
    /// Trabajo de piercing.
    /// </summary>
    Piercing = 1
}

/// <summary>
/// Estado/fase de un trabajo en el estudio.
/// </summary>
public enum EstadoTrabajo
{
    /// <summary>
    /// Trabajo en fase de diseño / planificación.
    /// </summary>
    Diseno = 0,

    /// <summary>
    /// Trabajo ya iniciado, con sesiones en curso.
    /// </summary>
    EnProgreso = 1,

    /// <summary>
    /// Trabajo finalizado, no se esperan más sesiones.
    /// </summary>
    Finalizado = 2
}

/// <summary>
/// Tipo de consentimiento que debe firmar el cliente.
/// </summary>
/// <remarks>
/// - RGPD: Obligatorio para cumplir con la protección de datos (mayores de edad).
/// - RGPD_Menor: Para menores de edad, requiere doble firma (menor + tutor).
/// - Imagenes: Opcional, para usar fotos en redes sociales (mayores de edad).
/// - Imagenes_Menor: Uso de imágenes del menor; doble firma (menor + tutor).
/// - Trabajo: Obligatorio antes de cada tatuaje/piercing (mayores de edad).
/// - Trabajo_Menor: Para menores de edad, requiere doble firma (menor + tutor).
/// </remarks>
public enum TipoConsentimiento
{
    /// <summary>
    /// Consentimiento RGPD (protección de datos personales).
    /// Se firma una vez por cliente mayor de edad.
    /// </summary>
    RGPD = 0,

    /// <summary>
    /// Consentimiento de uso de imágenes en redes sociales.
    /// Se firma una vez por cliente mayor de edad.
    /// </summary>
    Imagenes = 1,

    /// <summary>
    /// Consentimiento específico para un trabajo.
    /// Se firma antes de cada tatuaje/piercing (mayores de edad).
    /// </summary>
    Trabajo = 2,

    /// <summary>
    /// Consentimiento RGPD para menores de edad.
    /// Requiere doble firma: menor + tutor/representante legal.
    /// </summary>
    RGPD_Menor = 3,

    /// <summary>
    /// Consentimiento de trabajo para menores de edad.
    /// Requiere doble firma: menor + tutor/representante legal.
    /// </summary>
    Trabajo_Menor = 4,

    /// <summary>
    /// Consentimiento de uso de imágenes del menor.
    /// Requiere doble firma: menor + tutor/representante legal.
    /// </summary>
    Imagenes_Menor = 5
}

/// <summary>
/// Criterio de ordenación de la lista de clientes.
/// </summary>
public enum OrdenClientes
{
    /// <summary>Registrados hace más tiempo primero.</summary>
    MasAntiguo = 0,

    /// <summary>Últimos registrados primero.</summary>
    MasReciente = 1,

    /// <summary>Alfabético por nombre y apellidos.</summary>
    Nombre = 2,

    /// <summary>Mayor edad primero.</summary>
    Edad = 3
}
