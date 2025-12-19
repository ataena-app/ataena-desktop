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

    /// <summary>
    /// Referencia opcional al ViewModel de trabajos (para crear trabajos desde el modal de cita).
    /// </summary>
    private TrabajosViewModel? _trabajosVM;

    /// <summary>
    /// Referencia opcional al ViewModel principal (para navegación).
    /// </summary>
    private MainWindowViewModel? _mainWindowVM;

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

    /// <summary>
    /// Se ejecuta cuando cambia ClienteSeleccionado.
    /// </summary>
    partial void OnClienteSeleccionadoChanged(Cliente? value)
    {
        OnPropertyChanged(nameof(TieneClienteSeleccionado));
        
        if (value != null)
        {
            _ = CargarTrabajosDelCliente(value.Id);
        }
        else
        {
            TrabajosDelCliente.Clear();
            TrabajoSeleccionado = null;
            TieneConsentimientoTrabajo = false;
            MensajeConsentimiento = string.Empty;
        }
        OnPropertyChanged(nameof(ColorFondoConsentimiento));
    }

    /// <summary>
    /// Trabajo seleccionado para la cita.
    /// </summary>
    [ObservableProperty]
    private Trabajo? _trabajoSeleccionado;

    /// <summary>
    /// Indica si hay un cliente seleccionado (para mostrar el selector de trabajo).
    /// </summary>
    public bool TieneClienteSeleccionado => ClienteSeleccionado != null;

    /// <summary>
    /// Se ejecuta cuando cambia TrabajoSeleccionado.
    /// </summary>
    partial void OnTrabajoSeleccionadoChanged(Trabajo? value)
    {
        if (value != null)
        {
            _ = VerificarConsentimientoTrabajo(value.Id);
        }
        else
        {
            TieneConsentimientoTrabajo = false;
            MensajeConsentimiento = string.Empty;
        }
        OnPropertyChanged(nameof(ColorFondoConsentimiento));
    }

    [ObservableProperty]
    private DateTimeOffset? _fechaCita = DateTimeOffset.Now.Date;

    [ObservableProperty]
    private TimeSpan _horaInicio = new TimeSpan(10, 0, 0); // 10:00 por defecto

    /// <summary>
    /// Hora de inicio como string (HH:mm) para el formulario.
    /// </summary>
    [ObservableProperty]
    private string _horaInicioString = "10:00";

    /// <summary>
    /// Se ejecuta cuando cambia HoraInicioString.
    /// </summary>
    partial void OnHoraInicioStringChanged(string value)
    {
        CalcularHoraFin();
    }

    /// <summary>
    /// Hora de fin como string (HH:mm) para el formulario.
    /// Se calcula automáticamente o se puede editar manualmente.
    /// </summary>
    [ObservableProperty]
    private string _horaFinString = "11:00";

    [ObservableProperty]
    private int _duracionMinutos = 60;

    /// <summary>
    /// Se ejecuta cuando cambia DuracionMinutos.
    /// </summary>
    partial void OnDuracionMinutosChanged(int value)
    {
        CalcularHoraFin();
    }

    [ObservableProperty]
    private TipoCita _tipoCita = TipoCita.Tatuaje;

    [ObservableProperty]
    private string _descripcion = string.Empty;

    [ObservableProperty]
    private EstadoCita _estadoCita = EstadoCita.Pendiente;

    [ObservableProperty]
    private string _notas = string.Empty;

    #endregion

    #region Propiedades - Consentimientos

    /// <summary>
    /// Indica si el cliente seleccionado tiene consentimiento de Trabajo firmado.
    /// Este consentimiento es necesario para crear citas/trabajos.
    /// </summary>
    [ObservableProperty]
    private bool _tieneConsentimientoTrabajo = false;

    /// <summary>
    /// Mensaje sobre el estado del consentimiento de trabajo del cliente.
    /// </summary>
    [ObservableProperty]
    private string _mensajeConsentimiento = string.Empty;

    /// <summary>
    /// Color de fondo para el indicador de consentimiento.
    /// Verde si tiene consentimiento de trabajo, rojo si no.
    /// </summary>
    public Avalonia.Media.SolidColorBrush ColorFondoConsentimiento => TieneConsentimientoTrabajo 
        ? new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#2d5a2d"))
        : new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#5a2d2d"));

    #endregion

    #region Propiedades - Lista de Clientes

    /// <summary>
    /// Lista de clientes para el selector del formulario.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<Cliente> _clientes = new();

    /// <summary>
    /// Lista de trabajos del cliente seleccionado.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<Trabajo> _trabajosDelCliente = new();

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
                .Include(c => c.Trabajo)
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
                if (VistaActual == VistaAgenda.Semana)
                {
                    CalendarVM.CargarSemana(fechaCalendario, listaOrdenada);
                }
                else
                {
                    CalendarVM.CargarMes(fechaCalendario, listaOrdenada);
                }
                
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

            if (TrabajoSeleccionado == null)
            {
                MensajeError = "Debes seleccionar un trabajo";
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

            // Parsear hora de inicio desde string
            if (!TimeSpan.TryParse(HoraInicioString, out var horaInicioParsed))
            {
                // Intentar formato HH:mm
                var parts = HoraInicioString.Split(':');
                if (parts.Length == 2 && 
                    int.TryParse(parts[0], out var hours) && 
                    int.TryParse(parts[1], out var minutes))
                {
                    horaInicioParsed = new TimeSpan(hours, minutes, 0);
                }
                else
                {
                    MensajeError = "Formato de hora de inicio inválido. Usa HH:mm (ej: 10:30)";
                    return;
                }
            }

            // Parsear hora de fin desde string (si fue editada manualmente)
            TimeSpan? horaFinParsed = null;
            if (!string.IsNullOrWhiteSpace(HoraFinString))
            {
                if (TimeSpan.TryParse(HoraFinString, out var horaFin))
                {
                    horaFinParsed = horaFin;
                }
                else
                {
                    // Intentar formato HH:mm
                    var partsFin = HoraFinString.Split(':');
                    if (partsFin.Length == 2 &&
                        int.TryParse(partsFin[0], out var hoursFin) &&
                        int.TryParse(partsFin[1], out var minutesFin))
                    {
                        horaFinParsed = new TimeSpan(hoursFin, minutesFin, 0);
                    }
                }
            }

            // Si se editó la hora fin, recalcular duración
            if (horaFinParsed.HasValue)
            {
                var nuevaDuracion = (int)(horaFinParsed.Value - horaInicioParsed).TotalMinutes;
                if (nuevaDuracion > 0)
                {
                    DuracionMinutos = nuevaDuracion;
                }
                else
                {
                    MensajeError = "La hora de fin debe ser posterior a la hora de inicio";
                    return;
                }
            }

            // Validar que la hora fin sea mayor que la hora inicio
            if (horaFinParsed.HasValue && horaFinParsed.Value <= horaInicioParsed)
            {
                MensajeError = "La hora de fin debe ser posterior a la hora de inicio";
                return;
            }

            // Validar duración mínima
            if (DuracionMinutos < 15)
            {
                MensajeError = "La duración mínima es de 15 minutos";
                return;
            }

            var horaParsed = horaInicioParsed;

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
                
                // Actualizar vinculación del trabajo
                if (TrabajoSeleccionado != null)
                {
                    TrabajoSeleccionado.CitaId = CitaSeleccionada.Id;
                }

                await _db.SaveChangesAsync();
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
                await _db.SaveChangesAsync(); // Guardar primero para obtener el ID de la cita
                
                // Vincular el trabajo a la cita
                if (TrabajoSeleccionado != null)
                {
                    TrabajoSeleccionado.CitaId = nuevaCita.Id;
                    await _db.SaveChangesAsync();
                }
                
                Log.Information("Nueva cita creada para cliente {ClienteId} vinculada a trabajo {TrabajoId}", 
                    ClienteSeleccionado.Id, TrabajoSeleccionado?.Id);
            }
            
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

    /// <summary>
    /// Comando para crear un nuevo trabajo.
    /// </summary>
    [RelayCommand]
    private void CrearTrabajoNuevo()
    {
        if (ClienteSeleccionado == null)
        {
            MensajeError = "Primero debes seleccionar un cliente";
            return;
        }
        
        // Si tenemos referencia a TrabajosVM y MainWindowVM, navegar y abrir formulario
        if (_trabajosVM != null && _mainWindowVM != null)
        {
            // Cerrar el modal de cita
            MostrarFormulario = false;
            
            // Navegar a la vista de Trabajos
            _mainWindowVM.IrATrabajosCommand.Execute(null);
            
            // Abrir el formulario de nuevo trabajo con el cliente pre-seleccionado
            _trabajosVM.NuevoTrabajoParaCliente(ClienteSeleccionado);
            
            Log.Information("Navegando a Trabajos para crear trabajo para cliente {ClienteId}", ClienteSeleccionado.Id);
        }
        else
        {
            MensajeError = "No se puede crear trabajo desde aquí. Por favor, ve a la sección de Trabajos.";
            Log.Warning("Intento de crear trabajo sin referencias a TrabajosVM o MainWindowVM");
        }
    }

    /// <summary>
    /// Establece la referencia al ViewModel de trabajos.
    /// </summary>
    public void SetTrabajosViewModel(TrabajosViewModel trabajosVM)
    {
        _trabajosVM = trabajosVM;
    }

    /// <summary>
    /// Establece la referencia al ViewModel principal.
    /// </summary>
    public void SetMainWindowViewModel(MainWindowViewModel mainWindowVM)
    {
        _mainWindowVM = mainWindowVM;
    }

    #endregion

    #region Métodos Privados

    /// <summary>
    /// Limpia todos los campos del formulario.
    /// </summary>
    private void LimpiarFormulario()
    {
        ClienteSeleccionado = null;
        TrabajoSeleccionado = null;
        TrabajosDelCliente.Clear();
        FechaCita = DateTimeOffset.Now.Date;
        HoraInicio = new TimeSpan(10, 0, 0);
        HoraInicioString = "10:00";
        DuracionMinutos = 60;
        CalcularHoraFin(); // Calcular hora fin inicial
        TipoCita = TipoCita.Tatuaje;
        Descripcion = string.Empty;
        EstadoCita = EstadoCita.Pendiente;
        Notas = string.Empty;
        MensajeError = string.Empty;
        TieneConsentimientoTrabajo = false;
        MensajeConsentimiento = string.Empty;
    }

    /// <summary>
    /// Carga los datos de una cita en el formulario.
    /// </summary>
    /// <param name="cita">Cita a cargar.</param>
    private async void CargarCitaEnFormulario(Cita cita)
    {
        ClienteSeleccionado = cita.Cliente;
        
        // Cargar trabajos del cliente
        if (cita.Cliente != null)
        {
            await CargarTrabajosDelCliente(cita.Cliente.Id);
        }
        
        // Cargar trabajo asociado a la cita (si existe)
        if (cita.Trabajo != null)
        {
            TrabajoSeleccionado = cita.Trabajo;
        }
        
        FechaCita = new DateTimeOffset(cita.Fecha);
        HoraInicio = cita.HoraInicio;
        HoraInicioString = cita.HoraInicio.ToString(@"hh\:mm");
        DuracionMinutos = cita.DuracionMinutos;
        CalcularHoraFin(); // Calcular hora fin basada en inicio y duración
        TipoCita = cita.TipoCita;
        Descripcion = cita.Descripcion ?? string.Empty;
        EstadoCita = cita.Estado;
        Notas = cita.Notas ?? string.Empty;
        MensajeError = string.Empty;
    }

    /// <summary>
    /// Calcula la hora de fin basándose en la hora de inicio y la duración.
    /// </summary>
    private void CalcularHoraFin()
    {
        if (TimeSpan.TryParse(HoraInicioString, out var inicio))
        {
            var fin = inicio.Add(TimeSpan.FromMinutes(DuracionMinutos));
            HoraFinString = fin.ToString(@"hh\:mm");
        }
        else
        {
            // Intentar parsear formato HH:mm manualmente
            var parts = HoraInicioString.Split(':');
            if (parts.Length == 2 &&
                int.TryParse(parts[0], out var hours) &&
                int.TryParse(parts[1], out var minutes))
            {
                var inicioManual = new TimeSpan(hours, minutes, 0);
                var fin = inicioManual.Add(TimeSpan.FromMinutes(DuracionMinutos));
                HoraFinString = fin.ToString(@"hh\:mm");
            }
        }
    }

    /// <summary>
    /// Carga los trabajos del cliente seleccionado.
    /// </summary>
    private async Task CargarTrabajosDelCliente(int clienteId)
    {
        try
        {
            var trabajos = await _db.Trabajos
                .Where(t => t.ClienteId == clienteId)
                .OrderByDescending(t => t.Fecha)
                .ToListAsync();
            
            TrabajosDelCliente.Clear();
            foreach (var trabajo in trabajos)
            {
                TrabajosDelCliente.Add(trabajo);
            }
            
            Log.Debug("Trabajos cargados para cliente {ClienteId}: {Count}", clienteId, trabajos.Count);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al cargar trabajos para cliente {ClienteId}", clienteId);
        }
    }

    /// <summary>
    /// Verifica el consentimiento del trabajo seleccionado.
    /// </summary>
    /// <param name="trabajoId">ID del trabajo a verificar.</param>
    private async Task VerificarConsentimientoTrabajo(int trabajoId)
    {
        try
        {
            var consentimiento = await _db.Consentimientos
                .FirstOrDefaultAsync(c => c.TrabajoId == trabajoId && 
                                         c.Tipo == TipoConsentimiento.Trabajo && 
                                         c.Firmado);

            TieneConsentimientoTrabajo = consentimiento != null;
            
            if (!TieneConsentimientoTrabajo)
            {
                MensajeConsentimiento = "⚠️ Este trabajo no tiene consentimiento firmado";
            }
            else
            {
                MensajeConsentimiento = "✅ Consentimiento de Trabajo verificado";
            }
            
            OnPropertyChanged(nameof(ColorFondoConsentimiento));
            Log.Debug("Consentimiento de trabajo verificado para trabajo {TrabajoId}: TieneConsentimiento={TieneConsentimiento}",
                trabajoId, TieneConsentimientoTrabajo);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al verificar consentimiento del trabajo {TrabajoId}", trabajoId);
            MensajeConsentimiento = "⚠️ Error al verificar consentimiento";
        }
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
