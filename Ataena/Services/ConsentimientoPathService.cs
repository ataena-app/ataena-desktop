using System;
using System.IO;

namespace Ataena.Services;

/// <summary>
/// Servicio para gestionar las rutas de almacenamiento de consentimientos y PDFs.
/// </summary>
public static class ConsentimientoPathService
{
    private static string? _basePath;

    /// <summary>
    /// Obtiene la ruta base donde se almacenan los ficheros de Ataena.
    /// Estructura: %LOCALAPPDATA%\Ataena\ficheros\
    /// </summary>
    /// <returns>Ruta completa a la carpeta de ficheros.</returns>
    public static string ObtenerRutaBaseFicheros()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var rutaFicheros = Path.Combine(localAppData, "Ataena", "ficheros");

        // Crear la carpeta si no existe
        if (!Directory.Exists(rutaFicheros))
        {
            Directory.CreateDirectory(rutaFicheros);
        }

        return rutaFicheros;
    }

    /// <summary>
    /// Obtiene la ruta base donde se almacenan los consentimientos.
    /// DEPRECADO: Usar ObtenerRutaCarpetaCliente en su lugar.
    /// </summary>
    /// <returns>Ruta completa a la carpeta de consentimientos.</returns>
    [Obsolete("Usar ObtenerRutaCarpetaCliente en su lugar. La estructura ahora es ficheros/clientes/{id}/consentimientos/")]
    public static string ObtenerRutaBaseConsentimientos()
    {
        if (_basePath != null)
            return _basePath;

        // Usar %LOCALAPPDATA%\Ataena\consentimientos\
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _basePath = Path.Combine(localAppData, "Ataena", "consentimientos");

        // Crear la carpeta si no existe
        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
        }

        return _basePath;
    }

    /// <summary>
    /// Obtiene la ruta de la carpeta de consentimientos para un cliente específico.
    /// Estructura: %LOCALAPPDATA%\Ataena\ficheros\clientes\{id}\consentimientos\
    /// </summary>
    /// <param name="clienteId">ID del cliente.</param>
    /// <returns>Ruta completa a la carpeta de consentimientos del cliente.</returns>
    public static string ObtenerRutaCarpetaCliente(int clienteId)
    {
        var baseFicheros = ObtenerRutaBaseFicheros();
        var carpetaClientes = Path.Combine(baseFicheros, "clientes");
        var carpetaCliente = Path.Combine(carpetaClientes, clienteId.ToString());

        // Crear carpeta base del cliente si no existe
        if (!Directory.Exists(carpetaCliente))
        {
            Directory.CreateDirectory(carpetaCliente);
        }

        return carpetaCliente;
    }

    /// <summary>
    /// Obtiene la ruta de la carpeta de consentimientos para un cliente específico.
    /// Estructura: %LOCALAPPDATA%\Ataena\ficheros\clientes\{id}\consentimientos\
    /// </summary>
    public static string ObtenerRutaCarpetaConsentimientosCliente(int clienteId)
    {
        var carpetaCliente = ObtenerRutaCarpetaCliente(clienteId);
        var carpetaConsentimientos = Path.Combine(carpetaCliente, "consentimientos");

        if (!Directory.Exists(carpetaConsentimientos))
        {
            Directory.CreateDirectory(carpetaConsentimientos);
        }

        return carpetaConsentimientos;
    }

    /// <summary>
    /// Obtiene la ruta de la carpeta de un trabajo concreto de un cliente.
    /// Estructura: %LOCALAPPDATA%\Ataena\ficheros\clientes\{clienteId}\trabajos\{trabajoId}\
    /// </summary>
    public static string ObtenerRutaCarpetaTrabajo(int clienteId, int trabajoId)
    {
        var carpetaCliente = ObtenerRutaCarpetaCliente(clienteId);
        var carpetaTrabajos = Path.Combine(carpetaCliente, "trabajos");
        var carpetaTrabajo = Path.Combine(carpetaTrabajos, trabajoId.ToString());

        if (!Directory.Exists(carpetaTrabajo))
        {
            Directory.CreateDirectory(carpetaTrabajo);
        }

        return carpetaTrabajo;
    }

    /// <summary>
    /// Obtiene la ruta completa para la foto "antes" de un trabajo.
    /// </summary>
    public static string ObtenerRutaFotoAntes(int clienteId, int trabajoId)
    {
        var carpetaTrabajo = ObtenerRutaCarpetaTrabajo(clienteId, trabajoId);
        return Path.Combine(carpetaTrabajo, "antes.jpg");
    }

    /// <summary>
    /// Obtiene la ruta completa para la foto "después" de un trabajo.
    /// </summary>
    public static string ObtenerRutaFotoDespues(int clienteId, int trabajoId)
    {
        var carpetaTrabajo = ObtenerRutaCarpetaTrabajo(clienteId, trabajoId);
        return Path.Combine(carpetaTrabajo, "despues.jpg");
    }

    /// <summary>
    /// Obtiene la ruta de la carpeta de documentos de un cliente.
    /// Estructura: %LOCALAPPDATA%\Ataena\ficheros\clientes\{clienteId}\documentos\
    /// </summary>
    public static string ObtenerRutaCarpetaDocumentosCliente(int clienteId)
    {
        var carpetaCliente = ObtenerRutaCarpetaCliente(clienteId);
        var carpetaDocumentos = Path.Combine(carpetaCliente, "documentos");

        if (!Directory.Exists(carpetaDocumentos))
        {
            Directory.CreateDirectory(carpetaDocumentos);
        }

        return carpetaDocumentos;
    }

    /// <summary>
    /// Obtiene la ruta completa para la foto del DNI del cliente.
    /// </summary>
    public static string ObtenerRutaFotoDni(int clienteId)
    {
        var carpetaDocumentos = ObtenerRutaCarpetaDocumentosCliente(clienteId);
        return Path.Combine(carpetaDocumentos, "dni_cliente.jpg");
    }

    /// <summary>
    /// Obtiene la ruta completa para la foto del DNI del tutor.
    /// </summary>
    public static string ObtenerRutaFotoDniTutor(int clienteId)
    {
        var carpetaDocumentos = ObtenerRutaCarpetaDocumentosCliente(clienteId);
        return Path.Combine(carpetaDocumentos, "dni_tutor.jpg");
    }

    /// <summary>
    /// Devuelve la ruta de la foto del DNI del cliente si existe en disco (ruta guardada o ubicación estándar).
    /// </summary>
    public static string? RutaFotoDniExistente(int clienteId, string? rutaGuardada)
    {
        if (!string.IsNullOrWhiteSpace(rutaGuardada) && File.Exists(rutaGuardada))
            return rutaGuardada;

        var estandar = ObtenerRutaFotoDni(clienteId);
        return File.Exists(estandar) ? estandar : null;
    }

    /// <summary>
    /// Devuelve la ruta de la foto del DNI del tutor si existe en disco (ruta guardada o ubicación estándar).
    /// </summary>
    public static string? RutaFotoDniTutorExistente(int clienteId, string? rutaGuardada)
    {
        if (!string.IsNullOrWhiteSpace(rutaGuardada) && File.Exists(rutaGuardada))
            return rutaGuardada;

        var estandar = ObtenerRutaFotoDniTutor(clienteId);
        return File.Exists(estandar) ? estandar : null;
    }

    /// <summary>
    /// Obtiene la ruta de la carpeta para un tipo específico de consentimiento.
    /// DEPRECADO: Usar ObtenerRutaCarpetaCliente en su lugar.
    /// </summary>
    /// <param name="tipoConsentimiento">Tipo de consentimiento (RGPD, Imagenes, Trabajo).</param>
    /// <returns>Ruta completa a la carpeta del tipo de consentimiento.</returns>
    [Obsolete("Usar ObtenerRutaCarpetaCliente en su lugar. La estructura ahora es por cliente, no por tipo.")]
    public static string ObtenerRutaCarpetaTipo(Models.TipoConsentimiento tipoConsentimiento)
    {
        var basePath = ObtenerRutaBaseConsentimientos();
        var tipoFolder = tipoConsentimiento switch
        {
            Models.TipoConsentimiento.RGPD => "RGPD",
            Models.TipoConsentimiento.RGPD_Menor => "RGPD",
            Models.TipoConsentimiento.Imagenes => "Imagenes",
            Models.TipoConsentimiento.Imagenes_Menor => "Imagenes",
            Models.TipoConsentimiento.Trabajo => "Trabajos",
            Models.TipoConsentimiento.Trabajo_Menor => "Trabajos",
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
            Models.TipoConsentimiento.RGPD_Menor => "rgpd_menor",
            Models.TipoConsentimiento.Imagenes => "imagenes",
            Models.TipoConsentimiento.Imagenes_Menor => "imagenes_menor",
            Models.TipoConsentimiento.Trabajo => "trabajo",
            Models.TipoConsentimiento.Trabajo_Menor => "trabajo_menor",
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
    /// La estructura es: %LOCALAPPDATA%\Ataena\ficheros\clientes\{id}\consentimientos\nombre-archivo.pdf
    /// </summary>
    /// <param name="clienteId">ID del cliente.</param>
    /// <param name="tipoConsentimiento">Tipo de consentimiento.</param>
    /// <param name="trabajoId">ID del trabajo (opcional).</param>
    /// <returns>Ruta completa al archivo PDF.</returns>
    public static string ObtenerRutaCompletaPdf(int clienteId, Models.TipoConsentimiento tipoConsentimiento, int? trabajoId = null)
    {
        var carpetaCliente = ObtenerRutaCarpetaConsentimientosCliente(clienteId);
        var nombreArchivo = GenerarNombreArchivo(clienteId, tipoConsentimiento, trabajoId);
        return Path.Combine(carpetaCliente, nombreArchivo);
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

