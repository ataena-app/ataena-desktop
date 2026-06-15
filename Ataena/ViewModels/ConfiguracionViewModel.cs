using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ataena.Data;
using Ataena.Models;
using Ataena.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Ataena.ViewModels;

/// <summary>
/// ViewModel para la pantalla de Configuración del estudio.
/// Gestiona los datos básicos del estudio y la configuración SMTP.
/// </summary>
public partial class ConfiguracionViewModel : ViewModelBase
{
    private readonly AtaenaDbContext _db = new();

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

    [ObservableProperty]
    private string? _smtpServidor;

    [ObservableProperty]
    private int _smtpPuerto = 587;

    [ObservableProperty]
    private string? _smtpUsuario;

    [ObservableProperty]
    private string? _smtpPassword;

    [ObservableProperty]
    private bool _smtpUsarSsl = true;

    [ObservableProperty]
    private bool _usarImpresora;

    [ObservableProperty]
    private bool _temaOscuro = true;

    [ObservableProperty]
    private string _idiomaApp = "es";

    // Dashboard
    [ObservableProperty]
    private bool _dashboardMostrarEconomia = false;

    [ObservableProperty]
    private bool _dashboardMostrarEstadisticas = true;

    [ObservableProperty]
    private bool _dashboardMostrarAlertas = true;

    [ObservableProperty]
    private bool _dashboardMostrarAccionesRapidas = true;

    [ObservableProperty]
    private bool _cargando;

    [ObservableProperty]
    private string _mensajeError = string.Empty;

    [ObservableProperty]
    private string _mensajeOk = string.Empty;

    /// <summary>
    /// Sección activa del menú de configuración.
    /// </summary>
    [ObservableProperty]
    private string _seccionActiva = "Estudio";

    public bool MostrarSeccionEstudio => SeccionActiva == "Estudio";
    public bool MostrarSeccionCorreo => SeccionActiva == "Correo";
    public bool MostrarSeccionImpresora => SeccionActiva == "Impresora";
    public bool MostrarSeccionDashboard => SeccionActiva == "Dashboard";
    public bool MostrarSeccionPreferencias => SeccionActiva == "Preferencias";

    partial void OnSeccionActivaChanged(string value)
    {
        OnPropertyChanged(nameof(MostrarSeccionEstudio));
        OnPropertyChanged(nameof(MostrarSeccionCorreo));
        OnPropertyChanged(nameof(MostrarSeccionImpresora));
        OnPropertyChanged(nameof(MostrarSeccionDashboard));
        OnPropertyChanged(nameof(MostrarSeccionPreferencias));
    }

    [RelayCommand]
    private void IrASeccionConfiguracion(string seccion)
    {
        SeccionActiva = seccion;
    }

    [ObservableProperty]
    private string? _logoPath;

    [ObservableProperty]
    private Bitmap? _logoPreview;

    public bool TieneLogo => !string.IsNullOrEmpty(LogoPath) && File.Exists(LogoPath);

    public ConfiguracionViewModel()
    {
        _ = CargarConfiguracion();
    }

    [RelayCommand]
    private async Task CargarConfiguracion()
    {
        try
        {
            Cargando = true;
            MensajeError = string.Empty;
            MensajeOk = string.Empty;

            var cfg = await _db.Configuracion.FirstOrDefaultAsync(c => c.Id == 1);
            if (cfg == null)
            {
                cfg = new Configuracion();
                _db.Configuracion.Add(cfg);
                await _db.SaveChangesAsync();
            }

            NombreEstudio = cfg.NombreEstudio;
            NombreEmpresa = cfg.NombreEmpresa;
            Direccion = cfg.Direccion;
            Telefono = cfg.Telefono;
            Email = cfg.Email;
            SmtpServidor = cfg.SmtpServidor;
            SmtpPuerto = cfg.SmtpPuerto;
            SmtpUsuario = cfg.SmtpUsuario;
            SmtpPassword = cfg.SmtpPassword;
            SmtpUsarSsl = cfg.SmtpUsarSsl;
            UsarImpresora = cfg.UsarImpresora;
            TemaOscuro = cfg.TemaOscuro;
            IdiomaApp = cfg.IdiomaApp;
            LogoPath = cfg.LogoPath;
            DashboardMostrarEconomia = cfg.DashboardMostrarEconomia;
            DashboardMostrarEstadisticas = cfg.DashboardMostrarEstadisticas;
            DashboardMostrarAlertas = cfg.DashboardMostrarAlertas;
            DashboardMostrarAccionesRapidas = cfg.DashboardMostrarAccionesRapidas;

            CargarLogoPreview();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al cargar configuración");
            MensajeError = $"Error al cargar configuración: {ex.Message}";
        }
        finally
        {
            Cargando = false;
        }
    }

    [RelayCommand]
    private async Task GuardarConfiguracion()
    {
        try
        {
            Cargando = true;
            MensajeError = string.Empty;
            MensajeOk = string.Empty;

            var cfg = await _db.Configuracion.FirstOrDefaultAsync(c => c.Id == 1);
            if (cfg == null)
            {
                cfg = new Configuracion();
                _db.Configuracion.Add(cfg);
            }

            cfg.NombreEstudio = string.IsNullOrWhiteSpace(NombreEstudio) ? "Mi Estudio" : NombreEstudio.Trim();
            cfg.NombreEmpresa = string.IsNullOrWhiteSpace(NombreEmpresa) ? null : NombreEmpresa.Trim();
            cfg.Direccion = string.IsNullOrWhiteSpace(Direccion) ? null : Direccion.Trim();
            cfg.Telefono = string.IsNullOrWhiteSpace(Telefono) ? null : Telefono.Trim();
            cfg.Email = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim();
            cfg.SmtpServidor = string.IsNullOrWhiteSpace(SmtpServidor) ? null : SmtpServidor.Trim();
            cfg.SmtpPuerto = SmtpPuerto;
            cfg.SmtpUsuario = string.IsNullOrWhiteSpace(SmtpUsuario) ? null : SmtpUsuario.Trim();
            cfg.SmtpPassword = string.IsNullOrWhiteSpace(SmtpPassword) ? null : SmtpPassword;
            cfg.SmtpUsarSsl = SmtpUsarSsl;
            cfg.UsarImpresora = UsarImpresora;
            cfg.TemaOscuro = TemaOscuro;
            cfg.IdiomaApp = string.IsNullOrWhiteSpace(IdiomaApp) ? "es" : IdiomaApp.Trim();
            cfg.LogoPath = LogoPath;
            cfg.DashboardMostrarEconomia = DashboardMostrarEconomia;
            cfg.DashboardMostrarEstadisticas = DashboardMostrarEstadisticas;
            cfg.DashboardMostrarAlertas = DashboardMostrarAlertas;
            cfg.DashboardMostrarAccionesRapidas = DashboardMostrarAccionesRapidas;

            await _db.SaveChangesAsync();
            MensajeOk = "Configuración guardada correctamente.";
            Log.Information("Configuración guardada");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al guardar configuración");
            MensajeError = $"Error al guardar configuración: {ex.Message}";
        }
        finally
        {
            Cargando = false;
        }
    }

    [RelayCommand]
    private async Task SeleccionarLogoAsync()
    {
        try
        {
            var topLevel = Avalonia.Application.Current?.ApplicationLifetime is 
                Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop 
                ? desktop.MainWindow 
                : null;

            if (topLevel == null)
            {
                MensajeError = "No se pudo abrir el selector de archivos.";
                return;
            }

            var archivos = await topLevel.StorageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions
            {
                Title = "Seleccionar logo del estudio",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new Avalonia.Platform.Storage.FilePickerFileType("Imágenes")
                    {
                        Patterns = new[] { "*.jpg", "*.jpeg", "*.png", "*.bmp", "*.gif" }
                    }
                }
            });

            if (archivos == null || archivos.Count == 0)
                return;

            var archivo = archivos[0];
            var rutaOrigen = archivo.Path.LocalPath;

            // Copiar a carpeta de la aplicación
            var carpetaLogos = Path.Combine(ConsentimientoPathService.ObtenerRutaBaseFicheros(), "logos");
            Directory.CreateDirectory(carpetaLogos);

            var extension = Path.GetExtension(rutaOrigen);
            var rutaDestino = Path.Combine(carpetaLogos, $"logo_estudio{extension}");

            File.Copy(rutaOrigen, rutaDestino, overwrite: true);
            LogoPath = rutaDestino;

            CargarLogoPreview();
            OnPropertyChanged(nameof(TieneLogo));

            Log.Information("Logo seleccionado: {Ruta}", rutaDestino);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al seleccionar logo");
            MensajeError = $"Error al seleccionar logo: {ex.Message}";
        }
    }

    [RelayCommand]
    private void QuitarLogo()
    {
        try
        {
            // Eliminar archivo si existe
            if (!string.IsNullOrEmpty(LogoPath) && File.Exists(LogoPath))
            {
                File.Delete(LogoPath);
            }

            LogoPath = null;
            LogoPreview = null;
            OnPropertyChanged(nameof(TieneLogo));

            Log.Information("Logo eliminado");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al quitar logo");
            MensajeError = $"Error al quitar logo: {ex.Message}";
        }
    }

    private void CargarLogoPreview()
    {
        try
        {
            if (string.IsNullOrEmpty(LogoPath) || !File.Exists(LogoPath))
            {
                LogoPreview = null;
                return;
            }

            using var stream = File.OpenRead(LogoPath);
            LogoPreview = new Bitmap(stream);
            OnPropertyChanged(nameof(TieneLogo));
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error al cargar preview del logo");
            LogoPreview = null;
        }
    }

    [RelayCommand]
    private async Task ProbarSmtpAsync()
    {
        try
        {
            Cargando = true;
            MensajeError = string.Empty;
            MensajeOk = string.Empty;

            // Primero guardar configuración actual
            await GuardarConfiguracion();

            var emailService = new EmailService(_db);
            var (exito, mensaje) = await emailService.ProbarConexionSmtpAsync();

            if (exito)
            {
                MensajeOk = $"✅ {mensaje}";
            }
            else
            {
                MensajeError = $"❌ {mensaje}";
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al probar conexión SMTP");
            MensajeError = $"Error: {ex.Message}";
        }
        finally
        {
            Cargando = false;
        }
    }
}


