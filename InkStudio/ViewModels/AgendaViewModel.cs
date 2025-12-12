using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using InkStudio.Data;
using InkStudio.Models;
using Serilog;

namespace InkStudio.ViewModels;

/// <summary>
/// ViewModel para la gestión de la agenda y citas.
/// Permite ver citas en calendario y gestionar su estado.
/// </summary>
public partial class AgendaViewModel : ViewModelBase
{
    #region Campos Privados

    /// <summary>
    /// Contexto de base de datos.
    /// </summary>
    private readonly InkStudioDbContext _db = new();

    #endregion

    #region Propiedades - Vista de Calendario

    /// <summary>
    /// Fecha actualmente seleccionada en el calendario.
    /// </summary>
    [ObservableProperty]
    private DateTimeOffset? _fechaSeleccionada = DateTimeOffset.Now.Date;

    /// <summary>
    /// Modo de vista: Día, Semana o Mes.
    /// </summary>
    [ObservableProperty]
    private VistaAgenda _vistaActual = VistaAgenda.Semana;

    /// <summary>
    /// Se ejecuta cuando cambia VistaActual.
    /// </summary>
    partial void OnVistaActualChanged(VistaAgenda value)
    {
        OnPropertyChanged(nameof(MostrarCalendarioMes));
        OnPropertyChanged(nameof(MostrarCalendarioSemana));
        OnPropertyChanged(nameof(MostrarLista));
    }

    /// <summary>
    /// ViewModel del calendario visual.
    /// </summary>
    public CalendarViewModel CalendarVM { get; } = new();

    /// <summary>
    /// Indica si mostrar el calendario mensual (vista Mes).
    /// </summary>
    public bool MostrarCalendarioMes => VistaActual == VistaAgenda.Mes;

    /// <summary>
    /// Indica si mostrar el calendario semanal (vista Semana).
    /// </summary>
    public bool MostrarCalendarioSemana => VistaActual == VistaAgenda.Semana;

    /// <summary>
    /// Indica si mostrar la lista de citas (vista Día).
    /// </summary>
    public bool MostrarLista => VistaActual == VistaAgenda.Dia;

    /// <summary>
    /// Lista de citas para el período seleccionado.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<Cita> _citas = new();

    /// <summary>
    /// Cita actualmente seleccionada.
    /// </summary>
    [ObservableProperty]
    private Cita? _citaSeleccionada;

    #endregion

    #region Propiedades - Formulario de Cita

    /// <summary>
    /// Indica si el formulario de edición está visible.
    /// </summary>
    [ObservableProperty]
    private bool _mostrarFormulario = false;

    /// <summary>
    /// Indica si estamos editando (true) o creando (false) una cita.
    /// </summary>
    [ObservableProperty]
    private bool _esEdicion = false;

    /// <summary>
    /// Título del formulario.
    /// </summary>
    [ObservableProperty]
    private string _tituloFormulario = "Nueva Cita";

    // Campos del formulario
    [ObservableProperty]
    private Cliente? _clienteSeleccionado;

    [ObservableProperty]
    private DateTimeOffset? _fechaCita = DateTimeOffset.Now.Date;

    [ObservableProperty]
    private TimeSpan _horaInicio = new TimeSpan(10, 0, 0); // 10:00 por defecto

    /// <summary>
    /// Hora de inicio como string (HH:mm) para el formulario.
    /// </summary>
    [ObservableProperty]
    private string _horaInicioString = "10:00";

    [ObservableProperty]
    private int _duracionMinutos = 60;

    [ObservableProperty]
    private TipoCita _tipoCita = TipoCita.Tatuaje;

    [ObservableProperty]
    private string _descripcion = string.Empty;

    [ObservableProperty]
    private EstadoCita _estadoCita = EstadoCita.Pendiente;

    [ObservableProperty]
    private string _notas = string.Empty;

    #endregion

    #region Propiedades - Lista de Clientes

    /// <summary>
    /// Lista de clientes para el selector del formulario.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<Cliente> _clientes = new();

    /// <summary>
    /// Texto de búsqueda para filtrar clientes.
    /// </summary>
    [ObservableProperty]
    private string _textoBusquedaCliente = string.Empty;

    #endregion

    #region Propiedades - Filtros y Estado

    /// <summary>
    /// Filtro de estado de cita (null = todos).
    /// </summary>
    [ObservableProperty]
    private EstadoCita? _filtroEstado;

    /// <summary>
    /// Indica si hay una operación en curso.
    /// </summary>
    [ObservableProperty]
    private bool _cargando = false;

    /// <summary>
    /// Mensaje de error para mostrar al usuario.
    /// </summary>
    [ObservableProperty]
    private string _mensajeError = string.Empty;

    /// <summary>
    /// Total de citas en el período actual.
    /// </summary>
    [ObservableProperty]
    private int _totalCitas;

    #endregion

    #region Constructor

    /// <summary>
    /// Inicializa el ViewModel y carga los datos iniciales.
    /// </summary>
    public AgendaViewModel()
    {
        // Cargar clientes y citas al inicializar
        _ = CargarClientes();
        _ = CargarCitas();
    }

    #endregion

    #region Comandos - Navegación de Fechas

    /// <summary>
    /// Navega al día anterior.
    /// </summary>
    [RelayCommand]
    private void DiaAnterior()
    {
        if (FechaSeleccionada.HasValue)
        {
            FechaSeleccionada = FechaSeleccionada.Value.AddDays(-1);
        }
        else
        {
            FechaSeleccionada = DateTimeOffset.Now.Date.AddDays(-1);
        }
        _ = CargarCitas();
    }

    /// <summary>
    /// Navega al día siguiente.
    /// </summary>
    [RelayCommand]
    private void DiaSiguiente()
    {
        if (FechaSeleccionada.HasValue)
        {
            FechaSeleccionada = FechaSeleccionada.Value.AddDays(1);
        }
        else
        {
            FechaSeleccionada = DateTimeOffset.Now.Date.AddDays(1);
        }
        _ = CargarCitas();
    }

    /// <summary>
    /// Vuelve al día de hoy.
    /// </summary>
    [RelayCommand]
    private void IrAHoy()
    {
        FechaSeleccionada = DateTimeOffset.Now.Date;
        _ = CargarCitas();
    }

    /// <summary>
    /// Cambia el modo de vista (Día/Semana/Mes).
    /// </summary>
    [RelayCommand]
    private void CambiarVista(object? parametro)
    {
        if (parametro is VistaAgenda vista)
        {
            VistaActual = vista;
        }
        else if (parametro is string str && Enum.TryParse<VistaAgenda>(str, out var vistaParsed))
        {
            VistaActual = vistaParsed;
        }
        _ = CargarCitas();
    }

    #endregion

    #region Comandos - Carga de Datos

    /// <summary>
    /// Carga las citas del período seleccionado.
    /// </summary>
    [RelayCommand]
    private async Task CargarCitas()
    {
        try
        {
            Cargando = true;
            MensajeError = string.Empty;

            // Si no hay fecha seleccionada, usar hoy
            var fechaOffset = FechaSeleccionada ?? DateTimeOffset.Now.Date;
            var fecha = fechaOffset.Date; // Convertir a DateTime para la BD

            DateTime inicio, fin;

            switch (VistaActual)
            {
                case VistaAgenda.Dia:
                    inicio = fecha;
                    fin = inicio.AddDays(1);
                    break;
                case VistaAgenda.Semana:
                    // Lunes de la semana
                    var diasDesdeLunes = ((int)fecha.DayOfWeek + 6) % 7;
                    inicio = fecha.AddDays(-diasDesdeLunes);
                    fin = inicio.AddDays(7);
                    break;
                case VistaAgenda.Mes:
                    inicio = new DateTime(fecha.Year, fecha.Month, 1);
                    fin = inicio.AddMonths(1);
                    break;
                default:
                    inicio = fecha;
                    fin = inicio.AddDays(1);
                    break;
            }

            var query = _db.Citas
                .Include(c => c.Cliente)
                .Where(c => c.Fecha >= inicio && c.Fecha < fin);

            // Aplicar filtro de estado si existe
            if (FiltroEstado.HasValue)
            {
                query = query.Where(c => c.Estado == FiltroEstado.Value);
            }

            // Cargar sin ordenar por HoraInicio (SQLite no soporta TimeSpan en OrderBy)
            var lista = await query
                .OrderBy(c => c.Fecha)
                .ToListAsync();

            // Ordenar en memoria por HoraInicio
            var listaOrdenada = lista
                .OrderBy(c => c.Fecha)
                .ThenBy(c => c.HoraInicio)
                .ToList();

            Citas = new ObservableCollection<Cita>(listaOrdenada);
            TotalCitas = listaOrdenada.Count;

            // Actualizar calendario visual si estamos en vista Mes o Semana
            if (VistaActual == VistaAgenda.Mes || VistaActual == VistaAgenda.Semana)
            {
                var fechaCalendario = FechaSeleccionada ?? DateTimeOffset.Now.Date;
                CalendarVM.CargarMes(fechaCalendario, listaOrdenada);
                
                // Sincronizar la fecha seleccionada con el calendario
                if (FechaSeleccionada.HasValue)
                {
                    CalendarVM.FechaSeleccionada = FechaSeleccionada.Value.DateTime;
                }
            }

            var fechaLog = FechaSeleccionada ?? DateTimeOffset.Now.Date;
            Log.Debug("Citas cargadas: {Count} citas para {Vista} del {Fecha}", 
                TotalCitas, VistaActual, fechaLog.Date.ToString("dd/MM/yyyy"));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al cargar citas");
            MensajeError = $"Error al cargar citas: {ex.Message}";
        }
        finally
        {
            Cargando = false;
        }
    }

    /// <summary>
    /// Carga la lista de clientes activos.
    /// </summary>
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
            Log.Debug("Clientes cargados para selector: {Count}", lista.Count);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al cargar clientes para selector");
        }
    }

    #endregion

    #region Comandos - CRUD de Citas

    /// <summary>
    /// Abre el formulario para crear una nueva cita.
    /// </summary>
    [RelayCommand]
    private void NuevaCita()
    {
        LimpiarFormulario();
        FechaCita = FechaSeleccionada ?? DateTimeOffset.Now.Date;
        EsEdicion = false;
        TituloFormulario = "✨ Nueva Cita";
        MostrarFormulario = true;
    }

    /// <summary>
    /// Abre el formulario para editar la cita seleccionada.
    /// </summary>
    [RelayCommand]
    private void EditarCita()
    {
        if (CitaSeleccionada == null) return;

        CargarCitaEnFormulario(CitaSeleccionada);
        EsEdicion = true;
        TituloFormulario = "✏️ Editar Cita";
        MostrarFormulario = true;
    }

    /// <summary>
    /// Guarda la cita (crea nueva o actualiza existente).
    /// </summary>
    [RelayCommand]
    private async Task GuardarCita()
    {
        try
        {
            // Validación
            if (ClienteSeleccionado == null)
            {
                MensajeError = "Debes seleccionar un cliente";
                return;
            }

            if (!FechaCita.HasValue)
            {
                MensajeError = "Debes seleccionar una fecha";
                return;
            }

            var fechaCitaDateTime = FechaCita.Value.Date;
            if (fechaCitaDateTime < DateTime.Today && !EsEdicion)
            {
                MensajeError = "No se pueden crear citas en el pasado";
                return;
            }

            // Parsear hora desde string
            if (!TimeSpan.TryParse(HoraInicioString, out var horaParsed))
            {
                // Intentar formato HH:mm
                var parts = HoraInicioString.Split(':');
                if (parts.Length == 2 && 
                    int.TryParse(parts[0], out var hours) && 
                    int.TryParse(parts[1], out var minutes))
                {
                    horaParsed = new TimeSpan(hours, minutes, 0);
                }
                else
                {
                    MensajeError = "Formato de hora inválido. Usa HH:mm (ej: 10:30)";
                    return;
                }
            }

            Cargando = true;
            MensajeError = string.Empty;

            if (EsEdicion && CitaSeleccionada != null)
            {
                // Actualizar cita existente
                CitaSeleccionada.ClienteId = ClienteSeleccionado.Id;
                CitaSeleccionada.Fecha = fechaCitaDateTime;
                CitaSeleccionada.HoraInicio = horaParsed;
                CitaSeleccionada.DuracionMinutos = DuracionMinutos;
                CitaSeleccionada.TipoCita = TipoCita;
                CitaSeleccionada.Descripcion = string.IsNullOrWhiteSpace(Descripcion) ? null : Descripcion.Trim();
                CitaSeleccionada.Estado = EstadoCita;
                CitaSeleccionada.Notas = string.IsNullOrWhiteSpace(Notas) ? null : Notas.Trim();

                Log.Information("Cita {CitaId} actualizada", CitaSeleccionada.Id);
            }
            else
            {
                // Crear nueva cita
                var nuevaCita = new Cita
                {
                    ClienteId = ClienteSeleccionado.Id,
                    Fecha = fechaCitaDateTime,
                    HoraInicio = horaParsed,
                    DuracionMinutos = DuracionMinutos,
                    TipoCita = TipoCita,
                    Descripcion = string.IsNullOrWhiteSpace(Descripcion) ? null : Descripcion.Trim(),
                    Estado = EstadoCita,
                    Notas = string.IsNullOrWhiteSpace(Notas) ? null : Notas.Trim(),
                    FechaCreacion = DateTime.Now
                };

                _db.Citas.Add(nuevaCita);
                Log.Information("Nueva cita creada para cliente {ClienteId}", ClienteSeleccionado.Id);
            }

            await _db.SaveChangesAsync();
            
            MostrarFormulario = false;
            await CargarCitas();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al guardar cita");
            MensajeError = $"Error al guardar: {ex.Message}";
        }
        finally
        {
            Cargando = false;
        }
    }

    /// <summary>
    /// Elimina la cita seleccionada.
    /// </summary>
    [RelayCommand]
    private async Task EliminarCita()
    {
        if (CitaSeleccionada == null) return;

        try
        {
            Cargando = true;
            
            _db.Citas.Remove(CitaSeleccionada);
            await _db.SaveChangesAsync();
            
            Log.Information("Cita {CitaId} eliminada", CitaSeleccionada.Id);
            
            MostrarFormulario = false;
            await CargarCitas();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al eliminar cita {CitaId}", CitaSeleccionada.Id);
            MensajeError = $"Error al eliminar: {ex.Message}";
        }
        finally
        {
            Cargando = false;
        }
    }

    /// <summary>
    /// Cancela la edición y cierra el formulario.
    /// </summary>
    [RelayCommand]
    private void CancelarEdicion()
    {
        MostrarFormulario = false;
        MensajeError = string.Empty;
    }

    /// <summary>
    /// Cambia el estado de la cita seleccionada.
    /// </summary>
    [RelayCommand]
    private async Task CambiarEstadoCita(EstadoCita nuevoEstado)
    {
        if (CitaSeleccionada == null) return;

        try
        {
            CitaSeleccionada.Estado = nuevoEstado;
            await _db.SaveChangesAsync();
            
            Log.Information("Estado de cita {CitaId} cambiado a {Estado}", CitaSeleccionada.Id, nuevoEstado);
            
            await CargarCitas();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al cambiar estado de cita {CitaId}", CitaSeleccionada.Id);
            MensajeError = $"Error: {ex.Message}";
        }
    }

    #endregion

    #region Métodos Privados

    /// <summary>
    /// Limpia todos los campos del formulario.
    /// </summary>
    private void LimpiarFormulario()
    {
        ClienteSeleccionado = null;
        FechaCita = DateTimeOffset.Now.Date;
        HoraInicio = new TimeSpan(10, 0, 0);
        HoraInicioString = "10:00";
        DuracionMinutos = 60;
        TipoCita = TipoCita.Tatuaje;
        Descripcion = string.Empty;
        EstadoCita = EstadoCita.Pendiente;
        Notas = string.Empty;
        MensajeError = string.Empty;
    }

    /// <summary>
    /// Carga los datos de una cita en el formulario.
    /// </summary>
    /// <param name="cita">Cita a cargar.</param>
    private void CargarCitaEnFormulario(Cita cita)
    {
        ClienteSeleccionado = cita.Cliente;
        FechaCita = new DateTimeOffset(cita.Fecha);
        HoraInicio = cita.HoraInicio;
        HoraInicioString = cita.HoraInicio.ToString(@"hh\:mm");
        DuracionMinutos = cita.DuracionMinutos;
        TipoCita = cita.TipoCita;
        Descripcion = cita.Descripcion ?? string.Empty;
        EstadoCita = cita.Estado;
        Notas = cita.Notas ?? string.Empty;
        MensajeError = string.Empty;
    }

    #endregion
}

/// <summary>
/// Modo de vista de la agenda.
/// </summary>
public enum VistaAgenda
{
    /// <summary>
    /// Vista de un solo día.
    /// </summary>
    Dia = 0,

    /// <summary>
    /// Vista de una semana completa.
    /// </summary>
    Semana = 1,

    /// <summary>
    /// Vista de un mes completo.
    /// </summary>
    Mes = 2
}
