using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InkStudio.Data;
using InkStudio.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace InkStudio.ViewModels;

/// <summary>
/// ViewModel para la pantalla de Configuración del estudio.
/// Gestiona los datos básicos del estudio y la configuración SMTP.
/// </summary>
public partial class ConfiguracionViewModel : ViewModelBase
{
    private readonly InkStudioDbContext _db = new();

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
    private bool _temaOscuro = true;

    [ObservableProperty]
    private string _idiomaApp = "es";

    [ObservableProperty]
    private bool _cargando;

    [ObservableProperty]
    private string _mensajeError = string.Empty;

    [ObservableProperty]
    private string _mensajeOk = string.Empty;

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
            TemaOscuro = cfg.TemaOscuro;
            IdiomaApp = cfg.IdiomaApp;
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
            cfg.TemaOscuro = TemaOscuro;
            cfg.IdiomaApp = string.IsNullOrWhiteSpace(IdiomaApp) ? "es" : IdiomaApp.Trim();

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
}


