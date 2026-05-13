using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ataena.Data;
using Ataena.Models;
using Ataena.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Ataena.ViewModels;

/// <summary>
/// ViewModel para la vista global de consentimientos.
/// Permite listar, filtrar y abrir/descargar/enviar consentimientos.
/// </summary>
public partial class ConsentimientosViewModel : ViewModelBase
{
    private readonly AtaenaDbContext _db = new();

    [ObservableProperty]
    private ObservableCollection<Consentimiento> _consentimientos = new();

    [ObservableProperty]
    private Cliente? _clienteFiltro;

    [ObservableProperty]
    private TipoConsentimiento? _tipoFiltro;

    [ObservableProperty]
    private ObservableCollection<Cliente> _clientes = new();

    [ObservableProperty]
    private bool _cargando;

    [ObservableProperty]
    private string _mensajeError = string.Empty;

    public ConsentimientosViewModel()
    {
        _ = CargarFiltrosYDatos();
    }

    private async Task CargarFiltrosYDatos()
    {
        await CargarClientes();
        await CargarConsentimientos();
    }

    [RelayCommand]
    private async Task CargarConsentimientos()
    {
        try
        {
            Cargando = true;
            MensajeError = string.Empty;

            var query = _db.Consentimientos
                .Include(c => c.Cliente)
                .Include(c => c.Trabajo)
                .AsQueryable();

            if (ClienteFiltro != null)
            {
                query = query.Where(c => c.ClienteId == ClienteFiltro.Id);
            }

            if (TipoFiltro.HasValue)
            {
                query = query.Where(c => c.Tipo == TipoFiltro.Value);
            }

            var lista = await query
                .OrderByDescending(c => c.FechaFirma)
                .ThenByDescending(c => c.Id)
                .ToListAsync();

            Consentimientos = new ObservableCollection<Consentimiento>(lista);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al cargar consentimientos");
            MensajeError = $"Error al cargar consentimientos: {ex.Message}";
        }
        finally
        {
            Cargando = false;
        }
    }

    [RelayCommand]
    private async Task CargarClientes()
    {
        try
        {
            var lista = await _db.Clientes
                .Where(c => c.Activo)
                .OrderBy(c => c.Nombre)
                .ThenBy(c => c.Apellidos)
                .ToListAsync();

            Clientes = new ObservableCollection<Cliente>(lista);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al cargar clientes para filtros de consentimientos");
        }
    }

    partial void OnClienteFiltroChanged(Cliente? value)
    {
        _ = CargarConsentimientos();
    }

    partial void OnTipoFiltroChanged(TipoConsentimiento? value)
    {
        _ = CargarConsentimientos();
    }

    [RelayCommand]
    private Task LimpiarFiltros()
    {
        ClienteFiltro = null;
        TipoFiltro = null;
        return CargarConsentimientos();
    }

    [RelayCommand]
    private Task AbrirConsentimiento(Consentimiento consentimiento)
    {
        try
        {
            if (string.IsNullOrEmpty(consentimiento.RutaDocumento) || !System.IO.File.Exists(consentimiento.RutaDocumento))
            {
                MensajeError = "No se encontró el archivo PDF del consentimiento.";
                return Task.CompletedTask;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = consentimiento.RutaDocumento,
                UseShellExecute = true
            });

            Log.Information("PDF de consentimiento {ConsentimientoId} abierto", consentimiento.Id);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al abrir consentimiento {ConsentimientoId}", consentimiento.Id);
            MensajeError = $"Error al abrir el PDF: {ex.Message}";
        }

        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task ExportarConsentimiento(Consentimiento consentimiento)
    {
        try
        {
            if (string.IsNullOrEmpty(consentimiento.RutaDocumento) || !System.IO.File.Exists(consentimiento.RutaDocumento))
            {
                MensajeError = "No se encontró el archivo PDF del consentimiento para exportar.";
                return Task.CompletedTask;
            }

            var origen = consentimiento.RutaDocumento;
            var nombreArchivo = System.IO.Path.GetFileName(origen);
            var destinoCarpeta = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            var destino = System.IO.Path.Combine(destinoCarpeta, nombreArchivo);

            System.IO.File.Copy(origen, destino, overwrite: true);

            Log.Information("Consentimiento {ConsentimientoId} exportado a {Destino}", consentimiento.Id, destino);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al exportar consentimiento {ConsentimientoId}", consentimiento.Id);
            MensajeError = $"Error al exportar el PDF: {ex.Message}";
        }

        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task EnviarConsentimientoPorCorreo(Consentimiento consentimiento)
    {
        try
        {
            if (consentimiento.Cliente == null)
            {
                // Cargar cliente si no está cargado
                _db.Entry(consentimiento).Reference(c => c.Cliente).Load();
            }

            if (string.IsNullOrEmpty(consentimiento.Cliente?.Email))
            {
                MensajeError = "El cliente no tiene email configurado.";
                return Task.CompletedTask;
            }

            // Abrir cliente de correo predeterminado
            Process.Start(new ProcessStartInfo
            {
                FileName = $"mailto:{consentimiento.Cliente.Email}?subject=Consentimiento%20{consentimiento.NombreTipo}&body=Adjunto%20el%20consentimiento%20firmado.",
                UseShellExecute = true
            });

            // Abrir también el PDF para que el usuario lo adjunte
            if (!string.IsNullOrEmpty(consentimiento.RutaDocumento) && System.IO.File.Exists(consentimiento.RutaDocumento))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = consentimiento.RutaDocumento,
                    UseShellExecute = true
                });
            }

            Log.Information("Cliente de correo abierto para consentimiento {ConsentimientoId}", consentimiento.Id);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al enviar consentimiento {ConsentimientoId} por correo", consentimiento.Id);
            MensajeError = $"Error al abrir el cliente de correo: {ex.Message}";
        }

        return Task.CompletedTask;
    }
}


