using System;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Threading.Tasks;
using InkStudio.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace InkStudio.Services;

/// <summary>
/// Servicio para restaurar backups de la aplicación.
/// </summary>
public class RestauracionService
{
    /// <summary>
    /// Restaura un backup desde un archivo ZIP.
    /// </summary>
    /// <param name="rutaZip">Ruta al archivo ZIP del backup.</param>
    /// <param name="crearBackupActual">Si es true, crea un backup de los datos actuales antes de restaurar.</param>
    /// <returns>Metadatos del backup restaurado.</returns>
    public async Task<BackupMetadata> RestaurarBackupAsync(string rutaZip, bool crearBackupActual = true)
    {
        try
        {
            Log.Information("🔄 Iniciando restauración de backup: {Ruta}", rutaZip);

            // Esperar y verificar que el archivo esté disponible
            await EsperarArchivoDisponible(rutaZip);
            
            // Validar backup
            if (!BackupService.ValidarBackup(rutaZip))
            {
                throw new InvalidOperationException("El archivo de backup no es válido o está corrupto.");
            }

            // Esperar un poco más antes de leer metadatos
            await Task.Delay(300);
            
            // Obtener metadatos
            var metadata = BackupService.ObtenerMetadataBackup(rutaZip);
            if (metadata == null)
            {
                throw new InvalidOperationException("No se pudieron leer los metadatos del backup.");
            }
            
            // Esperar un poco más y verificar que el archivo esté disponible antes de extraer
            await Task.Delay(300);
            await EsperarArchivoDisponible(rutaZip);

            // Crear backup de datos actuales si se solicita
            if (crearBackupActual)
            {
                try
                {
                    var backupService = new BackupService(new Data.InkStudioDbContext());
                    var rutaBackupActual = await backupService.CrearBackupAsync();
                    Log.Information("✅ Backup de datos actuales creado: {Ruta}", rutaBackupActual);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "⚠️ No se pudo crear backup de datos actuales, continuando con restauración...");
                }
            }

            // Obtener rutas
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var carpetaInkStudio = Path.Combine(localAppData, "InkStudio");
            var rutaBaseDatos = Path.Combine(carpetaInkStudio, "data.db");
            var rutaFicheros = ConsentimientoPathService.ObtenerRutaBaseFicheros();

            // Crear carpeta temporal para extraer
            var carpetaTemp = Path.Combine(Path.GetTempPath(), $"inkstudio_restore_{Guid.NewGuid()}");
            Directory.CreateDirectory(carpetaTemp);

            try
            {
                // Extraer ZIP con reintentos si es necesario
                Log.Information("📦 Extrayendo backup...");
                int intentosExtraccion = 0;
                while (intentosExtraccion < 5)
                {
                    try
                    {
                        ZipFile.ExtractToDirectory(rutaZip, carpetaTemp);
                        break; // Éxito, salir del bucle
                    }
                    catch (IOException) when (intentosExtraccion < 4)
                    {
                        // Si el archivo está en uso, esperar y reintentar
                        Log.Warning("⚠️ Archivo ZIP en uso, esperando... (Intento {Intento}/5)", intentosExtraccion + 1);
                        await Task.Delay(500);
                        intentosExtraccion++;
                    }
                }
                
                if (intentosExtraccion >= 5)
                {
                    throw new IOException("No se pudo acceder al archivo ZIP después de varios intentos. Asegúrate de que no esté abierto en otro programa.");
                }

                // Cerrar conexiones a la BD y consolidar WAL antes de reemplazar
                // Hacer checkpoint para consolidar el WAL en la base de datos principal
                await CerrarConexionesBaseDatos(rutaBaseDatos);

                // Reemplazar base de datos
                var rutaDbEnZip = Path.Combine(carpetaTemp, "data.db");
                if (File.Exists(rutaDbEnZip))
                {
                    // Esperar un poco para asegurar que los archivos estén liberados
                    await Task.Delay(1000);
                    
                    // Intentar renombrar archivos WAL y SHM (no crítico si falla)
                    var rutaWal = rutaBaseDatos + "-wal";
                    var rutaShm = rutaBaseDatos + "-shm";
                    var rutaWalBackup = rutaBaseDatos + "-wal.backup";
                    var rutaShmBackup = rutaBaseDatos + "-shm.backup";
                    
                    // Intentar renombrar archivos WAL y SHM (no crítico si falla)
                    // Estos archivos pueden estar en uso por conexiones activas, pero SQLite
                    // creará nuevos cuando se abra la base de datos restaurada
                    bool walRenombrado = false;
                    bool shmRenombrado = false;
                    
                    try
                    {
                        await RenombrarArchivoConReintentos(rutaWal, rutaWalBackup);
                        walRenombrado = true;
                        Log.Information("✅ Archivo WAL renombrado correctamente");
                    }
                    catch
                    {
                        Log.Information("ℹ️ No se pudo renombrar el archivo WAL (está en uso), pero no es crítico. SQLite creará uno nuevo al abrir la base de datos.");
                    }
                    
                    try
                    {
                        await RenombrarArchivoConReintentos(rutaShm, rutaShmBackup);
                        shmRenombrado = true;
                        Log.Information("✅ Archivo SHM renombrado correctamente");
                    }
                    catch
                    {
                        Log.Information("ℹ️ No se pudo renombrar el archivo SHM (está en uso), pero no es crítico. SQLite creará uno nuevo al abrir la base de datos.");
                    }

                    // Reemplazar base de datos (esto es lo importante)
                    await CopiarArchivoConReintentos(rutaDbEnZip, rutaBaseDatos);
                    Log.Information("✅ Base de datos restaurada");
                    
                    // Intentar eliminar los archivos renombrados (solo si se renombraron correctamente)
                    if (walRenombrado || shmRenombrado)
                    {
                        await Task.Delay(500);
                        try
                        {
                            if (walRenombrado && File.Exists(rutaWalBackup))
                            {
                                File.Delete(rutaWalBackup);
                                Log.Information("✅ Archivo WAL antiguo eliminado");
                            }
                            if (shmRenombrado && File.Exists(rutaShmBackup))
                            {
                                File.Delete(rutaShmBackup);
                                Log.Information("✅ Archivo SHM antiguo eliminado");
                            }
                        }
                        catch
                        {
                            Log.Information("ℹ️ No se pudieron eliminar los archivos WAL/SHM renombrados. Se eliminarán automáticamente en el próximo inicio de la aplicación.");
                        }
                    }

                    // Restaurar archivos WAL y SHM si existen
                    var rutaWalEnZip = Path.Combine(carpetaTemp, "data.db-wal");
                    var rutaShmEnZip = Path.Combine(carpetaTemp, "data.db-shm");
                    if (File.Exists(rutaWalEnZip))
                    {
                        File.Copy(rutaWalEnZip, rutaWal, overwrite: true);
                    }
                    if (File.Exists(rutaShmEnZip))
                    {
                        File.Copy(rutaShmEnZip, rutaShm, overwrite: true);
                    }
                }

                // Reemplazar carpeta de ficheros
                var rutaFicherosEnZip = Path.Combine(carpetaTemp, "ficheros");
                if (Directory.Exists(rutaFicherosEnZip))
                {
                    // Eliminar carpeta actual si existe
                    if (Directory.Exists(rutaFicheros))
                    {
                        Directory.Delete(rutaFicheros, recursive: true);
                    }

                    // Copiar carpeta restaurada
                    CopiarDirectorio(rutaFicherosEnZip, rutaFicheros);
                    Log.Information("✅ Archivos de clientes restaurados");
                }

                Log.Information("✅ Restauración completada exitosamente");
                return metadata;
            }
            finally
            {
                // Limpiar carpeta temporal
                try
                {
                    if (Directory.Exists(carpetaTemp))
                    {
                        Directory.Delete(carpetaTemp, recursive: true);
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "No se pudo eliminar carpeta temporal: {Carpeta}", carpetaTemp);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "❌ Error al restaurar backup");
            throw;
        }
    }

    /// <summary>
    /// Valida que un backup sea compatible con la versión actual de la aplicación.
    /// </summary>
    public bool ValidarBackupCompatible(string rutaZip)
    {
        try
        {
            var metadata = BackupService.ObtenerMetadataBackup(rutaZip);
            if (metadata == null)
            {
                return false;
            }

            // Por ahora, aceptamos cualquier versión
            // En el futuro, podríamos validar compatibilidad de versión
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Obtiene un resumen del backup para mostrar al usuario antes de restaurar.
    /// </summary>
    public string ObtenerResumenBackup(string rutaZip)
    {
        try
        {
            var metadata = BackupService.ObtenerMetadataBackup(rutaZip);
            if (metadata == null)
            {
                return "No se pudieron leer los metadatos del backup.";
            }

            var fileInfo = new FileInfo(rutaZip);
            var tamañoFormateado = FormatearTamaño(fileInfo.Length);

            return $"Fecha: {metadata.FechaCreacion:dd/MM/yyyy HH:mm}\n" +
                   $"Tamaño: {tamañoFormateado}\n" +
                   $"Clientes: {metadata.NumeroClientes}\n" +
                   $"Citas: {metadata.NumeroCitas}\n" +
                   $"Trabajos: {metadata.NumeroTrabajos}\n" +
                   $"Consentimientos: {metadata.NumeroConsentimientos}\n" +
                   $"Versión: {metadata.VersionApp}";
        }
        catch (Exception ex)
        {
            return $"Error al leer información del backup: {ex.Message}";
        }
    }

    #region Métodos Auxiliares

    /// <summary>
    /// Cierra todas las conexiones a la base de datos y consolida el WAL.
    /// </summary>
    private static async Task CerrarConexionesBaseDatos(string rutaBaseDatos)
    {
        try
        {
            // Intentar hacer checkpoint para consolidar el WAL
            // Esto requiere abrir una conexión temporal solo para el checkpoint
            using (var db = new Data.InkStudioDbContext())
            {
                // Hacer checkpoint para consolidar el WAL en la base de datos principal
                await db.Database.ExecuteSqlRawAsync("PRAGMA wal_checkpoint(FULL);");
                Log.Information("✅ Checkpoint de WAL completado");
            }
            
            // Esperar un poco para asegurar que la conexión se cierre completamente
            await Task.Delay(300);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "⚠️ No se pudo hacer checkpoint de WAL, continuando...");
        }
    }

    /// <summary>
    /// Elimina un archivo con reintentos si está en uso.
    /// </summary>
    private static async Task EliminarArchivoConReintentos(string rutaArchivo, int maxIntentos = 15)
    {
        if (!File.Exists(rutaArchivo))
        {
            return;
        }

        for (int i = 0; i < maxIntentos; i++)
        {
            try
            {
                // Intentar eliminar con FileShare.ReadWrite para forzar el cierre
                using (var fileStream = new FileStream(rutaArchivo, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
                {
                    // Si podemos abrir el archivo, intentar cerrarlo y eliminarlo
                    fileStream.Close();
                }
                
                // Pequeño delay antes de intentar eliminar
                await Task.Delay(200);
                
                File.Delete(rutaArchivo);
                Log.Information("✅ Archivo eliminado: {Archivo}", Path.GetFileName(rutaArchivo));
                return;
            }
            catch (IOException) when (i < maxIntentos - 1)
            {
                Log.Warning("⚠️ Archivo en uso, esperando... (Intento {Intento}/{MaxIntentos}): {Archivo}", 
                    i + 1, maxIntentos, Path.GetFileName(rutaArchivo));
                
                // Forzar recolección de basura cada 5 intentos
                if (i % 5 == 4)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                }
                
                await Task.Delay(500); // Aumentar delay a 500ms
            }
        }
        
        // Si después de todos los intentos no se puede eliminar, intentar renombrarlo
        try
        {
            var rutaBackup = rutaArchivo + ".backup";
            File.Move(rutaArchivo, rutaBackup);
            Log.Warning("⚠️ No se pudo eliminar {Archivo}, renombrado a {Backup}", 
                Path.GetFileName(rutaArchivo), Path.GetFileName(rutaBackup));
        }
        catch
        {
            throw new IOException($"No se pudo eliminar ni renombrar el archivo {rutaArchivo} después de {maxIntentos} intentos. Por favor, cierra la aplicación y vuelve a intentar.");
        }
    }

    /// <summary>
    /// Renombra un archivo con reintentos si está en uso.
    /// </summary>
    private static async Task RenombrarArchivoConReintentos(string rutaOrigen, string rutaDestino, int maxIntentos = 10)
    {
        if (!File.Exists(rutaOrigen))
        {
            return;
        }

        // Si el archivo destino ya existe, eliminarlo primero
        if (File.Exists(rutaDestino))
        {
            try
            {
                File.Delete(rutaDestino);
            }
            catch
            {
                // Si no se puede eliminar, continuar de todos modos
            }
        }

        for (int i = 0; i < maxIntentos; i++)
        {
            try
            {
                File.Move(rutaOrigen, rutaDestino);
                Log.Information("✅ Archivo renombrado: {Origen} -> {Destino}", 
                    Path.GetFileName(rutaOrigen), Path.GetFileName(rutaDestino));
                return;
            }
            catch (IOException) when (i < maxIntentos - 1)
            {
                Log.Warning("⚠️ Archivo en uso, esperando para renombrar... (Intento {Intento}/{MaxIntentos}): {Archivo}", 
                    i + 1, maxIntentos, Path.GetFileName(rutaOrigen));
                
                // Forzar recolección de basura cada 3 intentos
                if (i % 3 == 2)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                }
                
                await Task.Delay(500);
            }
        }
        
        // Si no se puede renombrar después de todos los intentos, lanzar excepción
        // pero el código que llama debe manejarla y continuar
        throw new IOException($"No se pudo renombrar el archivo {rutaOrigen} después de {maxIntentos} intentos.");
    }

    /// <summary>
    /// Copia un archivo con reintentos si el destino está en uso.
    /// </summary>
    private static async Task CopiarArchivoConReintentos(string origen, string destino, int maxIntentos = 10)
    {
        for (int i = 0; i < maxIntentos; i++)
        {
            try
            {
                File.Copy(origen, destino, overwrite: true);
                Log.Information("✅ Archivo copiado: {Destino}", Path.GetFileName(destino));
                return;
            }
            catch (IOException) when (i < maxIntentos - 1)
            {
                Log.Warning("⚠️ Archivo destino en uso, esperando... (Intento {Intento}/{MaxIntentos}): {Archivo}", 
                    i + 1, maxIntentos, Path.GetFileName(destino));
                await Task.Delay(300);
            }
        }
        
        throw new IOException($"No se pudo copiar el archivo a {destino} después de {maxIntentos} intentos. Asegúrate de cerrar todas las conexiones a la base de datos.");
    }

    /// <summary>
    /// Espera hasta que un archivo esté disponible para lectura/escritura.
    /// </summary>
    private static async Task EsperarArchivoDisponible(string rutaArchivo, int maxIntentos = 10)
    {
        for (int i = 0; i < maxIntentos; i++)
        {
            try
            {
                // Intentar abrir el archivo en modo lectura compartida
                using (var fileStream = new FileStream(rutaArchivo, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    // Si se puede abrir, el archivo está disponible
                    await Task.Delay(100); // Pequeño delay adicional
                    return;
                }
            }
            catch (IOException)
            {
                // El archivo está en uso, esperar y reintentar
                if (i < maxIntentos - 1)
                {
                    await Task.Delay(300);
                }
                else
                {
                    throw new IOException($"El archivo {rutaArchivo} está siendo utilizado por otro proceso. Por favor, ciérralo e intenta de nuevo.");
                }
            }
        }
    }

    private static void CopiarDirectorio(string origen, string destino)
    {
        Directory.CreateDirectory(destino);

        foreach (var archivo in Directory.GetFiles(origen))
        {
            var nombreArchivo = Path.GetFileName(archivo);
            var destinoArchivo = Path.Combine(destino, nombreArchivo);
            File.Copy(archivo, destinoArchivo, overwrite: true);
        }

        foreach (var subdirectorio in Directory.GetDirectories(origen))
        {
            var nombreSubdirectorio = Path.GetFileName(subdirectorio);
            var destinoSubdirectorio = Path.Combine(destino, nombreSubdirectorio);
            CopiarDirectorio(subdirectorio, destinoSubdirectorio);
        }
    }

    private static string FormatearTamaño(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    #endregion
}

