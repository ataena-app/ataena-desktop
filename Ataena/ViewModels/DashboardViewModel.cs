using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Ataena.Data;
using Ataena.Models;
using Serilog;

namespace Ataena.ViewModels;

/// <summary>
/// ViewModel para la pantalla principal (Dashboard).
/// Muestra resumen de citas del día, estadísticas y alertas.
/// </summary>
/// <remarks>
/// El Dashboard es la primera pantalla que ve el usuario.
/// Proporciona una vista rápida del estado del negocio.
/// </remarks>
public partial class DashboardViewModel : ViewModelBase
{
    #region Campos Privados

    /// <summary>
    /// Contexto de base de datos.
    /// </summary>
    private readonly AtaenaDbContext _db = new();

    /// <summary>
    /// Referencia al ViewModel principal para poder navegar y abrir formularios desde el Dashboard.
    /// </summary>
    private MainWindowViewModel? _mainWindowViewModel;

    #endregion

    #region Propiedades - Citas del Día

    /// <summary>
    /// Lista de citas programadas para hoy.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<Cita> _citasHoy = new();

    #endregion

    #region Propiedades - Estadísticas

    /// <summary>
    /// Número total de clientes activos.
    /// </summary>
    [ObservableProperty]
    private int _totalClientes;

    /// <summary>
    /// Número de citas para el día de hoy.
    /// </summary>
    [ObservableProperty]
    private int _citasHoyCount;

    /// <summary>
    /// Total de ingresos de la semana actual.
    /// </summary>
    [ObservableProperty]
    private decimal _ingresosSemana;

    /// <summary>
    /// Número de citas pendientes de confirmar (próximos 7 días).
    /// </summary>
    [ObservableProperty]
    private int _citasPendientesConfirmar;

    #endregion

    #region Propiedades - Alertas

    /// <summary>
    /// Lista de alertas y notificaciones importantes.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<string> _alertas = new();

    #endregion

    #region Propiedades - Interfaz de Usuario

    /// <summary>
    /// Saludo que cambia según la hora del día.
    /// </summary>
    [ObservableProperty]
    private string _saludo = "Buenos días";

    /// <summary>
    /// Fecha actual formateada para mostrar en la UI.
    /// </summary>
    [ObservableProperty]
    private string _fechaHoy = DateTime.Now.ToString("dddd, d MMMM yyyy");

    /// <summary>
    /// Nombre del estudio (desde configuración).
    /// </summary>
    [ObservableProperty]
    private string _nombreEstudio = "Ataena";

    #endregion

    #region Constructor

    /// <summary>
    /// Inicializa el ViewModel del Dashboard.
    /// </summary>
    public DashboardViewModel()
    {
        ActualizarSaludo();
    }

    /// <summary>
    /// Permite inyectar el MainWindowViewModel para navegación y acciones rápidas.
    /// </summary>
    /// <param name="mainWindowViewModel">Instancia principal.</param>
    public void SetMainWindowViewModel(MainWindowViewModel mainWindowViewModel)
    {
        _mainWindowViewModel = mainWindowViewModel;
    }

    #endregion

    #region Comandos

    /// <summary>
    /// Carga todos los datos del Dashboard.
    /// Se ejecuta al mostrar la vista.
    /// </summary>
    [RelayCommand]
    private async Task CargarDatos()
    {
        Log.Debug("Cargando datos del Dashboard");
        try
        {
            await CargarCitasHoy();
            await CargarEstadisticas();
            await CargarAlertas();
            await CargarConfiguracion();
            Log.Debug("Dashboard cargado exitosamente");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al cargar datos del Dashboard");
            throw;
        }
    }

    /// <summary>
    /// Acción rápida: navegar a Clientes y abrir el modal de nuevo cliente.
    /// </summary>
    [RelayCommand]
    private async Task NuevoClienteRapido()
    {
        if (_mainWindowViewModel == null) return;

        // Navegar primero a Clientes
        await _mainWindowViewModel.IrAClientesCommand.ExecuteAsync(null);

        // Abrir el formulario de nuevo cliente
        _mainWindowViewModel.ClientesVM.NuevoClienteCommand.Execute(null);
    }

    /// <summary>
    /// Acción rápida: navegar a Agenda y abrir el modal de nueva cita.
    /// </summary>
    [RelayCommand]
    private void NuevaCitaRapida()
    {
        if (_mainWindowViewModel == null) return;

        // Navegar a Agenda
        _mainWindowViewModel.IrAAgendaCommand.Execute(null);

        // Abrir formulario de nueva cita
        _mainWindowViewModel.AgendaVM.NuevaCitaCommand.Execute(null);
    }

    /// <summary>
    /// Acción rápida: navegar a Trabajos y abrir el modal de nuevo trabajo.
    /// </summary>
    [RelayCommand]
    private void NuevoTrabajoRapido()
    {
        if (_mainWindowViewModel == null) return;

        // Navegar a Trabajos
        _mainWindowViewModel.IrATrabajosCommand.Execute(null);

        // Abrir formulario de nuevo trabajo
        _mainWindowViewModel.TrabajosVM.NuevoTrabajoCommand.Execute(null);
    }

    /// <summary>
    /// Marca una cita como confirmada.
    /// </summary>
    /// <param name="cita">Cita a confirmar.</param>
    [RelayCommand]
    private async Task MarcarCitaConfirmada(Cita cita)
    {
        try
        {
            if (cita.Estado == EstadoCita.Pendiente)
            {
                cita.Estado = EstadoCita.Confirmada;
                await _db.SaveChangesAsync();
                Log.Information("Cita {CitaId} marcada como confirmada", cita.Id);
                await CargarDatos();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al confirmar cita {CitaId}", cita.Id);
            throw;
        }
    }

    /// <summary>
    /// Marca una cita como completada.
    /// </summary>
    /// <param name="cita">Cita a completar.</param>
    [RelayCommand]
    private async Task MarcarCitaCompletada(Cita cita)
    {
        try
        {
            if (cita.Estado == EstadoCita.Confirmada || cita.Estado == EstadoCita.EnProceso)
            {
                cita.Estado = EstadoCita.Completada;
                await _db.SaveChangesAsync();
                Log.Information("Cita {CitaId} marcada como completada", cita.Id);
                await CargarDatos();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al completar cita {CitaId}", cita.Id);
            throw;
        }
    }

    #endregion

    #region Métodos Privados

    /// <summary>
    /// Actualiza el saludo según la hora del día.
    /// Mañana (< 12), Tarde (12-20), Noche (> 20).
    /// </summary>
    private void ActualizarSaludo()
    {
        var hora = DateTime.Now.Hour;
        Saludo = hora switch
        {
            < 12 => "Buenos días 👋",
            < 20 => "Buenas tardes 👋",
            _ => "Buenas noches 👋"
        };
    }

    /// <summary>
    /// Carga las citas del día actual.
    /// </summary>
    /// <remarks>
    /// El ordenamiento se hace en memoria porque SQLite
    /// no soporta OrderBy con TimeSpan.
    /// </remarks>
    private async Task CargarCitasHoy()
    {
        try
        {
            var hoy = DateTime.Today;
            var citas = await _db.Citas
                .Include(c => c.Cliente)
                .Include(c => c.Trabajo!)
                    .ThenInclude(t => t.Consentimiento)
                .Where(c => c.Fecha.Date == hoy)
                .ToListAsync();

            // Ordenar en memoria (SQLite no soporta OrderBy con TimeSpan)
            var citasOrdenadas = citas.OrderBy(c => c.HoraInicio).ToList();

            CitasHoy = new ObservableCollection<Cita>(citasOrdenadas);
            CitasHoyCount = citasOrdenadas.Count;
            
            Log.Debug("Citas de hoy cargadas: {Count} citas", citasOrdenadas.Count);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al cargar citas de hoy");
            throw;
        }
    }

    /// <summary>
    /// Carga las estadísticas generales del negocio.
    /// </summary>
    private async Task CargarEstadisticas()
    {
        // Total de clientes activos
        TotalClientes = await _db.Clientes.CountAsync(c => c.Activo);

        // Citas pendientes de confirmar (próximos 7 días)
        var enUnaSemana = DateTime.Today.AddDays(7);
        CitasPendientesConfirmar = await _db.Citas
            .CountAsync(c => c.Estado == EstadoCita.Pendiente &&
                            c.Fecha >= DateTime.Today &&
                            c.Fecha <= enUnaSemana);

        // Ingresos de la semana (Lunes a Domingo)
        var inicioSemana = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek + 1);
        var finSemana = inicioSemana.AddDays(7);
        IngresosSemana = await _db.Trabajos
            .Where(t => t.Fecha >= inicioSemana && t.Fecha < finSemana)
            .SumAsync(t => t.Precio);
    }

    /// <summary>
    /// Carga alertas y notificaciones importantes.
    /// </summary>
    private async Task CargarAlertas()
    {
        var alertas = new ObservableCollection<string>();

        // Alerta: Citas sin confirmar para mañana
        var manana = DateTime.Today.AddDays(1);
        var citasManana = await _db.Citas
            .CountAsync(c => c.Fecha.Date == manana && c.Estado == EstadoCita.Pendiente);

        if (citasManana > 0)
        {
            alertas.Add($"📅 {citasManana} cita(s) sin confirmar para mañana");
        }

        // Alerta: Clientes sin consentimiento RGPD firmado
        var sinRgpd = await _db.Clientes
            .CountAsync(c => c.Activo && !c.Consentimientos.Any(
                con => con.Tipo == TipoConsentimiento.RGPD && con.Firmado));

        if (sinRgpd > 0)
        {
            alertas.Add($"📝 {sinRgpd} cliente(s) sin consentimiento RGPD");
        }

        Alertas = alertas;
    }

    /// <summary>
    /// Carga la configuración del estudio.
    /// </summary>
    private async Task CargarConfiguracion()
    {
        var config = await _db.Configuracion.FirstOrDefaultAsync();
        if (config != null)
        {
            NombreEstudio = config.NombreEstudio;
        }
    }

    #endregion
}
