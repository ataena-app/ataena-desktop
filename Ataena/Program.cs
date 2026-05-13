using Avalonia;
using Ataena.Data;
using Ataena.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Ataena;

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
    /// Logging se inicializa primero para diagnosticar problemas.
    /// La instalación se hace con Inno Setup. Las actualizaciones las gestiona ActualizacionService.
    /// </remarks>
    [STAThread]
    public static void Main(string[] args)
    {
        // Si la app se lanza con --update-completed, es porque viene de reinstalarse
        // (Inno Setup /VERYSILENT) y debe seguir su flujo normal.
        LoggingService.Inicializar();
        LoggingService.EscribirDiagnostico("Main: Inicio");

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
                        using var db = new AtaenaDbContext();
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
                
                LoggingService.EscribirDiagnostico("Main: Base de datos migrada OK");

                if (!migrado)
                {
                    throw new InvalidOperationException(
                        "No se pudo acceder a la base de datos después de varios intentos. " +
                        "Asegúrate de que:\n" +
                        "1. No esté abierta en otro programa (como un visor SQL)\n" +
                        "2. Cierres todas las instancias anteriores de la aplicación\n" +
                        "3. No haya otros procesos usando el archivo data.db");
                }

                // Asegurar también la estructura base de ficheros (%LOCALAPPDATA%\Ataena\ficheros\)
                ConsentimientoPathService.ObtenerRutaBaseFicheros();
                LoggingService.EscribirDiagnostico("Main: Iniciando Avalonia");
            }
            catch (Exception exDb)
            {
                LoggingService.EscribirDiagnostico($"Main: ERROR BD - {exDb.Message}");
                Serilog.Log.Fatal(exDb, "Error al inicializar la base de datos o la estructura de ficheros al iniciar la aplicación");
                throw;
            }

            LoggingService.EscribirDiagnostico("Main: BuildAvaloniaApp.StartWithClassicDesktopLifetime");
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
