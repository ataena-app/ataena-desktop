using Avalonia;
using InkStudio.Data;
using InkStudio.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace InkStudio;

/// <summary>
/// Punto de entrada principal de la aplicación.
/// </summary>
sealed class Program
{
    /// <summary>
    /// Método principal de la aplicación.
    /// </summary>
    /// <param name="args">Argumentos de línea de comandos.</param>
    /// <remarks>
    /// Inicializa el sistema de logging antes de iniciar Avalonia.
    /// </remarks>
    [STAThread]
    public static void Main(string[] args)
    {
        // Inicializar logging ANTES de todo
        LoggingService.Inicializar();

        try
        {
            // Asegurar que la base de datos y el esquema existen (crea/aplica migraciones en primer arranque)
            try
            {
                // Intentar migración con reintentos si la base de datos está bloqueada
                int maxIntentos = 5;
                int intento = 0;
                bool migrado = false;
                
                while (intento < maxIntentos && !migrado)
                {
                    try
                    {
                        using var db = new InkStudioDbContext();
                        db.Database.Migrate();
                        migrado = true;
                        Serilog.Log.Information("✅ Base de datos inicializada correctamente");
                    }
                    catch (SqliteException ex) when (ex.SqliteErrorCode == 5 && intento < maxIntentos - 1) // Error 5 = database is locked
                    {
                        intento++;
                        Serilog.Log.Warning("⚠️ Base de datos bloqueada, reintentando... (Intento {Intento}/{MaxIntentos})", intento, maxIntentos);
                        System.Threading.Thread.Sleep(1000 * intento); // Esperar progresivamente más tiempo
                    }
                }
                
                if (!migrado)
                {
                    throw new InvalidOperationException(
                        "No se pudo acceder a la base de datos después de varios intentos. " +
                        "Asegúrate de que:\n" +
                        "1. No esté abierta en otro programa (como un visor SQL)\n" +
                        "2. Cierres todas las instancias anteriores de la aplicación\n" +
                        "3. No haya otros procesos usando el archivo data.db");
                }

                // Asegurar también la estructura base de ficheros (%LOCALAPPDATA%\InkStudio\ficheros\)
                ConsentimientoPathService.ObtenerRutaBaseFicheros();
            }
            catch (Exception exDb)
            {
                Serilog.Log.Fatal(exDb, "Error al inicializar la base de datos o la estructura de ficheros al iniciar la aplicación");
                throw;
            }

            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            Serilog.Log.Fatal(ex, "Error fatal al iniciar la aplicación");
            throw;
        }
        finally
        {
            // Cerrar logging al salir
            LoggingService.Cerrar();
        }
    }

    /// <summary>
    /// Configuración de Avalonia.
    /// </summary>
    /// <returns>AppBuilder configurado.</returns>
    /// <remarks>
    /// No remover: también usado por el diseñador visual.
    /// </remarks>
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
