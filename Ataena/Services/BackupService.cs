using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Ataena.Data;
using Ataena.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Ataena.Services;

/// <summary>
/// Servicio para gestionar backups de la aplicación.
/// Incluye creación de backups, detección de servicios de nube y gestión de archivos de backup.
/// </summary>
public class BackupService
{
    private readonly AtaenaDbContext _db;

    public BackupService(AtaenaDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Obtiene la ruta de la carpeta de backups local.
    /// </summary>
    public static string ObtenerRutaCarpetaBackups()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var rutaBackups = Path.Combine(localAppData, "Ataena", "backups");

        if (!Directory.Exists(rutaBackups))
        {
            Directory.CreateDirectory(rutaBackups);
        }

        return rutaBackups;
    }

    /// <summary>
    /// Detecta los servicios de nube instalados en el sistema.
    /// </summary>
    public static List<InfoServicioNube> DetectarServiciosNube()
    {
        var servicios = new List<InfoServicioNube>();
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        // OneDrive
        var oneDrivePath = Path.Combine(userProfile, "OneDrive");
        if (Directory.Exists(oneDrivePath))
        {
            var backupPath = Path.Combine(oneDrivePath, "Ataena", "Backups");
            servicios.Add(new InfoServicioNube
            {
                Tipo = ServicioNube.OneDrive,
                Nombre = "OneDrive",
                RutaCarpeta = backupPath,
                Detectado = true,
                Sincronizado = Directory.Exists(backupPath),
                MensajeEstado = Directory.Exists(backupPath) ? "✅ Detectado y sincronizado" : "✅ Detectado (carpeta no creada)"
            });
        }

        // Google Drive
        var googleDrivePath = Path.Combine(userProfile, "Google Drive");
        if (Directory.Exists(googleDrivePath))
        {
            var backupPath = Path.Combine(googleDrivePath, "Ataena", "Backups");
            servicios.Add(new InfoServicioNube
            {
                Tipo = ServicioNube.GoogleDrive,
                Nombre = "Google Drive",
                RutaCarpeta = backupPath,
                Detectado = true,
                Sincronizado = Directory.Exists(backupPath),
                MensajeEstado = Directory.Exists(backupPath) ? "✅ Detectado y sincronizado" : "✅ Detectado (carpeta no creada)"
            });
        }

        // Dropbox
        var dropboxPath = Path.Combine(userProfile, "Dropbox");
        if (Directory.Exists(dropboxPath))
        {
            var backupPath = Path.Combine(dropboxPath, "Ataena", "Backups");
            servicios.Add(new InfoServicioNube
            {
                Tipo = ServicioNube.Dropbox,
                Nombre = "Dropbox",
                RutaCarpeta = backupPath,
                Detectado = true,
                Sincronizado = Directory.Exists(backupPath),
                MensajeEstado = Directory.Exists(backupPath) ? "✅ Detectado y sincronizado" : "✅ Detectado (carpeta no creada)"
            });
        }

        return servicios;
    }

    /// <summary>
    /// Crea un backup completo de la base de datos y archivos.
    /// </summary>
    public async Task<string> CrearBackupAsync(string? rutaDestino = null)
    {
        try
        {
            Log.Information("🔄 [BackupService] Iniciando creación de backup (Thread: {ThreadId})", System.Threading.Thread.CurrentThread.ManagedThreadId);

            // Determinar ruta de destino
            if (string.IsNullOrEmpty(rutaDestino))
            {
                rutaDestino = ObtenerRutaCarpetaBackups();
            }

            // Crear nombre de archivo único con milisegundos para evitar colisiones
            var fecha = DateTime.Now;
            var nombreArchivo = $"backup_ataena_{fecha:yyyy-MM-dd_HH-mm-ss-fff}.zip";
            var rutaCompleta = Path.Combine(rutaDestino, nombreArchivo);
            
            // Asegurar que el archivo no exista (por si acaso hay colisión)
            if (File.Exists(rutaCompleta))
            {
                // Si existe, añadir un número aleatorio
                var nombreSinExtension = Path.GetFileNameWithoutExtension(nombreArchivo);
                var extension = Path.GetExtension(nombreArchivo);
                var contador = 1;
                do
                {
                    nombreArchivo = $"{nombreSinExtension}_{contador}{extension}";
                    rutaCompleta = Path.Combine(rutaDestino, nombreArchivo);
                    contador++;
                } while (File.Exists(rutaCompleta) && contador < 100);
            }

            // Obtener estadísticas antes de crear el backup
            var numClientes = await _db.Clientes.CountAsync();
            var numCitas = await _db.Citas.CountAsync();
            var numTrabajos = await _db.Trabajos.CountAsync();
            var numConsentimientos = await _db.Consentimientos.CountAsync();

            // Obtener rutas de archivos
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var carpetaAtaena = Path.Combine(localAppData, "Ataena");
            var rutaBaseDatos = Path.Combine(carpetaAtaena, "data.db");
            var rutaFicheros = ConsentimientoPathService.ObtenerRutaBaseFicheros();

            // Calcular tamaño total
            long tamañoTotal = 0;
            if (File.Exists(rutaBaseDatos))
            {
                tamañoTotal += new FileInfo(rutaBaseDatos).Length;
            }

            if (Directory.Exists(rutaFicheros))
            {
                tamañoTotal += ObtenerTamañoDirectorio(rutaFicheros);
            }

            // Crear copia temporal de la base de datos usando VACUUM INTO
            // Esto evita problemas de bloqueo mientras la BD está en uso
            var rutaDbTemporal = Path.Combine(Path.GetTempPath(), $"ataena_backup_{Guid.NewGuid()}.db");
            
            // Asegurar que el archivo temporal no exista (por si acaso)
            if (File.Exists(rutaDbTemporal))
            {
                try
                {
                    File.Delete(rutaDbTemporal);
                    Log.Information("🗑️ Archivo temporal existente eliminado: {Ruta}", rutaDbTemporal);
                }
                catch (Exception exDel)
                {
                    Log.Warning(exDel, "⚠️ No se pudo eliminar archivo temporal existente, continuando...");
                }
            }
            
            try
            {
                // Hacer checkpoint para consolidar el WAL
                await _db.Database.ExecuteSqlRawAsync("PRAGMA wal_checkpoint(FULL);");
                
                // Crear copia limpia usando VACUUM INTO (SQLite 3.27+)
                // Si falla, intentamos copiar directamente con FileShare.Read
                try
                {
                    // Escapar comillas simples en la ruta para SQL
                    var rutaDbTemporalEscapada = rutaDbTemporal.Replace("'", "''");
                    // Usar FormattableString para evitar warning de SQL injection (la ruta es controlada internamente)
                    FormattableString sql = $"VACUUM INTO '{rutaDbTemporalEscapada}'";
                    await _db.Database.ExecuteSqlAsync(sql);
                    
                    // Verificar que el archivo se creó correctamente
                    if (!File.Exists(rutaDbTemporal))
                    {
                        throw new InvalidOperationException("VACUUM INTO no creó el archivo de destino");
                    }
                    
                    Log.Information("✅ Copia de base de datos creada usando VACUUM INTO");
                }
                catch (Exception exVacuum)
                {
                    Log.Warning(exVacuum, "⚠️ VACUUM INTO falló ({Error}), intentando copia directa con FileShare.Read", exVacuum.Message);
                    
                    // Eliminar archivo temporal si existe (por si VACUUM INTO creó un archivo corrupto)
                    if (File.Exists(rutaDbTemporal))
                    {
                        try
                        {
                            File.Delete(rutaDbTemporal);
                        }
                        catch
                        {
                            // Ignorar errores al eliminar
                        }
                    }
                    
                    // Fallback: copiar con FileShare.Read
                    using (var sourceStream = new FileStream(rutaBaseDatos, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var destStream = new FileStream(rutaDbTemporal, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        await sourceStream.CopyToAsync(destStream);
                    }
                    Log.Information("✅ Copia de base de datos creada usando FileShare.Read");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "❌ Error al crear copia de la base de datos");
                throw;
            }

            // Crear archivo ZIP
            using (var zip = ZipFile.Open(rutaCompleta, ZipArchiveMode.Create))
            {
                // Agregar base de datos desde la copia temporal
                if (File.Exists(rutaDbTemporal))
                {
                    zip.CreateEntryFromFile(rutaDbTemporal, "data.db");
                    Log.Information("✅ Base de datos agregada al backup");
                }
                else
                {
                    throw new InvalidOperationException("No se pudo crear la copia de la base de datos");
                }

                // Agregar carpeta de ficheros
                if (Directory.Exists(rutaFicheros))
                {
                    AgregarDirectorioAlZip(zip, rutaFicheros, "ficheros");
                    Log.Information("✅ Archivos de clientes agregados al backup");
                }

                // Crear metadata
                var metadata = new BackupMetadata
                {
                    FechaCreacion = fecha,
                    VersionApp = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.5.0",
                    TamañoTotalBytes = tamañoTotal,
                    NumeroClientes = numClientes,
                    NumeroCitas = numCitas,
                    NumeroTrabajos = numTrabajos,
                    NumeroConsentimientos = numConsentimientos
                };

                var metadataJson = JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
                var metadataEntry = zip.CreateEntry("metadata.json");
                using (var stream = metadataEntry.Open())
                using (var writer = new StreamWriter(stream))
                {
                    await writer.WriteAsync(metadataJson);
                    await writer.FlushAsync();
                }

                // Crear README con instrucciones
                var readme = CrearReadmeRestauracion();
                var readmeEntry = zip.CreateEntry("README.txt");
                using (var stream = readmeEntry.Open())
                using (var writer = new StreamWriter(stream))
                {
                    await writer.WriteAsync(readme);
                    await writer.FlushAsync();
                }
            } // El ZIP se cierra aquí automáticamente

            // Verificar que el ZIP se creó correctamente
            if (!File.Exists(rutaCompleta))
            {
                throw new InvalidOperationException("El archivo ZIP no se creó correctamente");
            }

            // Esperar un poco para asegurar que el ZIP se cierre completamente
            await Task.Delay(200);
            
            // Validar el backup creado
            if (!ValidarBackup(rutaCompleta))
            {
                throw new InvalidOperationException("El backup creado no es válido");
            }

            // Limpiar archivo temporal
            try
            {
                if (File.Exists(rutaDbTemporal))
                {
                    File.Delete(rutaDbTemporal);
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "⚠️ No se pudo eliminar el archivo temporal: {Ruta}", rutaDbTemporal);
            }

            Log.Information("✅ Backup creado exitosamente: {Ruta}", rutaCompleta);
            return rutaCompleta;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "❌ Error al crear backup");
            throw;
        }
    }

    /// <summary>
    /// Copia un backup a la carpeta de nube configurada.
    /// </summary>
    public async Task<string?> CopiarBackupANubeAsync(string rutaBackup, InfoServicioNube servicioNube)
    {
        try
        {
            if (!servicioNube.Detectado || string.IsNullOrEmpty(servicioNube.RutaCarpeta))
            {
                Log.Warning("Servicio de nube no válido para copiar backup");
                return null;
            }

            // Crear carpeta de backups en la nube si no existe
            if (!Directory.Exists(servicioNube.RutaCarpeta))
            {
                Directory.CreateDirectory(servicioNube.RutaCarpeta);
            }

            // Copiar archivo
            var nombreArchivo = Path.GetFileName(rutaBackup);
            var rutaDestino = Path.Combine(servicioNube.RutaCarpeta, nombreArchivo);

            await Task.Run(() => File.Copy(rutaBackup, rutaDestino, overwrite: true));

            Log.Information("✅ Backup copiado a {Servicio}: {Ruta}", servicioNube.Nombre, rutaDestino);
            return rutaDestino;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "❌ Error al copiar backup a nube");
            return null;
        }
    }

    /// <summary>
    /// Lista todos los backups disponibles en una carpeta.
    /// </summary>
    public static List<InfoBackup> ListarBackups(string carpeta)
    {
        var backups = new List<InfoBackup>();

        if (!Directory.Exists(carpeta))
        {
            return backups;
        }

        var archivos = Directory.GetFiles(carpeta, "backup_ataena_*.zip");
        foreach (var archivo in archivos.OrderByDescending(f => new FileInfo(f).CreationTime))
        {
            backups.Add(InfoBackup.DesdeArchivo(archivo));
        }

        return backups;
    }

    /// <summary>
    /// Obtiene los metadatos de un backup desde el archivo ZIP.
    /// </summary>
    public static BackupMetadata? ObtenerMetadataBackup(string rutaZip)
    {
        try
        {
            // Asegurar que el archivo no esté bloqueado esperando un poco si es necesario
            int intentos = 0;
            while (intentos < 5)
            {
                try
                {
                    using (var zip = ZipFile.OpenRead(rutaZip))
                    {
                        var metadataEntry = zip.GetEntry("metadata.json");
                        if (metadataEntry == null)
                        {
                            return null;
                        }

                        using (var stream = metadataEntry.Open())
                        using (var reader = new StreamReader(stream))
                        {
                            var json = reader.ReadToEnd();
                            return JsonSerializer.Deserialize<BackupMetadata>(json);
                        }
                    }
                }
                catch (IOException) when (intentos < 4)
                {
                    // Si el archivo está en uso, esperar un poco y reintentar
                    System.Threading.Thread.Sleep(100);
                    intentos++;
                }
            }
            return null;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al leer metadata del backup: {Ruta}", rutaZip);
            return null;
        }
    }

    /// <summary>
    /// Valida que un backup sea válido y restaurable.
    /// </summary>
    public static bool ValidarBackup(string rutaZip)
    {
        try
        {
            if (!File.Exists(rutaZip))
            {
                return false;
            }

            // Asegurar que el archivo no esté bloqueado esperando un poco si es necesario
            int intentos = 0;
            while (intentos < 5)
            {
                try
                {
                    using (var zip = ZipFile.OpenRead(rutaZip))
                    {
                        // Verificar que tenga data.db
                        if (zip.GetEntry("data.db") == null)
                        {
                            return false;
                        }

                        // Verificar que tenga metadata.json
                        if (zip.GetEntry("metadata.json") == null)
                        {
                            return false;
                        }

                        return true;
                    }
                }
                catch (IOException) when (intentos < 4)
                {
                    // Si el archivo está en uso, esperar un poco y reintentar
                    System.Threading.Thread.Sleep(100);
                    intentos++;
                }
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Elimina un backup (local o de nube).
    /// </summary>
    public static void EliminarBackup(string rutaBackup)
    {
        try
        {
            if (File.Exists(rutaBackup))
            {
                File.Delete(rutaBackup);
                Log.Information("✅ Backup eliminado: {Ruta}", rutaBackup);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al eliminar backup: {Ruta}", rutaBackup);
            throw;
        }
    }

    /// <summary>
    /// Realiza rotación de backups, eliminando los más antiguos.
    /// </summary>
    public static void RotarBackups(string carpeta, int mantenerUltimos)
    {
        var backups = ListarBackups(carpeta);
        if (backups.Count <= mantenerUltimos)
        {
            return;
        }

        // Eliminar los más antiguos
        var backupsAEliminar = backups.Skip(mantenerUltimos).ToList();
        foreach (var backup in backupsAEliminar)
        {
            EliminarBackup(backup.RutaCompleta);
        }

        Log.Information("🔄 Rotación de backups: {Eliminados} backups antiguos eliminados", backupsAEliminar.Count);
    }

    #region Métodos Auxiliares

    private static long ObtenerTamañoDirectorio(string directorio)
    {
        long tamaño = 0;
        try
        {
            var archivos = Directory.GetFiles(directorio, "*", SearchOption.AllDirectories);
            foreach (var archivo in archivos)
            {
                var info = new FileInfo(archivo);
                tamaño += info.Length;
            }
        }
        catch
        {
            // Ignorar errores al calcular tamaño
        }
        return tamaño;
    }

    private static void AgregarDirectorioAlZip(ZipArchive zip, string directorio, string nombreEnZip)
    {
        var archivos = Directory.GetFiles(directorio, "*", SearchOption.AllDirectories);
        foreach (var archivo in archivos)
        {
            var nombreRelativo = Path.GetRelativePath(directorio, archivo);
            var entradaZip = Path.Combine(nombreEnZip, nombreRelativo).Replace('\\', '/');
            zip.CreateEntryFromFile(archivo, entradaZip);
        }
    }

    private static string CrearReadmeRestauracion()
    {
        return @"INSTRUCCIONES DE RESTAURACIÓN
=============================

Este archivo contiene un backup completo de Ataena.

Para restaurar este backup en un PC nuevo:

1. Instalar Ataena en el nuevo PC
2. Abrir Ataena
3. Ir a: Configuración > Backup y Restauración
4. Click en 'Restaurar desde archivo'
5. Seleccionar este archivo ZIP
6. Confirmar la restauración
7. ¡Listo! Todos tus datos estarán restaurados.

IMPORTANTE: La restauración reemplazará TODOS los datos actuales.
Asegúrate de hacer un backup de los datos actuales antes de restaurar.

Fecha de creación: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
    }

    #endregion
}

