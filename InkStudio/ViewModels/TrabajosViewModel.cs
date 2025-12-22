using System;
using System.Collections.ObjectModel;
using System.IO;
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
    private ClientesViewModel? _clientesVM;

    /// <summary>
    /// Permite inyectar el ViewModel de Clientes para refrescar la ficha cuando cambian trabajos.
    /// </summary>
    public void SetClientesViewModel(ClientesViewModel clientesViewModel)
    {
        _clientesVM = clientesViewModel;
    }

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

    /// <summary>
    /// ViewModel del modal de firma de consentimientos.
    /// </summary>
    [ObservableProperty]
    private ConsentimientoFirmaViewModel? _consentimientoFirmaVM;

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
    /// Indica si se debe mostrar el aviso de que el trabajo no se puede modificar (solo en edición y con consentimiento).
    /// </summary>
    public bool MostrarAvisoNoModificable => EsEdicion && TrabajoSeleccionado != null && TrabajoSeleccionado.TieneConsentimiento;

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
    /// Se ejecuta cuando cambia EsEdicion.
    /// </summary>
    partial void OnEsEdicionChanged(bool value)
    {
        OnPropertyChanged(nameof(MostrarAvisoNoModificable));
    }

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
                .Include(t => t.Consentimiento)
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
    /// Se asegura de usar la instancia de cliente del propio DbContext para que
    /// el ComboBox quede correctamente seleccionado.
    /// </summary>
    public async Task NuevoTrabajoParaCliente(Cliente cliente)
    {
        LimpiarFormulario();

        try
        {
            // Asegurar que la lista de clientes está cargada
            if (!Clientes.Any())
            {
                await CargarClientes();
            }

            // Buscar al cliente por Id dentro de la colección propia
            var clienteEnContexto = Clientes.FirstOrDefault(c => c.Id == cliente.Id);

            ClienteSeleccionado = clienteEnContexto ?? cliente;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al preseleccionar cliente en NuevoTrabajoParaCliente (ClienteId: {ClienteId})", cliente.Id);
            MensajeError = "No se pudo preseleccionar el cliente en el formulario de trabajo.";
        }

        EsEdicion = false;
        TituloFormulario = "✨ Nuevo Trabajo";
        MostrarFormulario = true;
    }

    /// <summary>
    /// Abre el formulario para editar el trabajo seleccionado.
    /// </summary>
    [RelayCommand]
    private async Task EditarTrabajo()
    {
        if (TrabajoSeleccionado == null) return;

        // Cargar consentimiento para verificar estado (pero permitir abrir el modal para ver datos)
        await _db.Entry(TrabajoSeleccionado).Reference(t => t.Consentimiento).LoadAsync();

        CargarTrabajoEnFormulario(TrabajoSeleccionado);
        EsEdicion = true;
        TituloFormulario = "✏️ Editar Trabajo";
        MostrarFormulario = true;
        OnPropertyChanged(nameof(MostrarAvisoNoModificable));
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
            
            // Obtener el trabajo guardado (nuevo o actualizado)
            Trabajo trabajoGuardado;
            if (EsEdicion && TrabajoSeleccionado != null)
            {
                trabajoGuardado = TrabajoSeleccionado;
            }
            else
            {
                // Para trabajos nuevos, obtener el último trabajo del cliente
                trabajoGuardado = await _db.Trabajos
                    .Include(t => t.Cliente)
                    .Include(t => t.Consentimiento)
                    .Where(t => t.ClienteId == ClienteSeleccionado.Id)
                    .OrderByDescending(t => t.FechaCreacion)
                    .FirstOrDefaultAsync();
                
                if (trabajoGuardado == null)
                {
                    Log.Warning("No se pudo encontrar el trabajo guardado después de SaveChanges");
                    MostrarFormulario = false;
                    await CargarTrabajos();
                    return;
                }
            }
            
            if (trabajoGuardado != null)
            {
                // Recargar relaciones
                await _db.Entry(trabajoGuardado).Reference(t => t.Cliente).LoadAsync();
                await _db.Entry(trabajoGuardado).Reference(t => t.Consentimiento).LoadAsync();
                
                // Cargar consentimientos del cliente para verificar RGPD
                await _db.Entry(trabajoGuardado.Cliente).Collection(c => c.Consentimientos).LoadAsync();
                
                // Verificar si el cliente tiene RGPD (ahora con consentimientos cargados)
                var tieneRGPD = trabajoGuardado.Cliente.TieneConsentimientoRGPD;
                
                if (!tieneRGPD)
                {
                    // Mostrar aviso no bloqueante
                    TrabajoPendienteConsentimiento = trabajoGuardado;
                    MensajeAvisoConsentimiento = "⚠️ El cliente no tiene RGPD firmado. Recuerda solicitarlo antes de realizar el trabajo.";
                    MostrarAvisoConsentimiento = true;
                }
                else
                {
                    // Verificar si el trabajo tiene consentimiento
                    var tieneConsentimiento = trabajoGuardado.Consentimiento != null && trabajoGuardado.Consentimiento.Firmado;
                    
                    if (!tieneConsentimiento)
                    {
                        // Mostrar aviso no bloqueante
                        TrabajoPendienteConsentimiento = trabajoGuardado;
                        MensajeAvisoConsentimiento = "✅ Trabajo guardado. Recuerda firmar el consentimiento de trabajo.";
                        MostrarAvisoConsentimiento = true;
                    }
                }
            }
            
            MostrarFormulario = false;
            await CargarTrabajos();

            // Si la ficha del cliente está abierta, refrescarla para reflejar el nuevo trabajo
            if (_clientesVM != null && _clientesVM.MostrarFicha && 
                _clientesVM.ClienteSeleccionado != null &&
                _clientesVM.ClienteSeleccionado.Id == ClienteSeleccionado.Id)
            {
                await _clientesVM.VerFichaClienteCommand.ExecuteAsync(_clientesVM.ClienteSeleccionado);
            }
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
    /// Verifica si un trabajo tiene consentimiento firmado.
    /// </summary>
    public async Task<bool> TrabajoTieneConsentimiento(int trabajoId)
    {
        try
        {
            var trabajo = await _db.Trabajos
                .Include(t => t.Consentimiento)
                .FirstOrDefaultAsync(t => t.Id == trabajoId);
            
            return trabajo?.Consentimiento != null && trabajo.Consentimiento.Firmado;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al verificar consentimiento del trabajo {TrabajoId}", trabajoId);
            return false;
        }
    }

    /// <summary>
    /// Abre el modal para firmar el consentimiento de trabajo.
    /// </summary>
    [RelayCommand]
    private async Task FirmarConsentimientoTrabajo(Trabajo? trabajo = null)
    {
        var trabajoAFirmar = trabajo ?? TrabajoSeleccionado;
        if (trabajoAFirmar == null) return;

        try
        {
            // Recargar trabajo con relaciones
            await _db.Entry(trabajoAFirmar).ReloadAsync();
            await _db.Entry(trabajoAFirmar).Reference(t => t.Cliente).LoadAsync();
            await _db.Entry(trabajoAFirmar).Reference(t => t.Consentimiento).LoadAsync();
            
            // Cargar consentimientos del cliente para verificar RGPD
            await _db.Entry(trabajoAFirmar.Cliente).Collection(c => c.Consentimientos).LoadAsync();
            
            // Verificar si ya tiene consentimiento firmado
            if (trabajoAFirmar.Consentimiento != null && trabajoAFirmar.Consentimiento.Firmado)
            {
                MensajeError = "Este trabajo ya tiene el consentimiento firmado";
                await CargarTrabajos();
                return;
            }

            // Verificar que el cliente tenga RGPD (ahora con consentimientos cargados)
            if (!trabajoAFirmar.Cliente.TieneConsentimientoRGPD)
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
                    await CargarTrabajos();
                    // Si estamos editando un trabajo, recargar el trabajo para actualizar el consentimiento
                    if (EsEdicion && TrabajoSeleccionado != null)
                    {
                        await _db.Entry(TrabajoSeleccionado).ReloadAsync();
                        await _db.Entry(TrabajoSeleccionado).Reference(t => t.Consentimiento).LoadAsync();
                        OnPropertyChanged(nameof(TrabajoSeleccionado));
                        OnPropertyChanged(nameof(MostrarAvisoNoModificable));
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

    /// <summary>
    /// Abre el PDF del consentimiento de trabajo.
    /// </summary>
    [RelayCommand]
    private Task AbrirConsentimientoTrabajo(Trabajo? trabajo = null)
    {
        var trabajoAVer = trabajo ?? TrabajoSeleccionado;
        if (trabajoAVer == null || trabajoAVer.Consentimiento == null) return Task.CompletedTask;

        try
        {
            if (string.IsNullOrEmpty(trabajoAVer.Consentimiento.RutaDocumento))
            {
                MensajeError = "No se encontró el documento PDF para este consentimiento.";
                return Task.CompletedTask;
            }

            if (!System.IO.File.Exists(trabajoAVer.Consentimiento.RutaDocumento))
            {
                MensajeError = $"El archivo PDF no existe en la ruta: {trabajoAVer.Consentimiento.RutaDocumento}";
                Log.Warning("PDF no encontrado: {Ruta}", trabajoAVer.Consentimiento.RutaDocumento);
                return Task.CompletedTask;
            }

            // Abrir el PDF con el visor predeterminado del sistema
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = trabajoAVer.Consentimiento.RutaDocumento,
                UseShellExecute = true
            });

            Log.Information("PDF abierto: {Ruta}", trabajoAVer.Consentimiento.RutaDocumento);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al abrir PDF del consentimiento");
            MensajeError = $"Error al abrir el PDF: {ex.Message}";
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Exporta el PDF del consentimiento de trabajo a la carpeta de Descargas.
    /// </summary>
    [RelayCommand]
    private Task ExportarConsentimientoTrabajo(Trabajo? trabajo = null)
    {
        var trabajoAVer = trabajo ?? TrabajoSeleccionado;
        if (trabajoAVer == null || trabajoAVer.Consentimiento == null) return Task.CompletedTask;

        try
        {
            if (string.IsNullOrEmpty(trabajoAVer.Consentimiento.RutaDocumento))
            {
                MensajeError = "No se encontró el documento PDF para este consentimiento.";
                return Task.CompletedTask;
            }

            if (!System.IO.File.Exists(trabajoAVer.Consentimiento.RutaDocumento))
            {
                MensajeError = $"El archivo PDF no existe en la ruta: {trabajoAVer.Consentimiento.RutaDocumento}";
                return Task.CompletedTask;
            }

            // Copiar a la carpeta de Descargas
            var descargas = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            var nombreArchivo = Path.GetFileName(trabajoAVer.Consentimiento.RutaDocumento);
            var rutaDestino = Path.Combine(descargas, nombreArchivo);

            // Si ya existe, agregar un número
            int contador = 1;
            while (System.IO.File.Exists(rutaDestino))
            {
                var nombreSinExtension = Path.GetFileNameWithoutExtension(nombreArchivo);
                var extension = Path.GetExtension(nombreArchivo);
                nombreArchivo = $"{nombreSinExtension} ({contador}){extension}";
                rutaDestino = Path.Combine(descargas, nombreArchivo);
                contador++;
            }

            System.IO.File.Copy(trabajoAVer.Consentimiento.RutaDocumento, rutaDestino);
            Log.Information("PDF exportado a: {Ruta}", rutaDestino);
            MensajeError = string.Empty; // Limpiar mensajes anteriores
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al exportar PDF del consentimiento");
            MensajeError = $"Error al exportar el PDF: {ex.Message}";
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Envía el PDF del consentimiento de trabajo por correo electrónico.
    /// </summary>
    [RelayCommand]
    private Task EnviarConsentimientoTrabajoPorCorreo(Trabajo? trabajo = null)
    {
        var trabajoAVer = trabajo ?? TrabajoSeleccionado;
        if (trabajoAVer == null || trabajoAVer.Consentimiento == null) return Task.CompletedTask;

        try
        {
            if (string.IsNullOrEmpty(trabajoAVer.Consentimiento.RutaDocumento))
            {
                MensajeError = "No se encontró el documento PDF para este consentimiento.";
                return Task.CompletedTask;
            }

            if (!System.IO.File.Exists(trabajoAVer.Consentimiento.RutaDocumento))
            {
                MensajeError = $"El archivo PDF no existe en la ruta: {trabajoAVer.Consentimiento.RutaDocumento}";
                return Task.CompletedTask;
            }

            // Cargar cliente si no está cargado
            if (trabajoAVer.Cliente == null)
            {
                _db.Entry(trabajoAVer).Reference(t => t.Cliente).Load();
            }

            if (string.IsNullOrEmpty(trabajoAVer.Cliente?.Email))
            {
                MensajeError = "El cliente no tiene un email registrado. Por favor, añade un email al cliente primero.";
                return Task.CompletedTask;
            }

            // Crear mailto con el PDF adjunto (si el cliente de correo lo soporta)
            var asunto = Uri.EscapeDataString($"Consentimiento de Trabajo - {trabajoAVer.Descripcion}");
            var cuerpo = Uri.EscapeDataString($"Adjunto encontrarás el consentimiento de trabajo firmado.\n\nTrabajo: {trabajoAVer.Descripcion}\nFecha: {trabajoAVer.Fecha:dd/MM/yyyy}");
            var mailto = $"mailto:{trabajoAVer.Cliente.Email}?subject={asunto}&body={cuerpo}";

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = mailto,
                UseShellExecute = true
            });

            Log.Information("Correo abierto para enviar consentimiento a: {Email}", trabajoAVer.Cliente.Email);
            MensajeError = string.Empty; // Limpiar mensajes anteriores
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al abrir cliente de correo para consentimiento");
            MensajeError = $"Error al abrir el cliente de correo: {ex.Message}";
        }

        return Task.CompletedTask;
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

            // Si la ficha del cliente está abierta, refrescarla para reflejar la eliminación
            if (_clientesVM != null && _clientesVM.MostrarFicha && 
                _clientesVM.ClienteSeleccionado != null &&
                _clientesVM.ClienteSeleccionado.Id == ClienteSeleccionado?.Id)
            {
                await _clientesVM.VerFichaClienteCommand.ExecuteAsync(_clientesVM.ClienteSeleccionado);
            }
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
    private async void CargarTrabajoEnFormulario(Trabajo trabajo)
    {
        // Cargar relaciones necesarias
        await _db.Entry(trabajo).Reference(t => t.Cliente).LoadAsync();
        await _db.Entry(trabajo).Reference(t => t.Consentimiento).LoadAsync();
        
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
        
        // Notificar cambios en las propiedades del trabajo para actualizar la UI
        OnPropertyChanged(nameof(TrabajoSeleccionado));
    }

    #endregion
}

