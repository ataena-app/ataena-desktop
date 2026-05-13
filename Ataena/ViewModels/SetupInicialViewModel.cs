using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ataena.Data;
using Ataena.Models;
using Ataena.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Ataena.ViewModels;

/// <summary>
/// ViewModel para la pantalla de primer inicio: Crear nuevo estudio o Importar datos.
/// </summary>
public partial class SetupInicialViewModel : ViewModelBase
{
    private readonly Window _window;
    private readonly RestauracionService _restauracionService = new();

    public SetupInicialViewModel(Window window)
    {
        _window = window ?? throw new ArgumentNullException(nameof(window));
    }

    /// <summary>
    /// Se dispara cuando el usuario completa el setup (crear nuevo o importar).
    /// </summary>
    public event EventHandler? SetupCompletado;

    [ObservableProperty]
    private bool _cargando;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TieneMensaje))]
    private string _mensajeEstado = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TieneMensaje))]
    private string _mensajeError = string.Empty;

    /// <summary>
    /// True = mostrar formulario de datos del estudio. False = mostrar opciones Crear/Importar.
    /// </summary>
    [ObservableProperty]
    private bool _mostrarFormularioDatos;

    [ObservableProperty]
    private string _nombreEstudio = string.Empty;

    [ObservableProperty]
    private string? _nombreEmpresa;

    [ObservableProperty]
    private string? _direccion;

    [ObservableProperty]
    private string? _telefono;

    [ObservableProperty]
    private string? _email;

    public bool TieneMensaje => !string.IsNullOrEmpty(MensajeEstado) || !string.IsNullOrEmpty(MensajeError);

    /// <summary>
    /// Muestra el formulario para configurar los datos del estudio.
    /// </summary>
    [RelayCommand]
    private void CrearNuevoEstudio()
    {
        MensajeError = string.Empty;
        MensajeEstado = string.Empty;
        MostrarFormularioDatos = true;
    }

    /// <summary>
    /// Vuelve a la pantalla de opciones (Crear / Importar).
    /// </summary>
    [RelayCommand]
    private void Volver()
    {
        MensajeError = string.Empty;
        MensajeEstado = string.Empty;
        MostrarFormularioDatos = false;
    }

    /// <summary>
    /// Guarda los datos del estudio y abre la aplicación.
    /// </summary>
    [RelayCommand]
    private async Task GuardarDatosYContinuar()
    {
        if (string.IsNullOrWhiteSpace(NombreEstudio))
        {
            MensajeError = "El nombre del estudio es obligatorio.";
            return;
        }

        try
        {
            Cargando = true;
            MensajeError = string.Empty;
            MensajeEstado = "Guardando configuración...";

            await Task.Run(async () =>
            {
                using var db = new AtaenaDbContext();
                var cfg = await db.Configuracion.FirstOrDefaultAsync(c => c.Id == 1);
                if (cfg == null)
                {
                    cfg = new Configuracion { Id = 1 };
                    db.Configuracion.Add(cfg);
                }
                cfg.NombreEstudio = NombreEstudio.Trim();
                cfg.NombreEmpresa = string.IsNullOrWhiteSpace(NombreEmpresa) ? null : NombreEmpresa.Trim();
                cfg.Direccion = string.IsNullOrWhiteSpace(Direccion) ? null : Direccion.Trim();
                cfg.Telefono = string.IsNullOrWhiteSpace(Telefono) ? null : Telefono.Trim();
                cfg.Email = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim();
                await db.SaveChangesAsync();
            });

            MarcarSetupCompletado();
            MensajeEstado = "¡Listo! Abriendo Ataena...";
            await Task.Delay(400);

            SetupCompletado?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al guardar datos del estudio");
            MensajeError = $"Error al guardar: {ex.Message}";
        }
        finally
        {
            Cargando = false;
        }
    }

    [RelayCommand]
    private async Task ImportarDatos()
    {
        try
        {
            Cargando = true;
            MensajeError = string.Empty;
            MensajeEstado = string.Empty;

            var archivos = await _window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Seleccionar archivo de backup",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Backup Ataena")
                    {
                        Patterns = new[] { "*.zip" }
                    }
                }
            });

            if (archivos == null || archivos.Count == 0)
            {
                Cargando = false;
                return;
            }

            var rutaZip = archivos[0].Path.LocalPath;

            if (!BackupService.ValidarBackup(rutaZip))
            {
                MensajeError = "El archivo seleccionado no es un backup válido de Ataena.";
                Cargando = false;
                return;
            }

            MensajeEstado = "Restaurando backup...";

            var metadata = await _restauracionService.RestaurarBackupAsync(rutaZip, crearBackupActual: false);
            Log.Information("Backup restaurado en primer inicio: {Ruta}", rutaZip);

            MarcarSetupCompletado();
            MensajeEstado = "¡Datos importados! Abriendo Ataena...";
            await Task.Delay(400);

            SetupCompletado?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al importar datos");
            MensajeError = $"Error al restaurar: {ex.Message}";
        }
        finally
        {
            Cargando = false;
        }
    }

    private static void MarcarSetupCompletado()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var carpetaAtaena = Path.Combine(localAppData, "Ataena");
        var flagPath = Path.Combine(carpetaAtaena, ".setup_completado");

        Directory.CreateDirectory(carpetaAtaena);
        File.WriteAllText(flagPath, DateTime.UtcNow.ToString("O"));
        Log.Information("Setup completado, flag creado: {Path}", flagPath);
    }
}
