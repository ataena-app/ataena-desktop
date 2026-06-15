using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
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
/// Acciones por consentimiento: Ver, Renovar y Borrar.
/// </summary>
public partial class ConsentimientosViewModel : ViewModelBase
{
    private readonly AtaenaDbContext _db = new();

    private EventHandler<Cliente>? _renovacionConsentimientoFirmaHandler;
    private EventHandler? _renovacionModalCerradoHandler;
    private CancellationTokenSource? _busquedaCts;

    [ObservableProperty]
    private ObservableCollection<Consentimiento> _consentimientos = new();

    [ObservableProperty]
    private string _textoBusqueda = string.Empty;

    partial void OnTextoBusquedaChanged(string value) => ProgramarBusquedaAutomatica();

    [ObservableProperty]
    private TipoConsentimiento? _tipoFiltro;

    [ObservableProperty]
    private int _totalConsentimientos;

    [ObservableProperty]
    private bool _cargando;

    [ObservableProperty]
    private string _mensajeError = string.Empty;

    [ObservableProperty]
    private ConsentimientoFirmaViewModel? _consentimientoFirmaVM;

    public ConsentimientosViewModel()
    {
        _ = CargarConsentimientos();
    }

    private void ProgramarBusquedaAutomatica()
    {
        _busquedaCts?.Cancel();
        _busquedaCts?.Dispose();
        _busquedaCts = new CancellationTokenSource();
        var token = _busquedaCts.Token;
        _ = AplicarBusquedaAutomaticaAsync(token);
    }

    private async Task AplicarBusquedaAutomaticaAsync(CancellationToken token)
    {
        try
        {
            await Task.Delay(250, token);
            await CargarConsentimientos();
        }
        catch (OperationCanceledException)
        {
            // Nueva pulsación de tecla
        }
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
                .Where(c => c.Firmado)
                .AsQueryable();

            if (TipoFiltro.HasValue)
                query = query.Where(c => c.Tipo == TipoFiltro.Value);

            var lista = await query
                .OrderByDescending(c => c.FechaFirma)
                .ThenByDescending(c => c.Id)
                .ToListAsync();

            if (!string.IsNullOrWhiteSpace(TextoBusqueda))
            {
                var termino = TextoBusquedaHelper.Normalizar(TextoBusqueda);
                var terminoDigitos = TextoBusquedaHelper.SoloDigitos(TextoBusqueda);
                lista = lista
                    .Where(c => c.Cliente != null &&
                                TextoBusquedaHelper.ClienteCoincide(c.Cliente, termino, terminoDigitos))
                    .ToList();
            }

            Consentimientos = new ObservableCollection<Consentimiento>(lista);
            TotalConsentimientos = lista.Count;
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

    partial void OnTipoFiltroChanged(TipoConsentimiento? value) => _ = CargarConsentimientos();

    [RelayCommand]
    private Task LimpiarFiltros()
    {
        TextoBusqueda = string.Empty;
        TipoFiltro = null;
        return CargarConsentimientos();
    }

    [RelayCommand]
    private Task AbrirConsentimiento(Consentimiento consentimiento)
    {
        try
        {
            if (string.IsNullOrEmpty(consentimiento.RutaDocumento) ||
                !System.IO.File.Exists(consentimiento.RutaDocumento))
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
    private async Task RenovarConsentimiento(Consentimiento? consentimientoAnterior)
    {
        if (consentimientoAnterior == null || !consentimientoAnterior.Firmado)
            return;

        try
        {
            var cliente = consentimientoAnterior.Cliente ??
                await _db.Clientes.FindAsync(consentimientoAnterior.ClienteId);
            if (cliente == null)
            {
                MensajeError = "Cliente no encontrado.";
                return;
            }

            await _db.Entry(cliente).Collection(c => c.Consentimientos).LoadAsync();

            var tipoNuevo = consentimientoAnterior.Tipo;
            if (consentimientoAnterior.EsConsentimientoMenor && !cliente.EsMenorDeEdad)
            {
                tipoNuevo = consentimientoAnterior.Tipo switch
                {
                    TipoConsentimiento.RGPD_Menor => TipoConsentimiento.RGPD,
                    TipoConsentimiento.Trabajo_Menor => TipoConsentimiento.Trabajo,
                    TipoConsentimiento.Imagenes_Menor => TipoConsentimiento.Imagenes,
                    _ => consentimientoAnterior.Tipo
                };
            }

            ConsentimientoFirmaVM ??= new ConsentimientoFirmaViewModel();
            QuitarHandlersRenovacionConsentimientoTemporal();

            var idCliente = cliente.Id;
            var idAnterior = consentimientoAnterior.Id;
            var trabajo = consentimientoAnterior.TrabajoId.HasValue
                ? await _db.Trabajos.FindAsync(consentimientoAnterior.TrabajoId.Value)
                : null;

            _renovacionConsentimientoFirmaHandler = async (_, clienteFirmado) =>
            {
                try
                {
                    QuitarHandlersRenovacionConsentimientoTemporal();
                    if (clienteFirmado.Id != idCliente)
                        return;

                    await using var cx = new AtaenaDbContext();
                    var anteriorEnBd = await cx.Consentimientos.FindAsync(idAnterior);
                    if (anteriorEnBd != null && !anteriorEnBd.Renovado)
                    {
                        anteriorEnBd.Renovado = true;
                        await cx.SaveChangesAsync();
                    }

                    await CargarConsentimientos();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error al marcar consentimiento anterior {Id} como renovado", idAnterior);
                }
            };
            _renovacionModalCerradoHandler = (_, _) => QuitarHandlersRenovacionConsentimientoTemporal();

            ConsentimientoFirmaVM.FirmaCompletada += _renovacionConsentimientoFirmaHandler;
            ConsentimientoFirmaVM.ModalSesionFinalizada += _renovacionModalCerradoHandler;

            await ConsentimientoFirmaVM.AbrirModal(cliente, tipoNuevo, trabajo,
                omitirConsentimientoIdRenovacion: idAnterior);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al renovar consentimiento {ConsentimientoId}", consentimientoAnterior.Id);
            MensajeError = $"Error al renovar: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task EliminarConsentimiento(Consentimiento? consentimiento)
    {
        if (consentimiento == null || !consentimiento.Firmado)
            return;

        if (consentimiento.Cliente == null)
            await _db.Entry(consentimiento).Reference(c => c.Cliente).LoadAsync();

        var nombreCliente = consentimiento.Cliente?.NombreCompleto ?? "el cliente";
        var mensaje =
            $"Se eliminará el consentimiento «{consentimiento.NombreTipo}» y su PDF.\n\n" +
            ConsentimientoService.MensajeAvisoTrasEliminar(consentimiento.Tipo, nombreCliente) +
            "\n\nEsta acción no se puede deshacer.";

        var confirmado = await DialogService.ConfirmarAccionAsync(
            titulo: "Eliminar consentimiento",
            mensaje: mensaje,
            botonConfirmar: "Sí, eliminar",
            esPeligroso: true);

        if (!confirmado)
            return;

        try
        {
            var (exito, tipo, _) =
                await ConsentimientoService.EliminarConsentimientoAsync(_db, consentimiento.Id);
            if (!exito)
            {
                MensajeError = "No se pudo eliminar el consentimiento.";
                return;
            }

            await CargarConsentimientos();
            OverlayNotificationService.Mostrar(
                ConsentimientoService.MensajeAvisoTrasEliminar(tipo, nombreCliente),
                OverlayNotificationKind.Warning);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al eliminar consentimiento {ConsentimientoId}", consentimiento.Id);
            MensajeError = $"Error al eliminar: {ex.Message}";
        }
    }

    private void QuitarHandlersRenovacionConsentimientoTemporal()
    {
        if (ConsentimientoFirmaVM == null)
            return;

        if (_renovacionConsentimientoFirmaHandler != null)
        {
            ConsentimientoFirmaVM.FirmaCompletada -= _renovacionConsentimientoFirmaHandler;
            _renovacionConsentimientoFirmaHandler = null;
        }

        if (_renovacionModalCerradoHandler != null)
        {
            ConsentimientoFirmaVM.ModalSesionFinalizada -= _renovacionModalCerradoHandler;
            _renovacionModalCerradoHandler = null;
        }
    }
}
