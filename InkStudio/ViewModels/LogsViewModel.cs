using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InkStudio.Services;
using Serilog;

namespace InkStudio.ViewModels;

/// <summary>
/// ViewModel para la visualización y gestión de logs.
/// Permite ver logs recientes y exportarlos.
/// </summary>
public partial class LogsViewModel : ViewModelBase
{
    #region Propiedades

    /// <summary>
    /// Contenido del log actual mostrado.
    /// </summary>
    [ObservableProperty]
    private string _contenidoLog = string.Empty;

    /// <summary>
    /// Lista de archivos de log disponibles.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<string> _archivosLog = new();

    /// <summary>
    /// Archivo de log seleccionado.
    /// </summary>
    [ObservableProperty]
    private string? _archivoSeleccionado;

    /// <summary>
    /// Indica si se está cargando el log.
    /// </summary>
    [ObservableProperty]
    private bool _cargando = false;

    /// <summary>
    /// Mensaje de estado o error.
    /// </summary>
    [ObservableProperty]
    private string _mensaje = string.Empty;

    /// <summary>
    /// Ruta de la carpeta de logs.
    /// </summary>
    [ObservableProperty]
    private string _rutaCarpetaLogs = string.Empty;

    #endregion

    #region Constructor

    /// <summary>
    /// Inicializa el ViewModel y carga los logs disponibles.
    /// </summary>
    public LogsViewModel()
    {
        RutaCarpetaLogs = LoggingService.LogsFolder;
        CargarArchivosLog();
    }

    #endregion

    #region Comandos

    /// <summary>
    /// Carga la lista de archivos de log disponibles.
    /// </summary>
    [RelayCommand]
    private void CargarArchivosLog()
    {
        try
        {
            ArchivosLog.Clear();
            Mensaje = string.Empty;

            if (!Directory.Exists(LoggingService.LogsFolder))
            {
                Mensaje = "La carpeta de logs no existe";
                return;
            }

            var archivos = Directory.GetFiles(LoggingService.LogsFolder, "inkstudio-*.log")
                .OrderByDescending(f => File.GetLastWriteTime(f))
                .Select(f => Path.GetFileName(f))
                .ToList();

            foreach (var archivo in archivos)
            {
                ArchivosLog.Add(archivo);
            }

            // Seleccionar el más reciente por defecto
            if (archivos.Count > 0)
            {
                ArchivoSeleccionado = archivos[0];
                CargarLogCommand.Execute(null);
            }

            Log.Information("Archivos de log cargados: {Count} archivos", archivos.Count);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al cargar archivos de log");
            Mensaje = $"Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Carga el contenido del log seleccionado.
    /// </summary>
    [RelayCommand]
    private async Task CargarLog()
    {
        Log.Information("═══════════════════════════════════════════════════════");
        Log.Information("[CargarLog] INICIO - Archivo: {Archivo}", ArchivoSeleccionado);
        
        if (string.IsNullOrEmpty(ArchivoSeleccionado))
        {
            Log.Warning("[CargarLog] ERROR: ArchivoSeleccionado está vacío");
            return;
        }

        try
        {
            Cargando = true;
            Mensaje = string.Empty;

            var rutaCompleta = Path.Combine(LoggingService.LogsFolder, ArchivoSeleccionado);
            Log.Information("[CargarLog] Ruta completa: {Ruta}", rutaCompleta);
            Log.Information("[CargarLog] LogsFolder: {Folder}", LoggingService.LogsFolder);

            if (!File.Exists(rutaCompleta))
            {
                Log.Warning("[CargarLog] ERROR: El archivo no existe: {Ruta}", rutaCompleta);
                Mensaje = "El archivo de log no existe";
                Cargando = false;
                return;
            }

            // Verificar información del archivo
            var fileInfo = new FileInfo(rutaCompleta);
            Log.Information("[CargarLog] Tamaño del archivo: {Tamaño} bytes", fileInfo.Length);
            Log.Information("[CargarLog] Última modificación: {Fecha}", fileInfo.LastWriteTime);
            Log.Information("[CargarLog] Atributos: {Atributos}", fileInfo.Attributes);

            // Método especial: leer desde el final del archivo hacia atrás
            // Esto evita tener que abrir todo el archivo y reduce conflictos con Serilog
            var lineas = new List<string>();
            const int maxIntentos = 10;
            const int delayMs = 100;
            const int maxLineas = 1000; // Solo necesitamos las últimas 1000 líneas

            Log.Information("[CargarLog] Iniciando lectura desde el final del archivo (máximo {MaxIntentos} intentos)", maxIntentos);

            for (int intento = 1; intento <= maxIntentos; intento++)
            {
                Log.Information("[CargarLog] Intento {Intento}/{MaxIntentos}", intento, maxIntentos);
                try
                {
                    Log.Information("[CargarLog] Intentando abrir FileStream con FileShare.ReadWrite | FileShare.Delete");
                    
                    // Abrir con FileShare.ReadWrite | FileShare.Delete para permitir acceso compartido completo
                    using (var fileStream = new FileStream(
                        rutaCompleta, 
                        FileMode.Open, 
                        FileAccess.Read, 
                        FileShare.ReadWrite | FileShare.Delete))
                    {
                        Log.Information("[CargarLog] FileStream abierto exitosamente. Longitud: {Longitud}", fileStream.Length);
                        
                        // Leer desde el final del archivo hacia atrás
                        lineas = await LeerUltimasLineasDesdeFinal(fileStream, maxLineas);
                        
                        Log.Information("[CargarLog] Total de líneas leídas: {Contador}", lineas.Count);
                    }
                    Log.Information("[CargarLog] ✅ ÉXITO en intento {Intento}", intento);
                    break; // Éxito
                }
                catch (IOException ex) when (intento < maxIntentos)
                {
                    Log.Warning(ex, "[CargarLog] ❌ IOException en intento {Intento}: {Tipo} - {Mensaje}", intento, ex.GetType().Name, ex.Message);
                    // Esperar antes de reintentar
                    Log.Information("[CargarLog] Esperando {Delay}ms antes de reintentar...", delayMs * intento);
                    await Task.Delay(delayMs * intento);
                    lineas.Clear();
                }
                catch (UnauthorizedAccessException ex) when (intento < maxIntentos)
                {
                    Log.Warning(ex, "[CargarLog] ❌ UnauthorizedAccessException en intento {Intento}: {Mensaje}", intento, ex.Message);
                    // También reintentar en caso de permisos
                    Log.Information("[CargarLog] Esperando {Delay}ms antes de reintentar...", delayMs * intento);
                    await Task.Delay(delayMs * intento);
                    lineas.Clear();
                }
                catch (Exception ex) when (intento < maxIntentos)
                {
                    Log.Warning(ex, "[CargarLog] ❌ Excepción inesperada en intento {Intento}: {Tipo} - {Mensaje}", intento, ex.GetType().Name, ex.Message);
                    Log.Information("[CargarLog] Esperando {Delay}ms antes de reintentar...", delayMs * intento);
                    await Task.Delay(delayMs * intento);
                    lineas.Clear();
                }
            }

            Log.Information("[CargarLog] Después del bucle. Líneas leídas: {Count}", lineas.Count);

            if (lineas.Count == 0)
            {
                Log.Warning("[CargarLog] ⚠️ No se leyeron líneas. Intentando método alternativo...");
                // Si falla todo, intentar leer usando File.ReadAllText con manejo de errores
                try
                {
                    var contenido = await Task.Run(() =>
                    {
                        Log.Information("[CargarLog] Método alternativo: Intentando leer con ReadToEnd...");
                        // Intentar leer directamente con FileShare explícito
                        using (var fs = new FileStream(rutaCompleta, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                        using (var sr = new StreamReader(fs))
                        {
                            var texto = sr.ReadToEnd();
                            Log.Information("[CargarLog] Método alternativo: Leídos {Caracteres} caracteres", texto.Length);
                            return texto;
                        }
                    });

                    lineas = contenido.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None)
                        .Where(l => !string.IsNullOrEmpty(l))
                        .ToList();
                    Log.Information("[CargarLog] Método alternativo: ✅ Éxito. {Count} líneas procesadas", lineas.Count);
                }
                catch (Exception ex2)
                {
                    Log.Error(ex2, "[CargarLog] ❌❌ Método alternativo también falló: {Tipo} - {Mensaje}", ex2.GetType().Name, ex2.Message);
                    Mensaje = $"No se pudo leer el archivo después de {maxIntentos} intentos. El archivo puede estar bloqueado por otro proceso.\n\nDetalle: {ex2.Message}";
                    ContenidoLog = string.Empty;
                    Cargando = false;
                    return;
                }
            }

            // Mostrar las últimas 1000 líneas para no sobrecargar la UI
            var lineasMostrar = lineas.Count > 1000 
                ? lineas.Skip(lineas.Count - 1000).ToArray() 
                : lineas.ToArray();

            Log.Information("[CargarLog] Preparando contenido para mostrar. Total: {Total}, Mostrando: {Mostrando}", lineas.Count, lineasMostrar.Length);

            ContenidoLog = string.Join(Environment.NewLine, lineasMostrar);

            if (lineas.Count > 1000)
            {
                Mensaje = $"Mostrando últimas 1000 líneas de {lineas.Count} totales";
            }
            else
            {
                Mensaje = $"Log cargado: {lineas.Count} líneas";
            }

            Log.Information("[CargarLog] ✅✅ ÉXITO FINAL: {Count} líneas cargadas", lineas.Count);
            Log.Debug("Log cargado: {Archivo}, {Lineas} líneas", ArchivoSeleccionado, lineas.Count);
        }
        catch (UnauthorizedAccessException ex)
        {
            Log.Error(ex, "[CargarLog] ❌❌ UnauthorizedAccessException capturada: {Mensaje}", ex.Message);
            Mensaje = $"Error de permisos: {ex.Message}";
            ContenidoLog = string.Empty;
        }
        catch (IOException ex)
        {
            Log.Error(ex, "[CargarLog] ❌❌ IOException capturada: {Tipo} - {Mensaje}, HResult: {HResult}", ex.GetType().Name, ex.Message, ex.HResult);
            Mensaje = $"Error al leer el archivo: {ex.Message}\n\nSugerencia: Cierra cualquier programa que pueda tener el archivo abierto.";
            ContenidoLog = string.Empty;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[CargarLog] ❌❌ Excepción general capturada: {Tipo} - {Mensaje}", ex.GetType().Name, ex.Message);
            Mensaje = $"Error al cargar el log: {ex.Message}";
            ContenidoLog = string.Empty;
        }
        finally
        {
            Log.Information("[CargarLog] FIN - Cargando = false");
            Cargando = false;
            Log.Information("═══════════════════════════════════════════════════════");
        }
    }

    /// <summary>
    /// Copia el contenido del log al portapapeles.
    /// </summary>
    [RelayCommand]
    private async Task CopiarLog()
    {
        try
        {
            if (string.IsNullOrEmpty(ContenidoLog))
            {
                Mensaje = "No hay contenido para copiar";
                return;
            }

            // TODO: Implementar copia al portapapeles en Avalonia
            // Por ahora, guardamos en un archivo temporal
            var tempFile = Path.Combine(Path.GetTempPath(), $"inkstudio-log-{DateTime.Now:yyyyMMddHHmmss}.txt");
            await File.WriteAllTextAsync(tempFile, ContenidoLog);
            
            Mensaje = $"Log copiado a: {tempFile}";
            Log.Information("Log copiado a archivo temporal: {Archivo}", tempFile);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al copiar el log");
            Mensaje = $"Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Abre la carpeta de logs en el explorador de archivos.
    /// </summary>
    [RelayCommand]
    private void AbrirCarpetaLogs()
    {
        LoggingService.AbrirCarpetaLogs();
        Mensaje = "Carpeta de logs abierta";
    }

    /// <summary>
    /// Exporta el log actual a un archivo seleccionado por el usuario.
    /// </summary>
    [RelayCommand]
    private Task ExportarLog()
    {
        // TODO: Implementar diálogo de guardar archivo en Avalonia
        // Por ahora, usamos la carpeta de documentos
        try
        {
            if (string.IsNullOrEmpty(ArchivoSeleccionado))
            {
                Mensaje = "No hay log seleccionado";
                return Task.CompletedTask;
            }

            var documentos = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var nombreArchivo = $"inkstudio-log-export-{DateTime.Now:yyyyMMddHHmmss}.txt";
            var rutaDestino = Path.Combine(documentos, nombreArchivo);

            var rutaOrigen = Path.Combine(LoggingService.LogsFolder, ArchivoSeleccionado);
            
            // Copiar el archivo usando FileShare.ReadWrite para evitar bloqueos
            using (var sourceStream = new FileStream(rutaOrigen, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var destStream = new FileStream(rutaDestino, FileMode.Create, FileAccess.Write))
            {
                sourceStream.CopyTo(destStream);
            }

            Mensaje = $"Log exportado a: {rutaDestino}";
            Log.Information("Log exportado: {Archivo}", rutaDestino);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al exportar el log");
            Mensaje = $"Error: {ex.Message}";
        }
        
        return Task.CompletedTask;
    }

    /// <summary>
    /// Lee las últimas líneas de un archivo desde el final hacia atrás.
    /// Esto es más eficiente y evita conflictos con procesos que están escribiendo.
    /// </summary>
    private async Task<List<string>> LeerUltimasLineasDesdeFinal(FileStream fileStream, int maxLineas)
    {
        var lineas = new List<string>();
        var buffer = new List<byte>();
        var lineasEncontradas = 0;
        
        // Empezar desde el final del archivo
        long posicion = fileStream.Length;
        const int bufferSize = 4096; // Leer en bloques de 4KB
        
        Log.Debug("[LeerUltimasLineasDesdeFinal] Iniciando lectura desde posición {Posicion}", posicion);
        
        while (posicion > 0 && lineasEncontradas < maxLineas)
        {
            // Calcular cuánto leer en este bloque
            int bytesALeer = (int)Math.Min(bufferSize, posicion);
            posicion -= bytesALeer;
            
            // Leer el bloque
            fileStream.Seek(posicion, SeekOrigin.Begin);
            var bloque = new byte[bytesALeer];
            int bytesLeidos = 0;
            while (bytesLeidos < bytesALeer)
            {
                int leidos = await fileStream.ReadAsync(bloque, bytesLeidos, bytesALeer - bytesLeidos);
                if (leidos == 0) break; // Fin del archivo
                bytesLeidos += leidos;
            }
            
            // Procesar el bloque de atrás hacia adelante
            for (int i = bloque.Length - 1; i >= 0; i--)
            {
                buffer.Insert(0, bloque[i]);
                
                // Si encontramos un salto de línea
                if (bloque[i] == '\n' || (bloque[i] == '\r' && (i == 0 || bloque[i - 1] != '\n')))
                {
                    if (buffer.Count > 1) // Ignorar líneas vacías al final
                    {
                        // Convertir buffer a string (sin el \n o \r\n)
                        var bytesLinea = buffer.Skip(1).ToList(); // Saltar el \n
                        if (bytesLinea.Count > 0 && bytesLinea.Last() == '\r')
                        {
                            bytesLinea.RemoveAt(bytesLinea.Count - 1); // Remover \r si existe
                        }
                        
                        if (bytesLinea.Count > 0)
                        {
                            var linea = System.Text.Encoding.UTF8.GetString(bytesLinea.ToArray());
                            lineas.Insert(0, linea); // Insertar al principio para mantener orden
                            lineasEncontradas++;
                            
                            if (lineasEncontradas >= maxLineas)
                                break;
                        }
                    }
                    buffer.Clear();
                }
            }
        }
        
        // Procesar cualquier línea restante al principio del archivo
        if (buffer.Count > 0 && lineasEncontradas < maxLineas)
        {
            var bytesLinea = buffer.ToList();
            if (bytesLinea.Count > 0 && bytesLinea.Last() == '\r')
            {
                bytesLinea.RemoveAt(bytesLinea.Count - 1);
            }
            if (bytesLinea.Count > 0)
            {
                var linea = System.Text.Encoding.UTF8.GetString(bytesLinea.ToArray());
                lineas.Insert(0, linea);
            }
        }
        
        Log.Debug("[LeerUltimasLineasDesdeFinal] Leídas {Count} líneas", lineas.Count);
        return lineas;
    }

    #endregion
}

