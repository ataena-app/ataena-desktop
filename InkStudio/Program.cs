using Avalonia;
using InkStudio.Services;
using InkStudio.Data;
using Microsoft.EntityFrameworkCore;
using System;

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
                using var db = new InkStudioDbContext();
                db.Database.Migrate();
            }
            catch (Exception exDb)
            {
                Serilog.Log.Fatal(exDb, "Error al aplicar migraciones de base de datos al iniciar la aplicación");
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
