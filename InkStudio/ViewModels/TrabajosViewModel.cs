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
/// ViewModel para la gestión de trabajos (tatuajes y piercings).
/// Implementa operaciones CRUD y búsqueda.
/// </summary>
public partial class TrabajosViewModel : ViewModelBase
{
    private readonly InkStudioDbContext _db = new();

    #region Propiedades - Lista y Selección

    /// <summary>
    /// Colección observable de trabajos mostrados en la lista.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<Trabajo> _trabajos = new();

    /// <summary>
    /// Trabajo actualmente seleccionado en la lista.
    /// </summary>
    [ObservableProperty]
    private Trabajo? _trabajoSeleccionado;

    /// <summary>
    /// Texto de búsqueda para filtrar trabajos.
    /// </summary>
    [ObservableProperty]
    private string _textoBusqueda = string.Empty;

    /// <summary>
    /// Cliente seleccionado para filtrar trabajos (opcional).
    /// </summary>
    [ObservableProperty]
    private Cliente? _clienteFiltro;

    /// <summary>
    /// Se ejecuta cuando cambia ClienteFiltro.
    /// </summary>
    partial void OnClienteFiltroChanged(Cliente? value)
    {
        _ = CargarTrabajos();
    }

    /// <summary>
    /// Lista de clientes para el filtro.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<Cliente> _clientes = new();

    #endregion

    #region Propiedades - Formulario de Edición

    /// <summary>
    /// Indica si el panel de edición/creación está visible.
    /// </summary>
    [ObservableProperty]
    private bool _mostrarFormulario = false;

    /// <summary>
    /// Indica si estamos editando (true) o creando (false) un trabajo.
    /// </summary>
    [ObservableProperty]
    private bool _esEdicion = false;

    /// <summary>
    /// Título del formulario (cambia según modo edición/creación).
    /// </summary>
    [ObservableProperty]
    private string _tituloFormulario = "Nuevo Trabajo";

    /// <summary>
    /// Cliente pre-seleccionado cuando se crea desde el modal de cita.
    /// </summary>
    [ObservableProperty]
    private Cliente? _clientePreseleccionado;

    // Campos del formulario
    [ObservableProperty]
    private Cliente? _clienteSeleccionado;

    /// <summary>
    /// Se ejecuta cuando cambia ClienteSeleccionado.
    /// </summary>
    partial void OnClienteSeleccionadoChanged(Cliente? value)
    {
        OnPropertyChanged(nameof(TieneClienteSeleccionado));
    }

    /// <summary>
    /// Indica si hay un cliente seleccionado.
    /// </summary>
    public bool TieneClienteSeleccionado => ClienteSeleccionado != null;

    [ObservableProperty]
    private TipoTrabajo _tipoTrabajo = TipoTrabajo.Tatuaje;

    [ObservableProperty]
    private string _descripcion = string.Empty;

    [ObservableProperty]
    private string _zonaCuerpo = string.Empty;

    [ObservableProperty]
    private string? _estilo;

    [ObservableProperty]
    private string? _tamano;

    [ObservableProperty]
    private bool _colores = false;

    [ObservableProperty]
    private decimal _precio = 0;

    [ObservableProperty]
    private DateTimeOffset? _fecha = DateTimeOffset.Now.Date;

    [ObservableProperty]
    private int _duracionMinutos = 60;

    [ObservableProperty]
    private string? _notas;

    #endregion

    #region Propiedades - Estado

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
    /// Total de trabajos (para mostrar en UI).
    /// </summary>
    [ObservableProperty]
    private int _totalTrabajos;

    #endregion

    #region Constructor

    /// <summary>
    /// Inicializa el ViewModel y carga los datos iniciales.
    /// </summary>
    public TrabajosViewModel()
    {
        _ = CargarTrabajos();
        _ = CargarClientes();
    }

    #endregion

    #region Comandos - Carga de Datos

    /// <summary>
    /// Carga todos los trabajos desde la base de datos.
    /// </summary>
    [RelayCommand]
    private async Task CargarTrabajos()
    {
        try
        {
            Log.Debug("Cargando lista de trabajos");
            Cargando = true;
            MensajeError = string.Empty;

            var query = _db.Trabajos
                .Include(t => t.Cliente)
                .Include(t => t.Cita)
                .AsQueryable();

            // Aplicar filtro de cliente si existe
            if (ClienteFiltro != null)
            {
                query = query.Where(t => t.ClienteId == ClienteFiltro.Id);
            }

            // Aplicar búsqueda si existe
            if (!string.IsNullOrWhiteSpace(TextoBusqueda))
            {
                var busqueda = TextoBusqueda.ToLower();
                query = query.Where(t =>
                    (t.Descripcion != null && t.Descripcion.ToLower().Contains(busqueda)) ||
                    (t.ZonaCuerpo != null && t.ZonaCuerpo.ToLower().Contains(busqueda)) ||
                    (t.Estilo != null && t.Estilo.ToLower().Contains(busqueda)) ||
                    (t.Cliente.Nombre.ToLower().Contains(busqueda) ||
                     t.Cliente.Apellidos.ToLower().Contains(busqueda))
                );
            }

            var lista = await query
                .OrderByDescending(t => t.Fecha)
                .ThenByDescending(t => t.Id)
                .ToListAsync();

            Trabajos = new ObservableCollection<Trabajo>(lista);
            TotalTrabajos = lista.Count;
            Log.Information("Trabajos cargados: {Count} trabajos", lista.Count);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al cargar trabajos desde la base de datos");
            MensajeError = $"Error al cargar trabajos: {ex.Message}";
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

    #region Comandos - CRUD

    /// <summary>
    /// Abre el formulario para crear un nuevo trabajo.
    /// </summary>
    [RelayCommand]
    private void NuevoTrabajo()
    {
        LimpiarFormulario();
        
        // Si hay un cliente pre-seleccionado (desde modal de cita), usarlo
        if (ClientePreseleccionado != null)
        {
            ClienteSeleccionado = ClientePreseleccionado;
            ClientePreseleccionado = null; // Limpiar después de usar
        }
        
        EsEdicion = false;
        TituloFormulario = "✨ Nuevo Trabajo";
        MostrarFormulario = true;
    }

    /// <summary>
    /// Abre el formulario para crear un nuevo trabajo para un cliente específico.
    /// </summary>
    public void NuevoTrabajoParaCliente(Cliente cliente)
    {
        LimpiarFormulario();
        ClienteSeleccionado = cliente;
        EsEdicion = false;
        TituloFormulario = "✨ Nuevo Trabajo";
        MostrarFormulario = true;
    }

    /// <summary>
    /// Abre el formulario para editar el trabajo seleccionado.
    /// </summary>
    [RelayCommand]
    private void EditarTrabajo()
    {
        if (TrabajoSeleccionado == null) return;

        CargarTrabajoEnFormulario(TrabajoSeleccionado);
        EsEdicion = true;
        TituloFormulario = "✏️ Editar Trabajo";
        MostrarFormulario = true;
    }

    /// <summary>
    /// Guarda el trabajo (crea nuevo o actualiza existente).
    /// </summary>
    [RelayCommand]
    private async Task GuardarTrabajo()
    {
        try
        {
            // Validación básica
            if (ClienteSeleccionado == null)
            {
                MensajeError = "Debes seleccionar un cliente";
                return;
            }

            if (string.IsNullOrWhiteSpace(Descripcion))
            {
                MensajeError = "La descripción es obligatoria";
                return;
            }

            if (!Fecha.HasValue)
            {
                MensajeError = "Debes seleccionar una fecha";
                return;
            }

            if (Precio < 0)
            {
                MensajeError = "El precio no puede ser negativo";
                return;
            }

            if (DuracionMinutos < 1)
            {
                MensajeError = "La duración debe ser al menos 1 minuto";
                return;
            }

            Cargando = true;
            MensajeError = string.Empty;

            if (EsEdicion && TrabajoSeleccionado != null)
            {
                // Actualizar trabajo existente
                Log.Information("Actualizando trabajo ID: {TrabajoId}, Cliente: {ClienteId}", 
                    TrabajoSeleccionado.Id, ClienteSeleccionado.Id);
                
                TrabajoSeleccionado.ClienteId = ClienteSeleccionado.Id;
                TrabajoSeleccionado.Tipo = TipoTrabajo;
                TrabajoSeleccionado.Descripcion = Descripcion.Trim();
                TrabajoSeleccionado.ZonaCuerpo = ZonaCuerpo.Trim();
                TrabajoSeleccionado.Estilo = string.IsNullOrWhiteSpace(Estilo) ? null : Estilo.Trim();
                TrabajoSeleccionado.Tamano = string.IsNullOrWhiteSpace(Tamano) ? null : Tamano.Trim();
                TrabajoSeleccionado.Colores = Colores;
                TrabajoSeleccionado.Precio = Precio;
                TrabajoSeleccionado.Fecha = Fecha.Value.DateTime;
                TrabajoSeleccionado.DuracionMinutos = DuracionMinutos;
                TrabajoSeleccionado.Notas = string.IsNullOrWhiteSpace(Notas) ? null : Notas.Trim();
            }
            else
            {
                // Crear nuevo trabajo
                Log.Information("Creando nuevo trabajo para cliente {ClienteId}: {Descripcion}", 
                    ClienteSeleccionado.Id, Descripcion);
                
                var nuevoTrabajo = new Trabajo
                {
                    ClienteId = ClienteSeleccionado.Id,
                    Tipo = TipoTrabajo,
                    Descripcion = Descripcion.Trim(),
                    ZonaCuerpo = ZonaCuerpo.Trim(),
                    Estilo = string.IsNullOrWhiteSpace(Estilo) ? null : Estilo.Trim(),
                    Tamano = string.IsNullOrWhiteSpace(Tamano) ? null : Tamano.Trim(),
                    Colores = Colores,
                    Precio = Precio,
                    Fecha = Fecha.Value.DateTime,
                    DuracionMinutos = DuracionMinutos,
                    Notas = string.IsNullOrWhiteSpace(Notas) ? null : Notas.Trim(),
                    FechaCreacion = DateTime.Now
                };
                
                _db.Trabajos.Add(nuevoTrabajo);
            }

            await _db.SaveChangesAsync();
            Log.Information("Trabajo guardado exitosamente");
            
            MostrarFormulario = false;
            await CargarTrabajos();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al guardar trabajo. Cliente: {ClienteId}, Descripción: {Descripcion}", 
                ClienteSeleccionado?.Id, Descripcion);
            MensajeError = $"Error al guardar: {ex.Message}";
        }
        finally
        {
            Cargando = false;
        }
    }

    /// <summary>
    /// Elimina el trabajo seleccionado.
    /// </summary>
    [RelayCommand]
    private async Task EliminarTrabajo()
    {
        if (TrabajoSeleccionado == null) return;

        try
        {
            Log.Warning("Eliminando trabajo ID: {TrabajoId}, Descripción: {Descripcion}", 
                TrabajoSeleccionado.Id, TrabajoSeleccionado.Descripcion);
            Cargando = true;
            
            _db.Trabajos.Remove(TrabajoSeleccionado);
            await _db.SaveChangesAsync();
            
            Log.Information("Trabajo eliminado exitosamente: ID {TrabajoId}", TrabajoSeleccionado.Id);
            MostrarFormulario = false;
            await CargarTrabajos();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al eliminar trabajo ID: {TrabajoId}", TrabajoSeleccionado?.Id);
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
        ClientePreseleccionado = null;
    }

    #endregion

    #region Métodos Privados

    /// <summary>
    /// Limpia todos los campos del formulario.
    /// </summary>
    private void LimpiarFormulario()
    {
        ClienteSeleccionado = null;
        TipoTrabajo = TipoTrabajo.Tatuaje;
        Descripcion = string.Empty;
        ZonaCuerpo = string.Empty;
        Estilo = null;
        Tamano = null;
        Colores = false;
        Precio = 0;
        Fecha = DateTimeOffset.Now.Date;
        DuracionMinutos = 60;
        Notas = null;
        MensajeError = string.Empty;
    }

    /// <summary>
    /// Carga los datos de un trabajo en el formulario.
    /// </summary>
    /// <param name="trabajo">Trabajo a cargar.</param>
    private void CargarTrabajoEnFormulario(Trabajo trabajo)
    {
        ClienteSeleccionado = trabajo.Cliente;
        TipoTrabajo = trabajo.Tipo;
        Descripcion = trabajo.Descripcion;
        ZonaCuerpo = trabajo.ZonaCuerpo;
        Estilo = trabajo.Estilo;
        Tamano = trabajo.Tamano;
        Colores = trabajo.Colores;
        Precio = trabajo.Precio;
        Fecha = new DateTimeOffset(trabajo.Fecha);
        DuracionMinutos = trabajo.DuracionMinutos;
        Notas = trabajo.Notas;
        MensajeError = string.Empty;
    }

    #endregion
}

