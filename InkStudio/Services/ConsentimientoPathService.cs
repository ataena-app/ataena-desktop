using System;
using System.IO;

namespace InkStudio.Services;

/// <summary>
/// Servicio para gestionar las rutas de almacenamiento de consentimientos y PDFs.
/// </summary>
public static class ConsentimientoPathService
{
    private static string? _basePath;

    /// <summary>
    /// Obtiene la ruta base donde se almacenan los consentimientos.
    /// </summary>
    /// <returns>Ruta completa a la carpeta de consentimientos.</returns>
    public static string ObtenerRutaBaseConsentimientos()
    {
        if (_basePath != null)
            return _basePath;

        // Usar %LOCALAPPDATA%\InkStudio\consentimientos\
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _basePath = Path.Combine(localAppData, "InkStudio", "consentimientos");

        // Crear la carpeta si no existe
        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
        }

        return _basePath;
    }

    /// <summary>
    /// Obtiene la ruta de la carpeta para un tipo específico de consentimiento.
    /// </summary>
    /// <param name="tipoConsentimiento">Tipo de consentimiento (RGPD, Imagenes, Trabajo).</param>
    /// <returns>Ruta completa a la carpeta del tipo de consentimiento.</returns>
    public static string ObtenerRutaCarpetaTipo(Models.TipoConsentimiento tipoConsentimiento)
    {
        var basePath = ObtenerRutaBaseConsentimientos();
        var tipoFolder = tipoConsentimiento switch
        {
            Models.TipoConsentimiento.RGPD => "RGPD",
            Models.TipoConsentimiento.Imagenes => "Imagenes",
            Models.TipoConsentimiento.Trabajo => "Trabajos",
            _ => "Otros"
        };

        var ruta = Path.Combine(basePath, tipoFolder);

        // Crear la carpeta si no existe
        if (!Directory.Exists(ruta))
        {
            Directory.CreateDirectory(ruta);
        }

        return ruta;
    }

    /// <summary>
    /// Genera un nombre de archivo único para un consentimiento.
    /// </summary>
    /// <param name="clienteId">ID del cliente.</param>
    /// <param name="tipoConsentimiento">Tipo de consentimiento.</param>
    /// <param name="trabajoId">ID del trabajo (opcional, solo para consentimientos de trabajo).</param>
    /// <returns>Nombre de archivo único con formato: cliente-{id}_{tipo}_{fecha}_{hora}.pdf</returns>
    public static string GenerarNombreArchivo(int clienteId, Models.TipoConsentimiento tipoConsentimiento, int? trabajoId = null)
    {
        var fecha = DateTime.Now;
        var tipoStr = tipoConsentimiento switch
        {
            Models.TipoConsentimiento.RGPD => "rgpd",
            Models.TipoConsentimiento.Imagenes => "imagenes",
            Models.TipoConsentimiento.Trabajo => "trabajo",
            _ => "otro"
        };

        var fechaStr = fecha.ToString("yyyyMMdd");
        var horaStr = fecha.ToString("HHmmss");

        if (trabajoId.HasValue)
        {
            return $"cliente-{clienteId}_{tipoStr}_trabajo-{trabajoId.Value}_{fechaStr}_{horaStr}.pdf";
        }

        return $"cliente-{clienteId}_{tipoStr}_{fechaStr}_{horaStr}.pdf";
    }

    /// <summary>
    /// Obtiene la ruta completa de un archivo PDF de consentimiento.
    /// </summary>
    /// <param name="clienteId">ID del cliente.</param>
    /// <param name="tipoConsentimiento">Tipo de consentimiento.</param>
    /// <param name="trabajoId">ID del trabajo (opcional).</param>
    /// <returns>Ruta completa al archivo PDF.</returns>
    public static string ObtenerRutaCompletaPdf(int clienteId, Models.TipoConsentimiento tipoConsentimiento, int? trabajoId = null)
    {
        var carpetaTipo = ObtenerRutaCarpetaTipo(tipoConsentimiento);
        var nombreArchivo = GenerarNombreArchivo(clienteId, tipoConsentimiento, trabajoId);
        return Path.Combine(carpetaTipo, nombreArchivo);
    }

    /// <summary>
    /// Obtiene la ruta de la carpeta de plantillas de texto.
    /// </summary>
    /// <returns>Ruta completa a la carpeta de plantillas.</returns>
    public static string ObtenerRutaPlantillas()
    {
        // En desarrollo, usar la carpeta del proyecto
        // En producción, usar la carpeta de ejecución
        var appDirectory = AppContext.BaseDirectory;
        var rutaPlantillas = Path.Combine(appDirectory, "Plantillas");

        // Si no existe en la carpeta de ejecución, intentar la carpeta del proyecto (para desarrollo)
        if (!Directory.Exists(rutaPlantillas))
        {
            var proyectoPath = Path.GetDirectoryName(typeof(ConsentimientoPathService).Assembly.Location);
            if (proyectoPath != null)
            {
                var rutaAlternativa = Path.Combine(proyectoPath, "..", "..", "..", "..", "Plantillas");
                rutaAlternativa = Path.GetFullPath(rutaAlternativa);
                if (Directory.Exists(rutaAlternativa))
                {
                    rutaPlantillas = rutaAlternativa;
                }
            }
        }

        return rutaPlantillas;
    }

    /// <summary>
    /// Obtiene la ruta de la carpeta wwwroot para archivos web estáticos.
    /// </summary>
    /// <returns>Ruta completa a la carpeta wwwroot.</returns>
    public static string ObtenerRutaWwwRoot()
    {
        // En desarrollo, usar la carpeta del proyecto
        // En producción, usar la carpeta de ejecución
        var appDirectory = AppContext.BaseDirectory;
        var rutaWwwRoot = Path.Combine(appDirectory, "wwwroot");

        // Si no existe en la carpeta de ejecución, intentar la carpeta del proyecto (para desarrollo)
        if (!Directory.Exists(rutaWwwRoot))
        {
            var proyectoPath = Path.GetDirectoryName(typeof(ConsentimientoPathService).Assembly.Location);
            if (proyectoPath != null)
            {
                var rutaAlternativa = Path.Combine(proyectoPath, "..", "..", "..", "..", "wwwroot");
                rutaAlternativa = Path.GetFullPath(rutaAlternativa);
                if (Directory.Exists(rutaAlternativa))
                {
                    rutaWwwRoot = rutaAlternativa;
                }
            }
        }

        return rutaWwwRoot;
    }
}

