using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Ataena.Data;
using Ataena.Models;
using Ataena.Services;
using Serilog;

namespace Ataena.ViewModels;

/// <summary>
/// ViewModel para la gestión de la agenda y citas.
/// Permite ver citas en calendario y gestionar su estado.
/// </summary>
public partial class AgendaViewModel : ViewModelBase
{
    #region Tipos auxiliares

    /// <summary>
    /// Información de un día en la vista semanal.
    /// </summary>
    public class DiaSemanaInfo
    {
        public DateTime Fecha { get; set; }
        public string Etiqueta { get; set; } = string.Empty; // Ej: "Lun 17"
        public bool EsHoy { get; set; }
        public bool EsFinDeSemana => Fecha.DayOfWeek == DayOfWeek.Saturday || Fecha.DayOfWeek == DayOfWeek.Sunday;
    }

    /// <summary>
    /// Slot horario en la vista semanal (ej: 08:00, 08:30) con información
    /// de si es una hora "en punto" (para dibujar líneas más fuertes).
    /// </summary>
    public class HoraSemanaSlot
    {
        public string Etiqueta { get; set; } = string.Empty; // "08:00"
        public bool EsHoraCompleta { get; set; }             // true si mm == 00
    }

    /// <summary>
    /// Información de una cita posicionada en el calendario semanal.
    /// Contiene la cita original y sus coordenadas en el grid (columna, fila, rowspan).
    /// </summary>
    public class CitaSemanaInfo : ObservableObject
    {
        public Cita Cita { get; set; } = null!;
        public int Columna { get; set; }      // 0-6 (Lunes-Domingo)
        public int Fila { get; set; }         // Índice en HorasSemana
        public int RowSpan { get; set; }      // Número de slots de 30 minutos que ocupa
        
        /// <summary>
        /// Lista de citas superpuestas en la misma posición (incluyendo esta).
        /// Si hay más de una, significa que hay superposiciones.
        /// </summary>
        public List<CitaSemanaInfo> CitasSuperpuestas { get; set; } = new();
        
        /// <summary>
        /// Índice de esta cita dentro del grupo de citas superpuestas (para apilar visualmente).
        /// </summary>
        public int IndiceEnGrupo { get; set; } = 0;
        
        /// <summary>
        /// Número máximo de citas que se solapan simultáneamente en cualquier punto del tiempo.
        /// Se usa para calcular el ancho de cada cita cuando hay superposiciones.
        /// </summary>
        public int MaxSuperposicionesSimultaneas { get; set; } = 1;
        
        private double _left;
        public double Left 
        { 
            get => _left; 
            set => SetProperty(ref _left, value); 
        }
        
        private double _top;
        public double Top 
        { 
            get => _top; 
            set => SetProperty(ref _top, value); 
        }
        
        private double _width;
        public double Width 
        { 
            get => _width; 
            set => SetProperty(ref _width, value); 
        }
        
        private double _height;
        public double Height 
        { 
            get => _height; 
            set => SetProperty(ref _height, value); 
        }
        
        /// <summary>
        /// Indica si esta cita tiene otras citas superpuestas.
        /// </summary>
        public bool TieneSuperposiciones => CitasSuperpuestas.Count > 1;
        
        /// <summary>
        /// Número total de citas superpuestas en esta posición.
        /// </summary>
        public int NumeroCitasSuperpuestas => CitasSuperpuestas.Count;
    }

    /// <summary>
    /// Información de un día en la vista mensual.
    /// </summary>
    public class DiaMesInfo : ObservableObject
    {
        public DateTime Fecha { get; set; }
        public int Dia => Fecha.Day;
        public bool EsHoy => Fecha.Date == DateTime.Today;
        public bool EsDelMesActual { get; set; } = true;
        public bool EsFinDeSemana => Fecha.DayOfWeek == DayOfWeek.Saturday || Fecha.DayOfWeek == DayOfWeek.Sunday;
        
        /// <summary>
        /// Indica si es un día festivo.
        /// </summary>
        public bool EsFestivo { get; set; }
        
        /// <summary>
        /// Nombre del festivo (si aplica).
        /// </summary>
        public string? NombreFestivo { get; set; }
        
        /// <summary>
        /// Color del festivo para mostrar en el calendario.
        /// </summary>
        public string ColorFestivo { get; set; } = "#dc2626";
        
        /// <summary>
        /// Tipo de festivo (Nacional, Autonómico, Local).
        /// </summary>
        public TipoFestivo? TipoFestivo { get; set; }
        
        /// <summary>
        /// Número de citas en este día.
        /// </summary>
        public int NumeroCitas { get; set; }
        
        /// <summary>
        /// Lista de citas del día (máximo 3-4 para mostrar en el calendario).
        /// </summary>
        public ObservableCollection<Cita> CitasDelDia { get; set; } = new();
        
        /// <summary>
        /// Indica si hay más citas de las que se muestran.
        /// </summary>
        public bool TieneMasCitas => NumeroCitas > 3;
        
        /// <summary>
        /// Texto de "más citas" (ej: "+2 más").
        /// </summary>
        public string TextoMasCitas => TieneMasCitas ? $"+{NumeroCitas - 3} más" : string.Empty;
        
        /// <summary>
        /// Color de fondo según el estado del día.
        /// </summary>
        public string ColorFondo
        {
            get
            {
                if (EsFestivo) return "#1c1917"; // Fondo oscuro para festivos
                if (!EsDelMesActual) return "#0c0a09"; // Muy oscuro para días de otros meses
                if (EsFinDeSemana) return "#1c1917"; // Oscuro para fines de semana
                return "#171717"; // Normal
            }
        }
    }

    /// <summary>
    /// Estado del formulario de cita al navegar temporalmente a crear un trabajo.
    /// </summary>
    private sealed class CitaFormularioSnapshot
    {
        public bool EsEdicion { get; init; }
        public int? CitaId { get; init; }
        public int? ClienteId { get; init; }
        public int? TrabajoId { get; init; }
        public DateTimeOffset? FechaCita { get; init; }
        public string HoraInicioString { get; init; } = "10:00";
        public int DuracionMinutos { get; init; } = 30;
        public TipoCita TipoCita { get; init; }
        public string Descripcion { get; init; } = string.Empty;
        public EstadoCita EstadoCita { get; init; }
        public string Notas { get; init; } = string.Empty;
        public string TituloFormulario { get; init; } = "✨ Nueva Cita";
        public bool AjustarDuracionPorTipo { get; init; } = true;
    }

    #endregion

    #region Campos Privados

    /// <summary>
    /// Contexto de base de datos.
    /// </summary>
    private readonly AtaenaDbContext _db = new();

    /// <summary>
    /// Servicio de gestión de festivos.
    /// </summary>
    private readonly FestivosService _festivosService = new();

    /// <summary>
    /// Referencia opcional al ViewModel de trabajos (para crear trabajos desde el modal de cita).
    /// </summary>
    private TrabajosViewModel? _trabajosVM;

    /// <summary>
    /// Referencia opcional al ViewModel principal (para navegación).
    /// </summary>
    private MainWindowViewModel? _mainWindowVM;

    /// <summary>
    /// Formulario de cita guardado al ir a crear un trabajo y volver.
    /// </summary>
    private CitaFormularioSnapshot? _citaFormularioRetorno;

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
        OnPropertyChanged(nameof(MostrarLista));
        OnPropertyChanged(nameof(MostrarSemana));
        OnPropertyChanged(nameof(MostrarMes));

        // Cuando entramos en vista Semana, recalcular la semana actual
        if (value == VistaAgenda.Semana)
        {
            ActualizarSemana();
        }
        // Cuando entramos en vista Mes, recalcular el mes actual
        else if (value == VistaAgenda.Mes)
        {
            _ = ActualizarMesAsync();
        }
    }

    /// <summary>
    /// Indica si mostrar la lista de citas (solo modo Día).
    /// </summary>
    public bool MostrarLista => VistaActual == VistaAgenda.Dia;

    /// <summary>
    /// Indica si mostrar la nueva vista semanal personalizada.
    /// </summary>
    public bool MostrarSemana => VistaActual == VistaAgenda.Semana;

    /// <summary>
    /// Indica si mostrar la vista mensual.
    /// </summary>
    public bool MostrarMes => VistaActual == VistaAgenda.Mes;

    /// <summary>
    /// Información de los días de la semana actual (para la cabecera del calendario semanal).
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<DiaSemanaInfo> _diasSemana = new();

    /// <summary>
    /// Lista de horas para la vista semanal (08:00, 08:30, ..., 22:00).
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<HoraSemanaSlot> _horasSemana = new();

    /// <summary>
    /// Mes y año actual de la semana mostrada (ej: "Enero 2024").
    /// </summary>
    [ObservableProperty]
    private string _mesAnioActual = string.Empty;

    /// <summary>
    /// Lista de citas para el período seleccionado.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<Cita> _citas = new();

    /// <summary>
    /// Lista de citas posicionadas para la vista semanal (con coordenadas en el grid).
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<CitaSemanaInfo> _citasSemana = new();

    /// <summary>
    /// Lista de días para la vista mensual (6 semanas x 7 días = 42 días).
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<DiaMesInfo> _diasMes = new();

    /// <summary>
    /// Lista de festivos del mes actual.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<DiaFestivo> _festivosMes = new();

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
        OnPropertyChanged(nameof(EsClienteSeleccionadoMenor));
        
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
    /// Indica si el cliente seleccionado es menor de edad.
    /// Solo devuelve true si hay cliente seleccionado Y es menor.
    /// </summary>
    public bool EsClienteSeleccionadoMenor => ClienteSeleccionado != null && ClienteSeleccionado.EsMenorDeEdad;

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
    private int _duracionMinutos = 30; // Por defecto 30 min (tatuaje)

    /// <summary>
    /// Se ejecuta cuando cambia DuracionMinutos.
    /// </summary>
    partial void OnDuracionMinutosChanged(int value)
    {
        CalcularHoraFin();
    }

    [ObservableProperty]
    private TipoCita _tipoCita = TipoCita.Tatuaje;

    /// <summary>
    /// Indica si se debe ajustar la duración automáticamente al cambiar el tipo de cita.
    /// Se desactiva cuando se crea una cita desde drag & drop (la duración viene del área seleccionada).
    /// </summary>
    private bool _ajustarDuracionPorTipo = true;

    /// <summary>
    /// Se ejecuta cuando cambia TipoCita para ajustar la duración predeterminada.
    /// </summary>
    partial void OnTipoCitaChanged(TipoCita value)
    {
        // Ajustar duración según el tipo de cita (solo desde botón, no desde drag & drop ni edición)
        if (_ajustarDuracionPorTipo && !EsEdicion)
        {
            DuracionMinutos = value switch
            {
                TipoCita.Piercing => 15,
                TipoCita.Tatuaje => 30,
                TipoCita.Consulta => 30,
                _ => 30
            };
        }
    }

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

    /// <summary>
    /// Indica si se debe mostrar un aviso sobre consentimiento pendiente.
    /// </summary>
    [ObservableProperty]
    private bool _mostrarAvisoConsentimiento = false;

    /// <summary>
    /// Mensaje del aviso de consentimiento.
    /// </summary>
    [ObservableProperty]
    private string _mensajeAvisoConsentimiento = string.Empty;

    /// <summary>
    /// Trabajo que necesita consentimiento (para el aviso).
    /// </summary>
    [ObservableProperty]
    private Trabajo? _trabajoPendienteConsentimiento;

    /// <summary>
    /// ViewModel del modal de firma de consentimientos.
    /// </summary>
    [ObservableProperty]
    private ConsentimientoFirmaViewModel? _consentimientoFirmaVM;

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

    partial void OnFiltroEstadoChanged(EstadoCita? value)
    {
        _ = CargarCitas();
    }

    /// <summary>
    /// Fecha mínima seleccionable (hoy): no se permiten citas en el pasado.
    /// </summary>
    public DateTimeOffset FechaMinimaCita => DateTimeOffset.Now.Date;

    /// <summary>
    /// Mini-calendario mensual expandido en el formulario de cita.
    /// </summary>
    [ObservableProperty]
    private bool _mostrarCalendarioCitaExpandido;

    [RelayCommand]
    private void AlternarCalendarioCita()
    {
        MostrarCalendarioCitaExpandido = !MostrarCalendarioCitaExpandido;
    }

    partial void OnFechaCitaChanged(DateTimeOffset? value)
    {
        if (value.HasValue && value.Value.Date < DateTime.Today)
            FechaCita = DateTimeOffset.Now.Date;

        if (MostrarCalendarioCitaExpandido)
            MostrarCalendarioCitaExpandido = false;
    }

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
        var fechaActual = FechaSeleccionada?.Date ?? DateTime.Now.Date;
        
        if (VistaActual == VistaAgenda.Mes)
        {
            // En vista Mes: retroceder un mes
            FechaSeleccionada = new DateTimeOffset(fechaActual.AddMonths(-1));
        }
        else if (VistaActual == VistaAgenda.Semana)
        {
            // En vista Semana: retroceder 7 días
            FechaSeleccionada = new DateTimeOffset(fechaActual.AddDays(-7));
        }
        else
        {
            // En vista Día: retroceder 1 día
            FechaSeleccionada = new DateTimeOffset(fechaActual.AddDays(-1));
        }
        _ = CargarCitas();
    }

    /// <summary>
    /// Navega al día siguiente.
    /// </summary>
    [RelayCommand]
    private void DiaSiguiente()
    {
        var fechaActual = FechaSeleccionada?.Date ?? DateTime.Now.Date;
        
        if (VistaActual == VistaAgenda.Mes)
        {
            // En vista Mes: avanzar un mes
            FechaSeleccionada = new DateTimeOffset(fechaActual.AddMonths(1));
        }
        else if (VistaActual == VistaAgenda.Semana)
        {
            // En vista Semana: avanzar 7 días
            FechaSeleccionada = new DateTimeOffset(fechaActual.AddDays(7));
        }
        else
        {
            // En vista Día: avanzar 1 día
            FechaSeleccionada = new DateTimeOffset(fechaActual.AddDays(1));
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

    #region Comandos - Carga de Datos y Vista Semanal

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
                    .ThenInclude(cl => cl.Consentimientos)
                .Include(c => c.Trabajo!)
                    .ThenInclude(t => t.Consentimiento)
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

            // Si estamos en vista semanal, actualizar la estructura de semana
            // (ActualizarSemana ya llama a CalcularPosicionesCitas internamente)
            if (VistaActual == VistaAgenda.Semana)
            {
                ActualizarSemana();
                // Forzar notificación de cambio para que el Canvas recalcule posiciones
                OnPropertyChanged(nameof(CitasSemana));
            }
            // Si estamos en vista mensual, actualizar la estructura del mes
            else if (VistaActual == VistaAgenda.Mes)
            {
                await ActualizarMesAsync();
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
    /// Calcula los días y horas de la semana actual para la vista semanal personalizada.
    /// </summary>
    private void ActualizarSemana()
    {
        // Determinar la fecha de referencia (día seleccionado o hoy)
        var referenciaOffset = FechaSeleccionada ?? DateTimeOffset.Now.Date;
        var referencia = referenciaOffset.Date;

        // Calcular lunes de la semana (Lunes = 0)
        var diasDesdeLunes = ((int)referencia.DayOfWeek + 6) % 7;
        var inicioSemana = referencia.AddDays(-diasDesdeLunes);

        DiasSemana.Clear();
        for (int i = 0; i < 7; i++)
        {
            var fecha = inicioSemana.AddDays(i);
            var esHoy = fecha.Date == DateTime.Today;

            string nombreDia = fecha.DayOfWeek switch
            {
                DayOfWeek.Monday => "Lun",
                DayOfWeek.Tuesday => "Mar",
                DayOfWeek.Wednesday => "Mié",
                DayOfWeek.Thursday => "Jue",
                DayOfWeek.Friday => "Vie",
                DayOfWeek.Saturday => "Sáb",
                DayOfWeek.Sunday => "Dom",
                _ => fecha.ToString("ddd")
            };

            var etiqueta = $"{nombreDia} {fecha:dd}";

            DiasSemana.Add(new DiaSemanaInfo
            {
                Fecha = fecha,
                Etiqueta = etiqueta,
                EsHoy = esHoy
            });
        }

        // Generar lista de horas (08:00 - 22:00 en pasos de 30 minutos)
        HorasSemana.Clear();
        var horaInicio = TimeSpan.FromHours(8);
        var horaFin = TimeSpan.FromHours(22);
        for (var hora = horaInicio; hora <= horaFin; hora = hora.Add(TimeSpan.FromMinutes(30)))
        {
            HorasSemana.Add(new HoraSemanaSlot
            {
                Etiqueta = hora.ToString(@"hh\:mm"),
                EsHoraCompleta = hora.Minutes == 0
            });
        }

        // Actualizar el mes y año actual (usar el día del medio de la semana para evitar confusión)
        var diaMedio = inicioSemana.AddDays(3); // Miércoles
        MesAnioActual = diaMedio.ToString("MMMM yyyy", System.Globalization.CultureInfo.GetCultureInfo("es-ES"));

        // Recalcular posiciones de citas después de actualizar la semana
        CalcularPosicionesCitas();
        
        // Forzar notificación de cambio para que el Canvas recalcule posiciones
        OnPropertyChanged(nameof(CitasSemana));
    }

    /// <summary>
    /// Calcula los días del mes actual para la vista mensual.
    /// Incluye festivos y resumen de citas por día.
    /// </summary>
    private async Task ActualizarMesAsync()
    {
        try
        {
            Cargando = true;
            
            // Determinar el mes de referencia
            var referenciaOffset = FechaSeleccionada ?? DateTimeOffset.Now.Date;
            var referencia = referenciaOffset.Date;
            var primerDiaMes = new DateTime(referencia.Year, referencia.Month, 1);
            var ultimoDiaMes = primerDiaMes.AddMonths(1).AddDays(-1);

            // Actualizar título del mes
            MesAnioActual = primerDiaMes.ToString("MMMM yyyy", System.Globalization.CultureInfo.GetCultureInfo("es-ES"));

            // Calcular el primer día a mostrar (puede ser del mes anterior)
            var diasDesdeLunes = ((int)primerDiaMes.DayOfWeek + 6) % 7;
            var primerDiaGrid = primerDiaMes.AddDays(-diasDesdeLunes);

            // Cargar festivos del mes desde API y locales
            var festivos = await _festivosService.ObtenerFestivosMesAsync(referencia.Year, referencia.Month);
            FestivosMes = new ObservableCollection<DiaFestivo>(festivos);

            // Inicializar festivos locales de Guadalajara si es necesario
            await _festivosService.InicializarFestivosLocalesGuadalajaraAsync(referencia.Year);

            // Crear diccionario de festivos por fecha para acceso rápido
            var festivosPorFecha = festivos.ToDictionary(f => f.Fecha.Date, f => f);

            // Cargar citas del mes
            // Cargar citas sin ordenar por TimeSpan (SQLite no lo soporta)
            var citasMesRaw = await _db.Citas
                .Include(c => c.Cliente)
                .Include(c => c.Trabajo)
                .Where(c => c.Fecha >= primerDiaMes && c.Fecha <= ultimoDiaMes)
                .OrderBy(c => c.Fecha)
                .ToListAsync();

            // Ordenar en memoria por HoraInicio
            var citasMes = citasMesRaw
                .OrderBy(c => c.Fecha)
                .ThenBy(c => c.HoraInicio)
                .ToList();

            // Agrupar citas por día
            var citasPorDia = citasMes.GroupBy(c => c.Fecha.Date).ToDictionary(g => g.Key, g => g.ToList());

            // Generar 42 días (6 semanas completas)
            DiasMes.Clear();
            for (int i = 0; i < 42; i++)
            {
                var fecha = primerDiaGrid.AddDays(i);
                var esDelMesActual = fecha.Month == referencia.Month;

                // Buscar festivo para esta fecha
                festivosPorFecha.TryGetValue(fecha.Date, out var festivo);

                // Obtener citas del día
                citasPorDia.TryGetValue(fecha.Date, out var citasDelDia);
                var listaCitas = citasDelDia ?? new List<Cita>();

                var diaInfo = new DiaMesInfo
                {
                    Fecha = fecha,
                    EsDelMesActual = esDelMesActual,
                    EsFestivo = festivo != null,
                    NombreFestivo = festivo?.Nombre,
                    ColorFestivo = festivo?.ColorFondo ?? "#dc2626",
                    TipoFestivo = festivo?.Tipo,
                    NumeroCitas = listaCitas.Count,
                    CitasDelDia = new ObservableCollection<Cita>(listaCitas.Take(3))
                };

                DiasMes.Add(diaInfo);
            }

            // Actualizar total de citas
            TotalCitas = citasMes.Count;

            Log.Information("📅 Vista mensual actualizada: {Mes} {Anio}, {Festivos} festivos, {Citas} citas", 
                referencia.Month, referencia.Year, festivos.Count, citasMes.Count);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al actualizar vista mensual");
            MensajeError = $"Error al cargar el mes: {ex.Message}";
        }
        finally
        {
            Cargando = false;
        }
    }

    /// <summary>
    /// Calcula las posiciones (columna, fila, rowspan) de las citas en el grid semanal.
    /// </summary>
    private void CalcularPosicionesCitas()
    {
        CitasSemana.Clear();

        // Si no estamos en vista semanal o no hay días/horas calculados, salir
        if (VistaActual != VistaAgenda.Semana || DiasSemana.Count == 0 || HorasSemana.Count == 0)
        {
            Log.Debug("CalcularPosicionesCitas: No se puede calcular - VistaActual={Vista}, DiasSemana={Dias}, HorasSemana={Horas}", 
                VistaActual, DiasSemana.Count, HorasSemana.Count);
            return;
        }

        Log.Information("🔍 CalcularPosicionesCitas: Iniciando cálculo. Citas totales: {Count}", Citas.Count);

        // Obtener el lunes de la semana actual
        var referenciaOffset = FechaSeleccionada ?? DateTimeOffset.Now.Date;
        var referencia = referenciaOffset.Date;
        var diasDesdeLunes = ((int)referencia.DayOfWeek + 6) % 7;
        var inicioSemana = referencia.AddDays(-diasDesdeLunes);

        Log.Debug("🔍 Semana de referencia: {InicioSemana} (Lunes)", inicioSemana);

        // Crear diccionario de fecha -> índice de columna (0-6)
        var fechaAColumna = new Dictionary<DateTime, int>();
        for (int i = 0; i < DiasSemana.Count; i++)
        {
            fechaAColumna[DiasSemana[i].Fecha.Date] = i;
            Log.Debug("🔍 Día {Index}: {Fecha} -> Columna {Columna}", i, DiasSemana[i].Fecha.Date, i);
        }

        // Crear diccionario de hora -> índice de fila
        var horaAFila = new Dictionary<TimeSpan, int>();
        for (int i = 0; i < HorasSemana.Count; i++)
        {
            if (TimeSpan.TryParse(HorasSemana[i].Etiqueta, out var hora))
            {
                horaAFila[hora] = i;
            }
        }

        Log.Debug("🔍 Slots de hora disponibles: {Count}", horaAFila.Count);

        // Procesar cada cita
        foreach (var cita in Citas)
        {
            Log.Debug("🔍 Procesando cita {CitaId}: Fecha={Fecha}, Hora={Hora}", cita.Id, cita.Fecha.Date, cita.HoraInicio);
            
            // Calcular columna (día de la semana)
            if (!fechaAColumna.TryGetValue(cita.Fecha.Date, out var columna))
            {
                Log.Debug("🔍 Cita {CitaId} no está en esta semana (Fecha: {Fecha})", cita.Id, cita.Fecha.Date);
                continue; // La cita no está en esta semana
            }
            
            Log.Debug("🔍 Cita {CitaId} está en columna {Columna} (día {Fecha})", cita.Id, columna, cita.Fecha.Date);

            // Calcular fila (hora de inicio)
            // Redondear hacia abajo al slot de 30 minutos más cercano
            var minutosInicio = (int)cita.HoraInicio.TotalMinutes;
            var minutosRedondeados = (minutosInicio / 30) * 30;
            var horaInicioRedondeada = TimeSpan.FromMinutes(minutosRedondeados);

            // Buscar el slot exacto que coincida con la hora redondeada
            int fila = -1;
            for (int i = 0; i < HorasSemana.Count; i++)
            {
                if (TimeSpan.TryParse(HorasSemana[i].Etiqueta, out var horaSlot))
                {
                    // Comparar horas redondeadas (solo horas y minutos, ignorar segundos)
                    if (horaSlot.Hours == horaInicioRedondeada.Hours && 
                        horaSlot.Minutes == horaInicioRedondeada.Minutes)
                    {
                        fila = i;
                        break;
                    }
                }
            }

            // Si no encontramos fila válida, saltar esta cita
            if (fila < 0 || fila >= HorasSemana.Count)
            {
                Log.Warning("⚠️ Cita {CitaId} no se puede posicionar: hora {Hora} no coincide con ningún slot (slots disponibles: {Slots})", 
                    cita.Id, cita.HoraInicio, HorasSemana.Count);
                continue;
            }
            
            Log.Debug("🔍 Cita {CitaId} está en fila {Fila} (hora {Hora})", cita.Id, fila, cita.HoraInicio);

            // Calcular RowSpan (duración en slots de 30 minutos)
            var duracionMinutos = cita.DuracionMinutos;
            var rowSpan = Math.Max(1, (int)Math.Ceiling(duracionMinutos / 30.0));

            // Asegurar que no se salga del grid
            if (fila + rowSpan > HorasSemana.Count)
            {
                rowSpan = HorasSemana.Count - fila;
            }

            // Calcular posiciones para Canvas (usando valores relativos que se ajustarán)
            // Asumimos un ancho base por columna y altura de 32px por fila
            // Estos valores se ajustarán cuando el Canvas tenga su tamaño real
            const double anchoColumnaBase = 120.0; // Ancho estimado por columna
            const double altoFila = 32.0;
            
            CitasSemana.Add(new CitaSemanaInfo
            {
                Cita = cita,
                Columna = columna,
                Fila = fila,
                RowSpan = rowSpan,
                Left = columna * anchoColumnaBase + 2, // Margen izquierdo
                Top = fila * altoFila, // Sin margen superior para alineación perfecta
                Width = anchoColumnaBase - 6, // Ancho menos márgenes
                Height = rowSpan * altoFila - 1 // Altura menos un pequeño margen
            });
            
            Log.Debug("Cita {CitaId} posicionada: Columna={Columna}, Fila={Fila}, RowSpan={RowSpan}, Hora={Hora}, Left={Left}, Top={Top}", 
                cita.Id, columna, fila, rowSpan, cita.HoraInicio, columna * anchoColumnaBase, fila * altoFila);
        }

        // Detectar y agrupar citas superpuestas
        DetectarCitasSuperpuestas();
        
        // Recalcular las posiciones Left, Top, Width, Height después de detectar superposiciones
        // Esto asegura que las posiciones visuales se actualicen correctamente
        // Usar las mismas constantes que se usaron al crear las citas
        const double anchoColumnaRecalc = 120.0;
        const double altoFilaRecalc = 32.0;
        
        foreach (var citaInfo in CitasSemana)
        {
            var leftRecalc = citaInfo.Columna * anchoColumnaRecalc + 2;
            var topRecalc = citaInfo.Fila * altoFilaRecalc;
            var widthRecalc = anchoColumnaRecalc - 6;
            var heightRecalc = citaInfo.RowSpan * altoFilaRecalc - 1;
            
            // Si hay citas superpuestas, ajustar ancho y posición
            if (citaInfo.TieneSuperposiciones && citaInfo.MaxSuperposicionesSimultaneas > 1)
            {
                var maxSuperposiciones = citaInfo.MaxSuperposicionesSimultaneas;
                var anchoDisponible = anchoColumnaRecalc - 6;
                var anchoPorCita = anchoDisponible / maxSuperposiciones;
                var espacioEntreCitas = 2.0;
                
                widthRecalc = anchoPorCita - espacioEntreCitas;
                leftRecalc = citaInfo.Columna * anchoColumnaRecalc + 2 + (citaInfo.IndiceEnGrupo * anchoPorCita);
                
                Log.Debug("📐 Recalculando cita {CitaId}: TieneSuperposiciones={Tiene}, Índice={Indice}, MaxSuperposiciones={Max}, Width={Width}, Left={Left}", 
                    citaInfo.Cita.Id, citaInfo.TieneSuperposiciones, citaInfo.IndiceEnGrupo, maxSuperposiciones, widthRecalc, leftRecalc);
            }
            
            // Actualizar propiedades (esto disparará PropertyChanged si cambian)
            // IMPORTANTE: Actualizar todas las propiedades para asegurar que se notifiquen los cambios
            citaInfo.Left = leftRecalc;
            citaInfo.Top = topRecalc;
            citaInfo.Width = widthRecalc;
            citaInfo.Height = heightRecalc;
        }
        
        Log.Information("✅ Posiciones recalculadas para {Count} citas después de detectar superposiciones", CitasSemana.Count);

        Log.Information("✅ Posiciones de citas calculadas: {Count} citas posicionadas en el calendario semanal (de {TotalCitas} citas totales)", 
            CitasSemana.Count, Citas.Count);
        if (CitasSemana.Count > 0)
        {
            foreach (var citaInfo in CitasSemana)
            {
                Log.Information("  📍 Cita {Id}: Col={Col}, Fila={Fila}, RowSpan={Span}, Left={Left}, Top={Top}, Width={Width}, Height={Height}, Cliente={Cliente}",
                    citaInfo.Cita.Id, citaInfo.Columna, citaInfo.Fila, citaInfo.RowSpan, 
                    citaInfo.Left, citaInfo.Top, citaInfo.Width, citaInfo.Height, citaInfo.Cita.Cliente.NombreCompleto);
            }
        }
        else if (Citas.Count > 0)
        {
            Log.Warning("⚠️ No se posicionaron citas aunque hay {Count} citas cargadas. Verificar que las citas estén en la semana actual y dentro del rango de horas (08:00-22:00)", Citas.Count);
            foreach (var cita in Citas)
            {
                Log.Debug("  📅 Cita {CitaId}: Fecha={Fecha}, Hora={Hora}", cita.Id, cita.Fecha.Date, cita.HoraInicio);
            }
        }
        else
        {
            Log.Warning("⚠️ No se posicionaron citas. Total citas en período: {Count}", Citas.Count);
        }
    }

    /// <summary>
    /// Detecta citas superpuestas (que ocupan la misma columna y se solapan en el tiempo).
    /// Agrupa las citas superpuestas y calcula sus índices para apilarlas visualmente.
    /// Usa un algoritmo de "sweep line" para calcular el número máximo de citas superpuestas
    /// en cualquier punto del tiempo, permitiendo manejar superposiciones parciales.
    /// </summary>
    private void DetectarCitasSuperpuestas()
    {
        // Primero, limpiar información previa de superposiciones
        foreach (var cita in CitasSemana)
        {
            cita.CitasSuperpuestas.Clear();
            cita.IndiceEnGrupo = 0;
        }
        
        // Agrupar citas por columna (día)
        var citasPorColumna = CitasSemana.GroupBy(c => c.Columna).ToList();
        
        foreach (var grupoColumna in citasPorColumna)
        {
            var citasEnColumna = grupoColumna.OrderBy(c => c.Fila).ThenBy(c => c.Cita.HoraInicio).ToList();
            
            if (citasEnColumna.Count <= 1) continue;
            
            // Usar un algoritmo de "sweep line" para encontrar grupos de superposición
            // y calcular el número máximo de citas superpuestas en cualquier momento
            var gruposSuperposicion = EncontrarGruposSuperposicion(citasEnColumna);
            
            // Para cada grupo de superposición, calcular índices y asignar posiciones
            foreach (var grupo in gruposSuperposicion)
            {
                if (grupo.Count <= 1) continue;
                
                // Calcular el número máximo de citas que se solapan simultáneamente
                var maxSuperposiciones = CalcularMaxSuperposicionesSimultaneas(grupo);
                
                // Ordenar por hora de inicio para asignar índices de izquierda a derecha
                // IMPORTANTE: Ordenar por hora de inicio asegura que las citas que empiezan primero
                // obtengan índices más bajos (izquierda), lo que es más intuitivo visualmente
                var citasOrdenadas = grupo.OrderBy(c => c.Fila).ThenBy(c => c.Cita.HoraInicio).ToList();
                
                Log.Debug("🔍 Procesando grupo de {Count} citas superpuestas, máximo simultáneas: {Max}", 
                    grupo.Count, maxSuperposiciones);
                foreach (var c in citasOrdenadas)
                {
                    Log.Debug("  📍 Cita {Id}: Fila={Fila}, RowSpan={Span}, Hora={Hora}", 
                        c.Cita.Id, c.Fila, c.RowSpan, c.Cita.HoraInicio);
                }
                
                // Asignar índices usando un algoritmo greedy: asignar la primera posición disponible
                AsignarIndicesGrupo(citasOrdenadas, maxSuperposiciones);
                
                // Verificar que todas las citas tienen índices asignados
                foreach (var c in citasOrdenadas)
                {
                    Log.Debug("  ✅ Cita {Id} -> Índice {Indice}", c.Cita.Id, c.IndiceEnGrupo);
                }
                
                // Asignar el grupo completo a todas las citas
                foreach (var cita in grupo)
                {
                    cita.CitasSuperpuestas = grupo;
                    cita.MaxSuperposicionesSimultaneas = maxSuperposiciones;
                }
                
                Log.Information("🔗 Grupo de superposición en columna {Columna}: {Count} citas, máximo simultáneas: {Max}", 
                    grupo[0].Columna, grupo.Count, maxSuperposiciones);
            }
        }
    }
    
    /// <summary>
    /// Encuentra grupos de citas que se solapan entre sí (transitivamente).
    /// </summary>
    private List<List<CitaSemanaInfo>> EncontrarGruposSuperposicion(List<CitaSemanaInfo> citasEnColumna)
    {
        var grupos = new List<List<CitaSemanaInfo>>();
        var procesadas = new HashSet<CitaSemanaInfo>();
        
        foreach (var cita in citasEnColumna)
        {
            if (procesadas.Contains(cita)) continue;
            
            // Encontrar todas las citas que se solapan con esta (transitivamente)
            var grupo = new List<CitaSemanaInfo>();
            var porProcesar = new Queue<CitaSemanaInfo>();
            porProcesar.Enqueue(cita);
            
            while (porProcesar.Count > 0)
            {
                var actual = porProcesar.Dequeue();
                if (procesadas.Contains(actual)) continue;
                
                procesadas.Add(actual);
                grupo.Add(actual);
                
                // Buscar todas las citas que se solapan con la actual
                foreach (var otra in citasEnColumna)
                {
                    if (procesadas.Contains(otra)) continue;
                    
                    var finActual = actual.Fila + actual.RowSpan;
                    var finOtra = otra.Fila + otra.RowSpan;
                    
                    // Se solapan si: (filaActual < finOtra) && (filaOtra < finActual)
                    if (actual.Fila < finOtra && otra.Fila < finActual)
                    {
                        porProcesar.Enqueue(otra);
                    }
                }
            }
            
            if (grupo.Count > 1)
            {
                grupos.Add(grupo);
            }
        }
        
        return grupos;
    }
    
    /// <summary>
    /// Calcula el número máximo de citas que se solapan simultáneamente en cualquier punto del tiempo.
    /// </summary>
    private int CalcularMaxSuperposicionesSimultaneas(List<CitaSemanaInfo> grupo)
    {
        if (grupo.Count <= 1) return grupo.Count;
        
        // Crear eventos de inicio y fin
        var eventos = new List<(int fila, bool esInicio, CitaSemanaInfo cita)>();
        
        foreach (var cita in grupo)
        {
            eventos.Add((cita.Fila, true, cita));
            eventos.Add((cita.Fila + cita.RowSpan, false, cita));
        }
        
        // Ordenar eventos por fila, y si hay empate, los finales antes que los iniciales
        eventos.Sort((a, b) => 
        {
            var comparacion = a.fila.CompareTo(b.fila);
            if (comparacion != 0) return comparacion;
            // Si hay empate, los finales van primero (para no contar mal en el límite)
            return a.esInicio == b.esInicio ? 0 : (a.esInicio ? 1 : -1);
        });
        
        // Sweep line: contar citas activas en cada punto
        int maxSuperposiciones = 0;
        int citasActivas = 0;
        
        foreach (var evento in eventos)
        {
            if (evento.esInicio)
            {
                citasActivas++;
                maxSuperposiciones = Math.Max(maxSuperposiciones, citasActivas);
            }
            else
            {
                citasActivas--;
            }
        }
        
        return maxSuperposiciones;
    }
    
    /// <summary>
    /// Asigna índices a las citas del grupo usando un algoritmo greedy mejorado.
    /// Cada cita obtiene el índice más bajo disponible en su rango de tiempo.
    /// Este algoritmo asegura que las citas que se solapan (incluso parcialmente) siempre tengan índices diferentes.
    /// </summary>
    private void AsignarIndicesGrupo(List<CitaSemanaInfo> citasOrdenadas, int maxIndices)
    {
        // Inicializar todos los índices a -1 (no asignado)
        foreach (var cita in citasOrdenadas)
        {
            cita.IndiceEnGrupo = -1;
        }
        
        // Para cada cita, encontrar el índice más bajo disponible
        foreach (var cita in citasOrdenadas)
        {
            var indicesOcupados = new HashSet<int>();
            var inicioCita = cita.Fila;
            var finCita = cita.Fila + cita.RowSpan;
            
            // Buscar todas las citas que se solapan con esta y ver qué índices están ocupados
            foreach (var otra in citasOrdenadas)
            {
                if (otra == cita || otra.IndiceEnGrupo < 0) continue;
                
                var inicioOtra = otra.Fila;
                var finOtra = otra.Fila + otra.RowSpan;
                
                // Si se solapan (incluso parcialmente): inicioCita < finOtra && inicioOtra < finCita
                if (inicioCita < finOtra && inicioOtra < finCita)
                {
                    indicesOcupados.Add(otra.IndiceEnGrupo);
                }
            }
            
            // Asignar el primer índice disponible (0, 1, 2, ...)
            for (int i = 0; i < maxIndices; i++)
            {
                if (!indicesOcupados.Contains(i))
                {
                    cita.IndiceEnGrupo = i;
                    Log.Debug("📌 Cita {CitaId} asignada al índice {Indice} (inicio={Inicio}, fin={Fin})", 
                        cita.Cita.Id, i, inicioCita, finCita);
                    break;
                }
            }
            
            // Si no se pudo asignar un índice (no debería pasar), asignar el último disponible
            if (cita.IndiceEnGrupo < 0)
            {
                cita.IndiceEnGrupo = maxIndices - 1;
                Log.Warning("⚠️ No se pudo asignar índice a cita {CitaId}, usando {Indice}", 
                    cita.Cita.Id, cita.IndiceEnGrupo);
            }
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
            // AsNoTracking para obtener datos frescos después de ediciones
            var lista = await _db.Clientes
                .AsNoTracking()
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
    private async Task NuevaCita()
    {
        // Recargar lista de clientes para obtener datos frescos (ej. menores actualizados)
        await CargarClientes();
        
        LimpiarFormulario();
        FechaCita = FechaSeleccionada ?? DateTimeOffset.Now.Date;
        EsEdicion = false;
        TituloFormulario = "✨ Nueva Cita";
        MostrarFormulario = true;
    }

    /// <summary>
    /// Crea una nueva cita desde el calendario semanal, precargando fecha, hora y duración.
    /// </summary>
    /// <param name="fecha">Fecha de la cita.</param>
    /// <param name="horaInicio">Hora de inicio (ya alineada a slots de 30 min).</param>
    /// <param name="duracionMinutos">Duración en minutos (múltiplo de 30).</param>
    public async Task CrearCitaDesdeCalendario(DateTime fecha, TimeSpan horaInicio, int duracionMinutos)
    {
        var fechaHora = fecha.Date + horaInicio;
        if (fechaHora < DateTime.Now)
        {
            MensajeError = "No se pueden crear citas en el pasado";
            return;
        }

        // Recargar lista de clientes para obtener datos frescos (ej. menores actualizados)
        await CargarClientes();
        
        LimpiarFormulario();

        // Desactivar ajuste automático de duración (viene del drag & drop)
        _ajustarDuracionPorTipo = false;

        // Fecha y hora
        FechaCita = new DateTimeOffset(fecha);
        HoraInicioString = $"{horaInicio.Hours:D2}:{horaInicio.Minutes:D2}";

        // Duración (asegurar múltiplos de 30 minutos, mínimo 15 para piercings)
        if (duracionMinutos < 15) duracionMinutos = 15;
        DuracionMinutos = duracionMinutos;

        // Valores por defecto razonables
        EstadoCita = EstadoCita.Pendiente;
        if (TipoCita == 0)
        {
            TipoCita = TipoCita.Tatuaje;
        }

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

            // Parsear hora de inicio desde string (formato HH:mm en 24 horas)
            Log.Information("🔍 Guardando cita - HoraInicioString recibida: '{HoraInicioString}'", HoraInicioString);
            
            TimeSpan horaInicioParsed;
            var horaInicioStringTrimmed = HoraInicioString?.Trim() ?? string.Empty;
            var parts = horaInicioStringTrimmed.Split(':');
            if (parts.Length == 2 && 
                int.TryParse(parts[0], out var hours) && 
                int.TryParse(parts[1], out var minutes))
            {
                // Validar rango de horas (0-23) y minutos (0-59)
                if (hours < 0 || hours > 23)
                {
                    MensajeError = "La hora debe estar entre 00 y 23";
                    return;
                }
                if (minutes < 0 || minutes > 59)
                {
                    MensajeError = "Los minutos deben estar entre 00 y 59";
                    return;
                }
                horaInicioParsed = new TimeSpan(hours, minutes, 0);
                Log.Information("🔍 Hora parseada correctamente: {Hora} (Hours={Hours}, Minutes={Minutes})", 
                    horaInicioParsed, hours, minutes);
            }
            else
            {
                Log.Warning("⚠️ Formato de hora inválido: '{HoraInicioString}' (parts.Length={Length})", 
                    HoraInicioString, parts.Length);
                MensajeError = "Formato de hora de inicio inválido. Usa HH:mm en formato 24 horas (ej: 11:00)";
                return;
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

            if (EsFechaHoraEnPasado(fechaCitaDateTime, horaParsed))
            {
                MensajeError = "No se pueden programar citas en el pasado";
                return;
            }

            var citaIdExcluir = EsEdicion && CitaSeleccionada != null ? CitaSeleccionada.Id : (int?)null;
            var citasSolapadas = await ObtenerCitasSolapadasAsync(
                fechaCitaDateTime, horaParsed, DuracionMinutos, citaIdExcluir);

            if (citasSolapadas.Count > 0)
            {
                var detalle = FormatearDetalleCitasSolapadas(citasSolapadas);
                var accion = EsEdicion ? "guardarla" : "crearla";
                var confirmado = await DialogService.ConfirmarAccionAsync(
                    titulo: "Conflicto de horario",
                    mensaje: $"Esta cita se solapa con otra(s) ya existente(s):\n\n{detalle}\n\n¿Estás seguro de que quieres {accion} de todos modos?",
                    botonConfirmar: "Sí, continuar",
                    esPeligroso: true);

                if (!confirmado)
                    return;
            }

            Cargando = true;
            MensajeError = string.Empty;

            if (EsEdicion && CitaSeleccionada != null)
            {
                // Actualizar cita existente
                Log.Information("🔍 Actualizando cita {CitaId} - HoraInicioString: '{HoraInicioString}', horaParsed: {HoraParsed}", 
                    CitaSeleccionada.Id, HoraInicioString, horaParsed);
                
                CitaSeleccionada.ClienteId = ClienteSeleccionado.Id;
                CitaSeleccionada.Fecha = fechaCitaDateTime;
                CitaSeleccionada.HoraInicio = horaParsed;
                CitaSeleccionada.DuracionMinutos = DuracionMinutos;
                CitaSeleccionada.TipoCita = TipoCita;
                CitaSeleccionada.Descripcion = string.IsNullOrWhiteSpace(Descripcion) ? null : Descripcion.Trim();
                CitaSeleccionada.Estado = EstadoCita;
                CitaSeleccionada.Notas = string.IsNullOrWhiteSpace(Notas) ? null : Notas.Trim();
                
                // Actualizar vinculación del trabajo:
                // una cita pertenece (opcionalmente) a un trabajo,
                // y un trabajo puede tener varias citas.
                if (TrabajoSeleccionado != null)
                {
                    CitaSeleccionada.TrabajoId = TrabajoSeleccionado.Id;
                }

                await _db.SaveChangesAsync();
                Log.Information("✅ Cita {CitaId} actualizada - HoraInicio guardada: {HoraInicio}", 
                    CitaSeleccionada.Id, CitaSeleccionada.HoraInicio);

                // Si la cita está vinculada a un trabajo, recalcular su duración real
                if (CitaSeleccionada.TrabajoId.HasValue)
                {
                    await RecalcularDuracionRealTrabajoAsync(CitaSeleccionada.TrabajoId.Value);
                }
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
                    nuevaCita.TrabajoId = TrabajoSeleccionado.Id;
                    await _db.SaveChangesAsync();

                    // Al vincular una nueva cita a un trabajo, recalcular su duración real
                    await RecalcularDuracionRealTrabajoAsync(TrabajoSeleccionado.Id);
                }
                
                Log.Information("Nueva cita creada para cliente {ClienteId} vinculada a trabajo {TrabajoId}", 
                    ClienteSeleccionado.Id, TrabajoSeleccionado?.Id);
            }
            
            // Verificar consentimiento del trabajo si está asociado
            if (TrabajoSeleccionado != null)
            {
                await _db.Entry(TrabajoSeleccionado).Reference(t => t.Consentimiento).LoadAsync();
                await _db.Entry(TrabajoSeleccionado).Reference(t => t.Cliente).LoadAsync();
                await _db.Entry(TrabajoSeleccionado.Cliente).Collection(c => c.Consentimientos).LoadAsync();
                var tieneConsentimiento = TrabajoSeleccionado.Consentimiento != null && TrabajoSeleccionado.Consentimiento.Firmado;
                
                if (!tieneConsentimiento)
                {
                    // Mostrar aviso no bloqueante
                    TrabajoPendienteConsentimiento = TrabajoSeleccionado;
                    MensajeAvisoConsentimiento = "⚠️ El trabajo asociado no tiene consentimiento firmado. Recuerda solicitarlo antes de la cita.";
                    MostrarAvisoConsentimiento = true;
                }
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

        // Preparar descripción para el diálogo
        var descripcionCita = $"{CitaSeleccionada.Fecha:dd/MM/yyyy} a las {CitaSeleccionada.HoraInicio:hh\\:mm}";
        var clienteNombre = CitaSeleccionada.Cliente?.NombreCompleto ?? "Cliente desconocido";
        var trabajoDesc = CitaSeleccionada.Trabajo?.Descripcion ?? "Sin trabajo asociado";

        // Mostrar diálogo de confirmación
        var confirmado = await DialogService.ConfirmarEliminarAsync(
            tipoElemento: "la cita",
            nombreElemento: $"{descripcionCita}\nCliente: {clienteNombre}\nTrabajo: {trabajoDesc}",
            advertenciaAdicional: "La cita se eliminará permanentemente."
        );

        if (!confirmado)
        {
            Log.Debug("Eliminación de cita cancelada por el usuario: {CitaId}", CitaSeleccionada.Id);
            return;
        }

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
    /// Mueve una cita a una nueva fecha y hora.
    /// </summary>
    /// <param name="citaId">ID de la cita a mover.</param>
    /// <param name="nuevaFecha">Nueva fecha para la cita.</param>
    /// <param name="nuevaHora">Nueva hora de inicio para la cita.</param>
    public async Task MoverCitaAsync(int citaId, DateTime nuevaFecha, TimeSpan nuevaHora)
    {
        try
        {
            Cargando = true;
            
            // Buscar la cita en la base de datos
            var cita = await _db.Citas.FirstOrDefaultAsync(c => c.Id == citaId);
            if (cita == null)
            {
                Log.Warning("No se encontró la cita {CitaId} para mover", citaId);
                return;
            }

            // Validar que la nueva fecha/hora sea válida
            var nuevaFechaHora = nuevaFecha.Date + nuevaHora;
            if (nuevaFechaHora < DateTime.Now)
            {
                Log.Warning("No se puede mover la cita {CitaId} a una fecha/hora pasada: {FechaHora}", 
                    citaId, nuevaFechaHora);
                MensajeError = "No se puede mover una cita a una fecha/hora pasada";
                await CargarCitas();
                return;
            }

            var citasSolapadas = await ObtenerCitasSolapadasAsync(
                nuevaFecha.Date, nuevaHora, cita.DuracionMinutos, citaId);

            if (citasSolapadas.Count > 0)
            {
                var detalle = FormatearDetalleCitasSolapadas(citasSolapadas);
                var confirmado = await DialogService.ConfirmarAccionAsync(
                    titulo: "Conflicto de horario",
                    mensaje: $"Al mover la cita se solapa con:\n\n{detalle}\n\n¿Estás seguro de que quieres moverla?",
                    botonConfirmar: "Sí, mover",
                    esPeligroso: true);

                if (!confirmado)
                {
                    await CargarCitas();
                    return;
                }
            }

            // Actualizar la cita
            var fechaAnterior = cita.Fecha;
            var horaAnterior = cita.HoraInicio;
            
            cita.Fecha = nuevaFecha.Date;
            cita.HoraInicio = nuevaHora;
            
            await _db.SaveChangesAsync();
            
            Log.Information("✅ Cita {CitaId} movida: {FechaAnterior} {HoraAnterior} -> {NuevaFecha} {NuevaHora}", 
                citaId, fechaAnterior, horaAnterior, nuevaFecha, nuevaHora);
            
            // Recargar las citas para actualizar la vista
            await CargarCitas();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al mover cita {CitaId}", citaId);
            MensajeError = $"Error al mover la cita: {ex.Message}";
        }
        finally
        {
            Cargando = false;
        }
    }

    /// <summary>
    /// Cambia la duración de una cita.
    /// </summary>
    /// <param name="citaId">ID de la cita a redimensionar.</param>
    /// <param name="nuevaDuracionMinutos">Nueva duración en minutos.</param>
    public async Task CambiarDuracionCitaAsync(int citaId, int nuevaDuracionMinutos)
    {
        try
        {
            Cargando = true;
            
            // Buscar la cita en la base de datos
            var cita = await _db.Citas
                .Include(c => c.Trabajo)
                .FirstOrDefaultAsync(c => c.Id == citaId);
            if (cita == null)
            {
                Log.Warning("No se encontró la cita {CitaId} para cambiar duración", citaId);
                return;
            }

            // Validar duración mínima y máxima
            nuevaDuracionMinutos = Math.Max(30, nuevaDuracionMinutos); // Mínimo 30 minutos
            nuevaDuracionMinutos = Math.Min(480, nuevaDuracionMinutos); // Máximo 8 horas

            // Actualizar la duración
            var duracionAnterior = cita.DuracionMinutos;
            cita.DuracionMinutos = nuevaDuracionMinutos;
            
            await _db.SaveChangesAsync();
            
            Log.Information("✅ Duración de cita {CitaId} cambiada: {DuracionAnterior}min -> {NuevaDuracion}min", 
                citaId, duracionAnterior, nuevaDuracionMinutos);

            // Recalcular duración real del trabajo asociado (si lo hay)
            if (cita.TrabajoId.HasValue)
            {
                await RecalcularDuracionRealTrabajoAsync(cita.TrabajoId.Value);
            }
            
            // Recargar las citas para actualizar la vista
            await CargarCitas();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al cambiar duración de cita {CitaId}", citaId);
            MensajeError = $"Error al cambiar la duración: {ex.Message}";
        }
        finally
        {
            Cargando = false;
        }
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

            // Si la cita pertenece a un trabajo, recalcular la duración real del trabajo
            if (CitaSeleccionada.TrabajoId.HasValue)
            {
                await RecalcularDuracionRealTrabajoAsync(CitaSeleccionada.TrabajoId.Value);
            }
            
            await CargarCitas();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al cambiar estado de cita {CitaId}", CitaSeleccionada.Id);
            MensajeError = $"Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Recalcula la duración real total de un trabajo a partir de las citas asociadas.
    /// Solo suma las citas marcadas como Completadas o EnProceso (trabajo efectivo).
    /// </summary>
    /// <param name="trabajoId">ID del trabajo.</param>
    private async Task RecalcularDuracionRealTrabajoAsync(int trabajoId)
    {
        try
        {
            var trabajo = await _db.Trabajos
                .Include(t => t.Citas)
                .FirstOrDefaultAsync(t => t.Id == trabajoId);

            if (trabajo == null)
            {
                Log.Warning("No se encontró el trabajo {TrabajoId} para recalcular duración real", trabajoId);
                return;
            }

            // Sumar solo citas efectivas (en proceso o completadas)
            var minutosReales = trabajo.Citas
                .Where(c => c.Estado == EstadoCita.EnProceso || c.Estado == EstadoCita.Completada)
                .Sum(c => c.DuracionMinutos);

            trabajo.DuracionRealMinutos = minutosReales > 0 ? minutosReales : null;

            await _db.SaveChangesAsync();

            Log.Information("⏱ Duración real de trabajo {TrabajoId} actualizada a {Minutos} minutos", 
                trabajoId, trabajo.DuracionRealMinutos ?? 0);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al recalcular la duración real del trabajo {TrabajoId}", trabajoId);
        }
    }

    /// <summary>
    /// Comando para crear un nuevo trabajo.
    /// </summary>
    [RelayCommand]
    private async Task CrearTrabajoNuevo()
    {
        if (ClienteSeleccionado == null)
        {
            MensajeError = "Primero debes seleccionar un cliente";
            return;
        }
        
        if (_trabajosVM != null && _mainWindowVM != null)
        {
            MostrarFormulario = false;
            _mainWindowVM.IrATrabajosCommand.Execute(null);
            await _trabajosVM.NuevoTrabajoDesdeAgenda(this, ClienteSeleccionado);
            
            Log.Information("Navegando a Trabajos para crear trabajo para cliente {ClienteId} (retorno a cita)", ClienteSeleccionado.Id);
        }
        else
        {
            MensajeError = "No se puede crear trabajo desde aquí. Por favor, ve a la sección de Trabajos.";
            Log.Warning("Intento de crear trabajo sin referencias a TrabajosVM o MainWindowVM");
        }
    }

    /// <summary>
    /// Guarda el estado del formulario de cita antes de ir a crear un trabajo.
    /// </summary>
    public void CapturarEstadoFormularioParaRetorno()
    {
        _citaFormularioRetorno = new CitaFormularioSnapshot
        {
            EsEdicion = EsEdicion,
            CitaId = CitaSeleccionada?.Id,
            ClienteId = ClienteSeleccionado?.Id,
            TrabajoId = TrabajoSeleccionado?.Id,
            FechaCita = FechaCita,
            HoraInicioString = HoraInicioString,
            DuracionMinutos = DuracionMinutos,
            TipoCita = TipoCita,
            Descripcion = Descripcion,
            EstadoCita = EstadoCita,
            Notas = Notas,
            TituloFormulario = TituloFormulario,
            AjustarDuracionPorTipo = _ajustarDuracionPorTipo
        };
    }

    /// <summary>
    /// Vuelve al modal de cita tras crear un trabajo, vinculando el trabajo nuevo.
    /// </summary>
    public async Task RestaurarFormularioCitaTrasCrearTrabajoAsync(Trabajo trabajoNuevo)
    {
        await RestaurarFormularioCitaInternoAsync(trabajoNuevo);
    }

    /// <summary>
    /// Vuelve al modal de cita si se canceló la creación del trabajo.
    /// </summary>
    public void RestaurarFormularioCitaTrasCancelarTrabajo()
    {
        _ = RestaurarFormularioCitaInternoAsync(null);
    }

    private async Task RestaurarFormularioCitaInternoAsync(Trabajo? trabajoNuevo)
    {
        var snapshot = _citaFormularioRetorno;
        _citaFormularioRetorno = null;
        if (snapshot == null)
            return;

        await CargarClientes();

        EsEdicion = snapshot.EsEdicion;
        TituloFormulario = snapshot.TituloFormulario;
        _ajustarDuracionPorTipo = snapshot.AjustarDuracionPorTipo;

        if (snapshot.EsEdicion && snapshot.CitaId.HasValue)
            CitaSeleccionada = await _db.Citas
                .Include(c => c.Cliente)
                .Include(c => c.Trabajo)
                .FirstOrDefaultAsync(c => c.Id == snapshot.CitaId.Value);

        if (snapshot.ClienteId.HasValue)
            ClienteSeleccionado = Clientes.FirstOrDefault(c => c.Id == snapshot.ClienteId.Value);

        if (ClienteSeleccionado != null)
            await CargarTrabajosDelCliente(ClienteSeleccionado.Id);

        if (trabajoNuevo != null)
        {
            var trabajoEnLista = TrabajosDelCliente.FirstOrDefault(t => t.Id == trabajoNuevo.Id);
            TrabajoSeleccionado = trabajoEnLista ?? trabajoNuevo;
        }
        else if (snapshot.TrabajoId.HasValue)
            TrabajoSeleccionado = TrabajosDelCliente.FirstOrDefault(t => t.Id == snapshot.TrabajoId.Value);

        FechaCita = snapshot.FechaCita;
        HoraInicioString = snapshot.HoraInicioString;
        DuracionMinutos = snapshot.DuracionMinutos;
        CalcularHoraFin();
        TipoCita = snapshot.TipoCita;
        Descripcion = snapshot.Descripcion;
        EstadoCita = snapshot.EstadoCita;
        Notas = snapshot.Notas;
        MensajeError = string.Empty;
        MostrarFormulario = true;
    }

    /// <summary>
    /// Selecciona un día en la vista mensual y cambia a vista de día.
    /// </summary>
    [RelayCommand]
    private async Task SeleccionarDiaMes(DateTime fecha)
    {
        FechaSeleccionada = fecha.Date;
        VistaActual = VistaAgenda.Dia;
        await CargarCitas();
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

    private static bool EsFechaHoraEnPasado(DateTime fecha, TimeSpan horaInicio)
        => fecha.Date + horaInicio < DateTime.Now;

    private async Task<List<Cita>> ObtenerCitasSolapadasAsync(
        DateTime fecha,
        TimeSpan horaInicio,
        int duracionMinutos,
        int? excluirCitaId = null)
    {
        var finNueva = horaInicio.Add(TimeSpan.FromMinutes(duracionMinutos));

        var citasDelDia = await _db.Citas
            .Include(c => c.Cliente)
            .Where(c => c.Fecha.Date == fecha.Date && (excluirCitaId == null || c.Id != excluirCitaId))
            .ToListAsync();

        return citasDelDia
            .Where(c =>
            {
                var finCita = c.HoraInicio.Add(TimeSpan.FromMinutes(c.DuracionMinutos));
                return c.HoraInicio < finNueva && horaInicio < finCita;
            })
            .OrderBy(c => c.HoraInicio)
            .ToList();
    }

    private static string FormatearDetalleCitasSolapadas(IEnumerable<Cita> citas)
    {
        var lineas = citas.Select(c =>
        {
            var cliente = c.Cliente?.NombreCompleto ?? "Sin cliente";
            var fin = c.HoraInicio.Add(TimeSpan.FromMinutes(c.DuracionMinutos));
            return $"• {c.Fecha:dd/MM/yyyy} {c.HoraInicio:hh\\:mm}–{fin:hh\\:mm} — {c.TipoCita} — {cliente}";
        });
        return string.Join("\n", lineas);
    }

    /// <summary>
    /// Limpia todos los campos del formulario.
    /// </summary>
    private void LimpiarFormulario()
    {
        // Reactivar ajuste automático de duración (para el botón "Nueva Cita")
        _ajustarDuracionPorTipo = true;
        
        ClienteSeleccionado = null;
        TrabajoSeleccionado = null;
        TrabajosDelCliente.Clear();
        FechaCita = DateTimeOffset.Now.Date;
        HoraInicio = new TimeSpan(10, 0, 0);
        HoraInicioString = "10:00"; // Formato 24 horas
        TipoCita = TipoCita.Tatuaje;
        DuracionMinutos = 30; // Duración por defecto para tatuaje
        CalcularHoraFin(); // Calcular hora fin inicial
        Descripcion = string.Empty;
        EstadoCita = EstadoCita.Pendiente;
        Notas = string.Empty;
        MensajeError = string.Empty;
        TieneConsentimientoTrabajo = false;
        MensajeConsentimiento = string.Empty;
        MostrarCalendarioCitaExpandido = false;
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
        HoraInicioString = $"{cita.HoraInicio.Hours:D2}:{cita.HoraInicio.Minutes:D2}"; // Formato 24 horas
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
        // Parsear formato HH:mm manualmente (24 horas)
        var parts = HoraInicioString?.Trim().Split(':');
        if (parts != null && parts.Length == 2 &&
            int.TryParse(parts[0], out var hours) &&
            int.TryParse(parts[1], out var minutes))
        {
            var inicio = new TimeSpan(hours, minutes, 0);
            var fin = inicio.Add(TimeSpan.FromMinutes(DuracionMinutos));
            // Formatear manualmente en formato 24 horas (HH:mm)
            // TimeSpan no soporta HH, así que lo hacemos manualmente
            HoraFinString = $"{fin.Hours:D2}:{fin.Minutes:D2}";
        }
        else
        {
            // Si no se puede parsear, dejar vacío o usar un valor por defecto
            HoraFinString = string.Empty;
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

    /// <summary>
    /// Abre el modal para firmar el consentimiento de trabajo.
    /// </summary>
    [RelayCommand]
    private async Task FirmarConsentimientoTrabajoDesdeCita(Trabajo? trabajo = null)
    {
        var trabajoAFirmar = trabajo ?? TrabajoPendienteConsentimiento ?? TrabajoSeleccionado;
        if (trabajoAFirmar == null || ClienteSeleccionado == null) return;

        try
        {
            // Recargar trabajo con relaciones
            await _db.Entry(trabajoAFirmar).ReloadAsync();
            await _db.Entry(trabajoAFirmar).Reference(t => t.Cliente).LoadAsync();
            await _db.Entry(trabajoAFirmar).Reference(t => t.Consentimiento).LoadAsync();
            
            // Verificar si ya tiene consentimiento firmado
            if (trabajoAFirmar.Consentimiento != null && trabajoAFirmar.Consentimiento.Firmado)
            {
                MensajeError = "Este trabajo ya tiene el consentimiento firmado";
                await CargarCitas();
                return;
            }

            if (!await ConsentimientoService.ClienteTieneRgpdVigenteAsync(trabajoAFirmar.ClienteId))
            {
                MensajeError = "El cliente debe tener RGPD firmado antes de firmar el consentimiento de trabajo";
                return;
            }

            // Cerrar formulario si está abierto
            MostrarFormulario = false;
            MostrarAvisoConsentimiento = false;
            
            // Abrir modal de firma de trabajo
            if (ConsentimientoFirmaVM == null)
            {
                ConsentimientoFirmaVM = new ConsentimientoFirmaViewModel();
                ConsentimientoFirmaVM.FirmaCompletada += async (s, cliente) => 
                {
                    await CargarCitas();
                    // Recargar trabajos del cliente para actualizar el estado
                    if (ClienteSeleccionado != null)
                    {
                        await CargarTrabajosDelCliente(ClienteSeleccionado.Id);
                    }
                };
            }
            await ConsentimientoFirmaVM.AbrirModal(trabajoAFirmar.Cliente, TipoConsentimiento.Trabajo, trabajoAFirmar);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al abrir modal de firma de consentimiento de trabajo");
            MensajeError = $"Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Cierra el aviso de consentimiento.
    /// </summary>
    [RelayCommand]
    private void CerrarAvisoConsentimiento()
    {
        MostrarAvisoConsentimiento = false;
        TrabajoPendienteConsentimiento = null;
        MensajeAvisoConsentimiento = string.Empty;
    }

    #endregion

    #region Email Recordatorio

    [ObservableProperty]
    private string _mensajeEmailEnviado = string.Empty;

    /// <summary>
    /// Envía un email de recordatorio al cliente de la cita.
    /// </summary>
    [RelayCommand]
    private async Task EnviarRecordatorioAsync(Cita cita)
    {
        if (cita == null) return;

        try
        {
            MensajeError = string.Empty;
            MensajeEmailEnviado = string.Empty;

            using var dbEmail = new AtaenaDbContext();
            var emailService = new EmailService(dbEmail);

            // Verificar configuración SMTP
            if (!await emailService.EstaConfiguradoAsync())
            {
                MensajeError = "⚠️ El SMTP no está configurado. Ve a Configuración para configurarlo.";
                return;
            }

            // Verificar que la cita tenga cliente con email
            if (cita.Cliente == null)
            {
                MensajeError = "La cita no tiene cliente asociado.";
                return;
            }

            if (string.IsNullOrWhiteSpace(cita.Cliente.Email))
            {
                MensajeError = $"El cliente {cita.Cliente.NombreCompleto} no tiene email registrado.";
                return;
            }

            // Enviar recordatorio
            var (exito, mensaje) = await emailService.EnviarRecordatorioCitaAsync(cita);

            if (exito)
            {
                // Actualizar estado de la cita
                var citaDb = await _db.Citas.FirstOrDefaultAsync(c => c.Id == cita.Id);
                if (citaDb != null)
                {
                    citaDb.EmailEnviado = true;
                    citaDb.FechaEmailEnviado = DateTime.Now;
                    await _db.SaveChangesAsync();
                    
                    // Actualizar cita local
                    cita.EmailEnviado = true;
                    cita.FechaEmailEnviado = DateTime.Now;
                }

                MensajeEmailEnviado = $"✅ {mensaje}";
                Log.Information("Recordatorio enviado para cita {CitaId}", cita.Id);

                // Limpiar mensaje después de unos segundos
                _ = Task.Run(async () =>
                {
                    await Task.Delay(5000);
                    MensajeEmailEnviado = string.Empty;
                });
            }
            else
            {
                MensajeError = $"❌ {mensaje}";
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al enviar recordatorio de cita");
            MensajeError = $"Error al enviar recordatorio: {ex.Message}";
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
