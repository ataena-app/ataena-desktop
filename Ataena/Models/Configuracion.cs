namespace Ataena.Models;

/// <summary>
/// Entidad singleton que almacena la configuración del estudio.
/// Solo existe un registro (Id = 1) en la base de datos.
/// </summary>
/// <remarks>
/// Incluye datos del estudio, configuración SMTP para emails
/// y preferencias de la aplicación.
/// </remarks>
public class Configuracion
{
    #region Identificación

    /// <summary>
    /// Identificador único. Siempre es 1 (singleton).
    /// </summary>
    public int Id { get; set; } = 1;

    #endregion

    #region Datos del Estudio

    /// <summary>
    /// Nombre del estudio de tatuajes (nombre comercial).
    /// Ej: "Erzulie Tattoo"
    /// </summary>
    public string NombreEstudio { get; set; } = "Mi Estudio";

    /// <summary>
    /// Nombre de la empresa o razón social (para documentos legales).
    /// Ej: "Alfredo Y Rebeca C.B"
    /// </summary>
    public string? NombreEmpresa { get; set; }

    /// <summary>
    /// Dirección física del estudio (opcional).
    /// </summary>
    public string? Direccion { get; set; }

    /// <summary>
    /// Teléfono de contacto del estudio (opcional).
    /// </summary>
    public string? Telefono { get; set; }

    /// <summary>
    /// Email de contacto del estudio (opcional).
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Ruta al archivo del logo del estudio (opcional).
    /// </summary>
    public string? LogoPath { get; set; }

    #endregion

    #region Normativa CCAA (DNI)

    /// <summary>
    /// Perfil normativo para captura y archivo del DNI.
    /// Por defecto estándar (CLM): solo datos en texto, sin imagen en ficha.
    /// </summary>
    public PerfilNormativoDni PerfilNormativoDni { get; set; } = PerfilNormativoDni.Estandar;

    #endregion

    #region Configuración SMTP (Email)

    /// <summary>
    /// Servidor SMTP para envío de emails.
    /// Ej: "smtp.gmail.com"
    /// </summary>
    public string? SmtpServidor { get; set; }

    /// <summary>
    /// Puerto del servidor SMTP.
    /// Valor por defecto: 587 (TLS).
    /// </summary>
    public int SmtpPuerto { get; set; } = 587;

    /// <summary>
    /// Usuario para autenticación SMTP.
    /// </summary>
    public string? SmtpUsuario { get; set; }

    /// <summary>
    /// Contraseña para autenticación SMTP.
    /// </summary>
    /// <remarks>
    /// TODO: En producción debería estar cifrada.
    /// </remarks>
    public string? SmtpPassword { get; set; }

    /// <summary>
    /// Indica si usar SSL/TLS para la conexión SMTP.
    /// </summary>
    public bool SmtpUsarSsl { get; set; } = true;

    #endregion

    #region Escáner e Impresora

    /// <summary>
    /// Obsoleto: el escaneo de DNI no forma parte del producto. Se conserva en BD por compatibilidad.
    /// </summary>
    public bool UsarEscanner { get; set; } = false;

    /// <summary>
    /// Habilita la opción de imprimir consentimientos con la impresora predeterminada de Windows.
    /// </summary>
    public bool UsarImpresora { get; set; } = false;

    #endregion

    #region Preferencias de la Aplicación

    /// <summary>
    /// Indica si usar tema oscuro en la UI.
    /// </summary>
    public bool TemaOscuro { get; set; } = true;

    /// <summary>
    /// Código de idioma de la aplicación.
    /// Valor por defecto: "es" (español).
    /// </summary>
    public string IdiomaApp { get; set; } = "es";

    #endregion

    #region Configuración de Backup

    /// <summary>
    /// Servicio de nube configurado para backups.
    /// </summary>
    public ServicioNube? BackupServicioNube { get; set; }

    /// <summary>
    /// Ruta de la carpeta de nube para backups.
    /// </summary>
    public string? BackupRutaNube { get; set; }

    /// <summary>
    /// Indica si copiar automáticamente los backups a la nube.
    /// </summary>
    public bool BackupCopiarAutomaticamenteNube { get; set; } = true;

    /// <summary>
    /// Indica si el backup automático está activado.
    /// </summary>
    public bool BackupAutomaticoActivo { get; set; } = false;

    /// <summary>
    /// Frecuencia del backup automático.
    /// 0 = Diario, 1 = Semanal, 2 = Mensual
    /// </summary>
    public int BackupFrecuencia { get; set; } = 0; // Diario por defecto

    /// <summary>
    /// Hora del día para el backup automático (en minutos desde medianoche).
    /// Por defecto: 14:00 (840 minutos).
    /// </summary>
    public int BackupHora { get; set; } = 840; // 14:00

    /// <summary>
    /// Número de backups a mantener (los más antiguos se eliminan).
    /// </summary>
    public int BackupMantenerUltimos { get; set; } = 10;

    #endregion

    #region Preferencias del Dashboard

    /// <summary>
    /// Muestra la tarjeta de ingresos económicos en el Dashboard.
    /// Desactivar si no se quiere hacer seguimiento económico.
    /// </summary>
    public bool DashboardMostrarEconomia { get; set; } = false;

    /// <summary>
    /// Muestra la fila de tarjetas de estadísticas (clientes, citas hoy, por confirmar).
    /// </summary>
    public bool DashboardMostrarEstadisticas { get; set; } = true;

    /// <summary>
    /// Muestra el panel de alertas y pendientes en el Dashboard.
    /// </summary>
    public bool DashboardMostrarAlertas { get; set; } = true;

    /// <summary>
    /// Muestra los botones de acciones rápidas (nuevo cliente, nueva cita, nuevo trabajo).
    /// </summary>
    public bool DashboardMostrarAccionesRapidas { get; set; } = true;

    #endregion

    #region Propiedades Calculadas

    /// <summary>
    /// Indica si el SMTP está configurado correctamente.
    /// Requiere servidor y usuario como mínimo.
    /// </summary>
    public bool SmtpConfigurado => !string.IsNullOrEmpty(SmtpServidor) &&
                                   !string.IsNullOrEmpty(SmtpUsuario);

    #endregion
}
