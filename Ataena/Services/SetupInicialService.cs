using System;
using System.IO;

namespace Ataena.Services;

/// <summary>
/// Servicio para detectar si es el primer inicio de la aplicación.
/// </summary>
public static class SetupInicialService
{
    private const string NombreArchivoFlag = ".setup_completado";

    /// <summary>
    /// Indica si el usuario ya completó el setup inicial (crear nuevo o importar).
    /// </summary>
    public static bool SetupCompletado
    {
        get
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var flagPath = Path.Combine(localAppData, "Ataena", NombreArchivoFlag);
            return File.Exists(flagPath);
        }
    }
}
