using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Ataena.Data;
using Ataena.Models;
using Ataena.Services;
using Serilog;

namespace Ataena.ViewModels;

/// <summary>
/// ViewModel para la gestión de trabajos (tatuajes y piercings).
/// Implementa operaciones CRUD y búsqueda.
/// </summary>
public partial class TrabajosViewModel : ViewModelBase
{
    private readonly AtaenaDbContext _db = new();
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
    /// Clientes que cumplen la búsqueda en el formulario de trabajo (nombre, apellidos o teléfono).
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<Cliente> _clientesFiltradosFormulario = new();

    /// <summary>
    /// Texto para filtrar el desplegable de cliente en el modal de trabajo.
    /// </summary>
    [ObservableProperty]
    private string _textoBusquedaClienteFormulario = string.Empty;

    partial void OnTextoBusquedaClienteFormularioChanged(string value)
    {
        ActualizarClientesFiltradosFormulario();
    }

    partial void OnClientesChanged(ObservableCollection<Cliente> value)
    {
        ActualizarClientesFiltradosFormulario();
    }

    /// <summary>
    /// ViewModel del modal de firma de consentimientos.
    /// </summary>
    [ObservableProperty]
    private ConsentimientoFirmaViewModel? _consentimientoFirmaVM;

    /// <summary>
    /// ViewModel del modal de fotos de trabajo.
    /// </summary>
    [ObservableProperty]
    private FotoTrabajoViewModel? _fotoTrabajoVM;

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

    /// <summary>
    /// Formulario de edición con consentimiento de trabajo ya firmado: bloquea cliente, tipo, descripción y notas.
    /// </summary>
    public bool TrabajoFormularioBloqueadoPorConsentimiento =>
        EsEdicion && TrabajoSeleccionado != null && TrabajoSeleccionado.TieneConsentimiento;

    /// <summary>
    /// El trabajo seleccionado tiene cliente que acepta fotos antes/después (UI).
    /// </summary>
    public bool ClientePermiteFotosEnTrabajo =>
        TrabajoSeleccionado?.Cliente != null && TrabajoSeleccionado.Cliente.PermiteFotosTrabajo;

    /// <summary>
    /// Si es false, cliente/tipo/descripción/notas se muestran deshabilitados (solo fotos y documentos siguen activos).
    /// </summary>
    public bool TrabajoCamposPrincipalesHabilitados => !TrabajoFormularioBloqueadoPorConsentimiento;

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
        OnPropertyChanged(nameof(TrabajoFormularioBloqueadoPorConsentimiento));
        OnPropertyChanged(nameof(TrabajoCamposPrincipalesHabilitados));
        OnPropertyChanged(nameof(ClientePermiteFotosEnTrabajo));
    }

    /// <summary>
    /// Trabajo seleccionado cambia: refrescar estado del formulario ante consentimiento.
    /// </summary>
    partial void OnTrabajoSeleccionadoChanged(Trabajo? value)
    {
        OnPropertyChanged(nameof(MostrarAvisoNoModificable));
        OnPropertyChanged(nameof(TrabajoFormularioBloqueadoPorConsentimiento));
        OnPropertyChanged(nameof(TrabajoCamposPrincipalesHabilitados));
        OnPropertyChanged(nameof(ClientePermiteFotosEnTrabajo));
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

    /// <summary>
    /// Notas internas del trabajo (no forman parte del consentimiento PDF).
    /// </summary>
    [ObservableProperty]
    private string? _notas;

    /// <summary>
    /// Imagen de la foto "antes" del trabajo (para preview en la UI).
    /// </summary>
    [ObservableProperty]
    private Bitmap? _fotoAntesImagen;

    /// <summary>
    /// Imagen de la foto "después" del trabajo (para preview en la UI).
    /// </summary>
    [ObservableProperty]
    private Bitmap? _fotoDespuesImagen;

    /// <summary>
    /// Placeholder "Sin foto" en la vista (no usar <c>!<see cref="FotoAntesImagen"/></c> en AXAML sobre un Bitmap).
    /// </summary>
    public bool MuestraSinFotoAntes => FotoAntesImagen == null;

    /// <inheritdoc cref="MuestraSinFotoAntes"/>
    public bool MuestraSinFotoDespues => FotoDespuesImagen == null;

    partial void OnFotoAntesImagenChanged(Bitmap? value)
    {
        OnPropertyChanged(nameof(MuestraSinFotoAntes));
    }

    partial void OnFotoDespuesImagenChanged(Bitmap? value)
    {
        OnPropertyChanged(nameof(MuestraSinFotoDespues));
    }

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
        FotoTrabajoVM = new FotoTrabajoViewModel(_db);
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
                .Include(t => t.Citas)
                .Include(t => t.Consentimiento)
                .AsQueryable();

            // Aplicar filtro de cliente si existe
            if (ClienteFiltro != null)
            {
                query = query.Where(t => t.ClienteId == ClienteFiltro.Id);
            }

            var lista = await query
                .OrderByDescending(t => t.FechaCreacion)
                .ThenByDescending(t => t.Id)
                .ToListAsync();

            if (!string.IsNullOrWhiteSpace(TextoBusqueda))
            {
                var busqueda = TextoBusqueda.Trim().ToLower();
                var busquedaDigitos = new string(TextoBusqueda.Where(char.IsDigit).ToArray());
                lista = lista
                    .Where(t => TrabajoCoincideBusqueda(t, busqueda, busquedaDigitos))
                    .ToList();
            }

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
                .AsNoTracking()
                .OrderBy(c => c.Nombre)
                .ThenBy(c => c.Apellidos)
                .ToListAsync();

            Clientes = new ObservableCollection<Cliente>(lista);

            // Mantener el filtro si seguía aplicado (evita instancia huérfana al reemplazar la colección)
            if (ClienteFiltro != null)
            {
                var idFiltro = ClienteFiltro.Id;
                ClienteFiltro = lista.FirstOrDefault(c => c.Id == idFiltro);
            }

            ActualizarClientesFiltradosFormulario();

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
    private async Task NuevoTrabajo()
    {
        await CargarClientes();

        LimpiarFormulario();

        // Si hay un cliente pre-seleccionado (desde modal de cita), usarlo
        if (ClientePreseleccionado != null)
        {
            ClienteSeleccionado = Clientes.FirstOrDefault(c => c.Id == ClientePreseleccionado.Id) ?? ClientePreseleccionado;
            ClientePreseleccionado = null; // Limpiar después de usar
        }

        ActualizarClientesFiltradosFormulario();
        
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
            await CargarClientes();

            // Buscar al cliente por Id dentro de la colección actual (misma instancia que el ComboBox)
            var clienteEnContexto = Clientes.FirstOrDefault(c => c.Id == cliente.Id);

            ClienteSeleccionado = clienteEnContexto ?? cliente;
            ActualizarClientesFiltradosFormulario();
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

        await CargarClientes();

        // Cargar consentimiento para verificar estado (pero permitir abrir el modal para ver datos)
        await _db.Entry(TrabajoSeleccionado).Reference(t => t.Consentimiento).LoadAsync();

        await CargarTrabajoEnFormularioAsync(TrabajoSeleccionado);
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
                TrabajoSeleccionado.Notas = string.IsNullOrWhiteSpace(Notas) ? null : Notas.Trim();
            }
            else
            {
                // Crear nuevo trabajo
                Log.Information("Creando nuevo trabajo para cliente {ClienteId}: {Descripcion}", 
                    ClienteSeleccionado.Id, Descripcion);
                
                var ahora = DateTime.Now;
                var nuevoTrabajo = new Trabajo
                {
                    ClienteId = ClienteSeleccionado.Id,
                    Tipo = TipoTrabajo,
                    Descripcion = Descripcion.Trim(),
                    Notas = string.IsNullOrWhiteSpace(Notas) ? null : Notas.Trim(),
                    ZonaCuerpo = string.Empty,
                    Estado = EstadoTrabajo.Diseno,
                    Precio = 0,
                    Colores = false,
                    FechaCreacion = ahora,
                    Fecha = ahora
                };
                
                _db.Trabajos.Add(nuevoTrabajo);
            }

            await _db.SaveChangesAsync();
            Log.Information("Trabajo guardado exitosamente");
            
            // Obtener el trabajo guardado (nuevo o actualizado)
            Trabajo? trabajoGuardado = null;
            if (EsEdicion && TrabajoSeleccionado != null)
            {
                trabajoGuardado = TrabajoSeleccionado;
            }
            else
            {
                // Para trabajos nuevos, obtener el último trabajo del cliente
                if (ClienteSeleccionado == null)
                {
                    Log.Warning("ClienteSeleccionado es null al intentar obtener el trabajo guardado");
                    MostrarFormulario = false;
                    await CargarTrabajos();
                    return;
                }

                var clienteId = ClienteSeleccionado!.Id;

                trabajoGuardado = await _db.Trabajos
                    .Include(t => t.Cliente)
                    .Include(t => t.Consentimiento)
                    .Where(t => t.ClienteId == clienteId)
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
    /// Inicia la captura de la foto ANTES del trabajo.
    /// </summary>
    [RelayCommand]
    private async Task TomarFotoAntes(Trabajo? trabajo)
    {
        await IniciarCapturaFotoAsync(trabajo, esAntes: true);
    }

    /// <summary>
    /// Inicia la captura de la foto DESPUÉS del trabajo.
    /// </summary>
    [RelayCommand]
    private async Task TomarFotoDespues(Trabajo? trabajo)
    {
        await IniciarCapturaFotoAsync(trabajo, esAntes: false);
    }

    /// <summary>
    /// Abre el explorador de archivos en la carpeta de fotos del trabajo.
    /// </summary>
    [RelayCommand]
    private Task AbrirCarpetaFotosTrabajo(Trabajo? trabajo)
    {
        var trabajoVer = trabajo ?? TrabajoSeleccionado;
        if (trabajoVer == null)
            return Task.CompletedTask;

        try
        {
            var rutaCarpeta = Services.ConsentimientoPathService.ObtenerRutaCarpetaTrabajo(trabajoVer.ClienteId, trabajoVer.Id);
            if (!Directory.Exists(rutaCarpeta))
            {
                Directory.CreateDirectory(rutaCarpeta);
            }

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = rutaCarpeta,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al abrir carpeta de fotos para trabajo {TrabajoId}", trabajoVer.Id);
            MensajeError = $"Error al abrir la carpeta de fotos: {ex.Message}";
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Lógica común para iniciar la captura de foto, validando consentimientos.
    /// </summary>
    private async Task IniciarCapturaFotoAsync(Trabajo? trabajo, bool esAntes)
    {
        var trabajoFoto = trabajo ?? TrabajoSeleccionado;
        if (trabajoFoto == null)
        {
            MensajeError = "No hay trabajo seleccionado.";
            return;
        }

        try
        {
            var clienteId = trabajoFoto.ClienteId;

            // Contexto fresco: evita leer estado desactualizado tras firmar en otro ViewModel/contexto SQLite.
            // La edad se consulta igual que en Cliente (TotalDays / 365.25) para alinear EsMenorDeEdad con la ficha.
            await using var readDb = new AtaenaDbContext();
            var clientePrefs = await readDb.Clientes.AsNoTracking()
                .Where(c => c.Id == clienteId)
                .Select(c => new { c.FechaNacimiento, c.PermiteFotosTrabajo })
                .FirstOrDefaultAsync();

            if (clientePrefs == null)
            {
                OverlayNotificationService.Mostrar(
                    "No se encontró el cliente del trabajo.",
                    OverlayNotificationKind.Warning);
                return;
            }

            if (!clientePrefs.PermiteFotosTrabajo)
            {
                OverlayNotificationService.Mostrar(
                    "Este cliente no autoriza fotos de trabajo. Actívalo al editar el cliente en Clientes si debe poder tomarse fotos y firmarse el consentimiento de imágenes.",
                    OverlayNotificationKind.Warning);
                return;
            }

            var fechaNacimiento = clientePrefs.FechaNacimiento;
            var esMenorDeEdad = fechaNacimiento.HasValue &&
                (int)((DateTime.Today - fechaNacimiento.Value).TotalDays / 365.25) < 18;

            var consentCliente = await readDb.Consentimientos.AsNoTracking()
                .Where(c => c.ClienteId == clienteId)
                .ToListAsync();

            var tieneRgpd = Consentimiento.TieneConsentimientoRgpdVigente(consentCliente);
            var tieneImagenes =
                Consentimiento.TieneConsentimientoImagenesVigente(consentCliente, esMenorDeEdad);

            // Misma condición que en la vista: Trabajo.TieneConsentimiento (solo Firmado; no usar !Renovado aquí).
            // Con FK 1:1 por trabajo, marcados Renovado por errores históricos bloqueaban fotos teniendo ✅ en pantalla.
            var tieneConsentimientoTrabajo = await readDb.Trabajos.AsNoTracking()
                .Where(t => t.Id == trabajoFoto.Id)
                .Select(t => t.Consentimiento != null && t.Consentimiento.Firmado)
                .FirstOrDefaultAsync();

            // Validaciones de consentimiento (aviso en primera plana sobre modales locales)
            if (!tieneRgpd)
            {
                OverlayNotificationService.Mostrar(
                    "El cliente debe tener el consentimiento RGPD firmado antes de tomar fotos.",
                    OverlayNotificationKind.Warning);
                return;
            }

            if (!tieneImagenes)
            {
                OverlayNotificationService.Mostrar(
                    esMenorDeEdad
                        ? "El tutor debe tener firmado el consentimiento de uso de imágenes del menor antes de tomar fotos."
                        : "El cliente debe tener firmado el consentimiento de uso de imágenes antes de tomar fotos.",
                    OverlayNotificationKind.Warning);
                return;
            }

            if (!tieneConsentimientoTrabajo)
            {
                OverlayNotificationService.Mostrar(
                    "Primero debe firmarse el consentimiento de trabajo antes de tomar fotos.",
                    OverlayNotificationKind.Warning);
                return;
            }

            MensajeError = string.Empty;

            if (FotoTrabajoVM == null)
            {
                FotoTrabajoVM = new FotoTrabajoViewModel(_db);
                // Suscribirse al evento de foto guardada para refrescar las imágenes en la UI
                FotoTrabajoVM.FotoGuardada += async (s, trabajoActualizado) =>
                {
                    // Refrescar las fotos cuando se guarda una nueva
                    if (EsEdicion && TrabajoSeleccionado != null && TrabajoSeleccionado.Id == trabajoActualizado.Id)
                    {
                        await RefrescarFotosTrabajoAsync();
                    }
                };
            }

            await FotoTrabajoVM.AbrirModalAsync(trabajoFoto, esAntes);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al iniciar captura de foto para trabajo {TrabajoId}", trabajoFoto.Id);
            MensajeError = $"Error al iniciar la captura de foto: {ex.Message}";
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

            // Validar datos del tutor si es menor
            if (trabajoAFirmar.Cliente.EsMenorDeEdad && !trabajoAFirmar.Cliente.TieneDatosTutor)
            {
                MensajeError = "⚠️ Para firmar consentimiento de menor se requieren los datos del tutor. Edita el cliente primero.";
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
                        OnPropertyChanged(nameof(TrabajoFormularioBloqueadoPorConsentimiento));
                        OnPropertyChanged(nameof(TrabajoCamposPrincipalesHabilitados));
                    }
                };
            }
            
            // Usar tipo de consentimiento correcto según edad
            var tipoConsentimiento = trabajoAFirmar.Cliente.EsMenorDeEdad 
                ? TipoConsentimiento.Trabajo_Menor 
                : TipoConsentimiento.Trabajo;
            
            await ConsentimientoFirmaVM.AbrirModal(trabajoAFirmar.Cliente, tipoConsentimiento, trabajoAFirmar);
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
            var cuerpo = Uri.EscapeDataString($"Adjunto encontrarás el consentimiento de trabajo firmado.\n\nTrabajo: {trabajoAVer.Descripcion}\nFecha (alta): {trabajoAVer.FechaCreacion:dd/MM/yyyy}");
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
    /// Muestra un diálogo de confirmación antes de proceder.
    /// </summary>
    [RelayCommand]
    private async Task EliminarTrabajo()
    {
        if (TrabajoSeleccionado == null) return;

        // Contar citas asociadas al trabajo
        var numCitas = await _db.Citas.CountAsync(c => c.TrabajoId == TrabajoSeleccionado.Id);
        var tieneConsentimiento = TrabajoSeleccionado.Consentimiento != null;
        
        string advertencia;
        if (numCitas > 0 && tieneConsentimiento)
        {
            advertencia = $"También se eliminarán {numCitas} cita(s) y el consentimiento firmado.";
        }
        else if (numCitas > 0)
        {
            advertencia = $"También se eliminarán {numCitas} cita(s) asociada(s).";
        }
        else if (tieneConsentimiento)
        {
            advertencia = "También se eliminará el consentimiento firmado.";
        }
        else
        {
            advertencia = "Esta acción no se puede deshacer.";
        }

        // Mostrar diálogo de confirmación
        var confirmado = await DialogService.ConfirmarEliminarAsync(
            tipoElemento: "el trabajo",
            nombreElemento: $"{TrabajoSeleccionado.Descripcion} ({TrabajoSeleccionado.Cliente?.NombreCompleto ?? "Cliente"})",
            advertenciaAdicional: advertencia
        );

        if (!confirmado)
        {
            Log.Debug("Eliminación de trabajo cancelada por el usuario: {TrabajoId}", TrabajoSeleccionado.Id);
            return;
        }

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
        // Al crear un nuevo trabajo no debe influir el trabajo seleccionado en la lista
        TrabajoSeleccionado = null;

        ClienteSeleccionado = null;
        TipoTrabajo = TipoTrabajo.Tatuaje;
        Descripcion = string.Empty;
        Notas = null;
        MensajeError = string.Empty;
        FotoAntesImagen = null;
        FotoDespuesImagen = null;
        TextoBusquedaClienteFormulario = string.Empty;
        ActualizarClientesFiltradosFormulario();
    }

    /// <summary>
    /// Carga los datos de un trabajo en el formulario (solo campos editables en UI: cliente, tipo, descripción, notas).
    /// </summary>
    private async Task CargarTrabajoEnFormularioAsync(Trabajo trabajo)
    {
        await _db.Entry(trabajo).Reference(t => t.Cliente).LoadAsync();
        await _db.Entry(trabajo).Reference(t => t.Consentimiento).LoadAsync();

        ClienteSeleccionado = Clientes.FirstOrDefault(c => c.Id == trabajo.ClienteId) ?? trabajo.Cliente;
        TipoTrabajo = trabajo.Tipo;
        Descripcion = trabajo.Descripcion;
        Notas = trabajo.Notas;
        MensajeError = string.Empty;

        CargarFotosTrabajo(trabajo);

        OnPropertyChanged(nameof(TrabajoSeleccionado));
        OnPropertyChanged(nameof(TrabajoFormularioBloqueadoPorConsentimiento));
        OnPropertyChanged(nameof(TrabajoCamposPrincipalesHabilitados));
        OnPropertyChanged(nameof(ClientePermiteFotosEnTrabajo));
        ActualizarClientesFiltradosFormulario();
    }

    /// <summary>
    /// Comprueba si un trabajo coincide con el texto de búsqueda (incluye teléfono solo dígitos).
    /// </summary>
    private static bool TrabajoCoincideBusqueda(Trabajo trabajo, string busquedaLower, string busquedaDigitos)
    {
        var cliente = trabajo.Cliente;

        if (!string.IsNullOrEmpty(busquedaLower))
        {
            if (trabajo.Descripcion != null &&
                trabajo.Descripcion.Contains(busquedaLower, StringComparison.OrdinalIgnoreCase))
                return true;

            if (trabajo.ZonaCuerpo != null &&
                trabajo.ZonaCuerpo.Contains(busquedaLower, StringComparison.OrdinalIgnoreCase))
                return true;

            if (trabajo.Estilo != null &&
                trabajo.Estilo.Contains(busquedaLower, StringComparison.OrdinalIgnoreCase))
                return true;

            if (cliente.Nombre.Contains(busquedaLower, StringComparison.OrdinalIgnoreCase) ||
                cliente.Apellidos.Contains(busquedaLower, StringComparison.OrdinalIgnoreCase) ||
                $"{cliente.Nombre} {cliente.Apellidos}".Contains(busquedaLower, StringComparison.OrdinalIgnoreCase))
                return true;

            if (!string.IsNullOrEmpty(cliente.Telefono) &&
                cliente.Telefono.Contains(busquedaLower, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        if (!string.IsNullOrEmpty(busquedaDigitos))
        {
            var telDigitos = new string((cliente.Telefono ?? "").Where(char.IsDigit).ToArray());
            if (telDigitos.Contains(busquedaDigitos, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Lista desplegable de clientes según texto de búsqueda y mantiene el seleccionado con la misma instancia que <see cref="Clientes"/>.
    /// </summary>
    private void ActualizarClientesFiltradosFormulario()
    {
        ClientesFiltradosFormulario.Clear();
        if (Clientes.Count == 0)
            return;

        const int limite = 300;
        var qRaw = TextoBusquedaClienteFormulario?.Trim() ?? string.Empty;

        IEnumerable<Cliente> candidatos = Clientes;
        if (!string.IsNullOrWhiteSpace(qRaw))
        {
            var ql = qRaw;
            var qDigits = new string(qRaw.Where(char.IsDigit).ToArray());
            candidatos = Clientes.Where(c =>
                c.Nombre.Contains(ql, StringComparison.OrdinalIgnoreCase) ||
                c.Apellidos.Contains(ql, StringComparison.OrdinalIgnoreCase) ||
                $"{c.Nombre} {c.Apellidos}".Contains(ql, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrEmpty(c.Telefono) && c.Telefono.Contains(ql, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(qDigits)
                 && new string((c.Telefono ?? "").Where(char.IsDigit).ToArray())
                     .Contains(qDigits, StringComparison.Ordinal)));
        }

        var ordenados = candidatos
            .OrderBy(c => c.Nombre)
            .ThenBy(c => c.Apellidos)
            .Take(limite)
            .ToList();

        foreach (var c in ordenados)
            ClientesFiltradosFormulario.Add(c);

        AsegurarClienteSeleccionadoEnListaFiltrada();
    }

    /// <summary>
    /// Si el cliente elegido no entra por el Take o por el texto, se antepone usando la referencia canonical de <see cref="Clientes"/>.
    /// </summary>
    private void AsegurarClienteSeleccionadoEnListaFiltrada()
    {
        if (ClienteSeleccionado == null || Clientes.Count == 0)
            return;

        var canon = Clientes.FirstOrDefault(c => c.Id == ClienteSeleccionado.Id);
        if (canon == null)
            return;

        if (!ClientesFiltradosFormulario.Any(c => c.Id == canon.Id))
            ClientesFiltradosFormulario.Insert(0, canon);

        if (!ReferenceEquals(ClienteSeleccionado, canon))
            ClienteSeleccionado = canon;
    }

    /// <summary>
    /// Carga las imágenes de las fotos del trabajo para mostrarlas en la UI.
    /// </summary>
    private void CargarFotosTrabajo(Trabajo trabajo)
    {
        FotoAntesImagen = CargarBitmapDesdeRuta(trabajo.FotoAntesPath);
        FotoDespuesImagen = CargarBitmapDesdeRuta(trabajo.FotoDespuesPath);
    }

    /// <summary>
    /// Refresca las fotos del trabajo seleccionado desde la base de datos.
    /// </summary>
    private async Task RefrescarFotosTrabajoAsync()
    {
        if (TrabajoSeleccionado == null) return;

        try
        {
            await _db.Entry(TrabajoSeleccionado).ReloadAsync();
            CargarFotosTrabajo(TrabajoSeleccionado);
            Log.Debug("Fotos del trabajo {TrabajoId} refrescadas", TrabajoSeleccionado.Id);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al refrescar fotos del trabajo {TrabajoId}", TrabajoSeleccionado.Id);
        }
    }

    /// <summary>
    /// Carga un Bitmap desde una ruta de archivo.
    /// </summary>
    private static Bitmap? CargarBitmapDesdeRuta(string? ruta)
    {
        if (string.IsNullOrWhiteSpace(ruta) || !File.Exists(ruta))
            return null;

        try
        {
            using var stream = File.OpenRead(ruta);
            return new Bitmap(stream);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error al cargar bitmap desde ruta: {Ruta}", ruta);
            return null;
        }
    }

    /// <summary>
    /// Abre la foto "antes" en el visor de imágenes del sistema.
    /// </summary>
    [RelayCommand]
    private Task VerFotoAntes(Trabajo? trabajo)
    {
        var trabajoAVer = trabajo ?? TrabajoSeleccionado;
        if (trabajoAVer == null || string.IsNullOrWhiteSpace(trabajoAVer.FotoAntesPath))
            return Task.CompletedTask;

        try
        {
            if (File.Exists(trabajoAVer.FotoAntesPath))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = trabajoAVer.FotoAntesPath,
                    UseShellExecute = true
                });
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al abrir foto antes");
            MensajeError = $"Error al abrir la foto: {ex.Message}";
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Abre la foto "después" en el visor de imágenes del sistema.
    /// </summary>
    [RelayCommand]
    private Task VerFotoDespues(Trabajo? trabajo)
    {
        var trabajoAVer = trabajo ?? TrabajoSeleccionado;
        if (trabajoAVer == null || string.IsNullOrWhiteSpace(trabajoAVer.FotoDespuesPath))
            return Task.CompletedTask;

        try
        {
            if (File.Exists(trabajoAVer.FotoDespuesPath))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = trabajoAVer.FotoDespuesPath,
                    UseShellExecute = true
                });
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al abrir foto después");
            MensajeError = $"Error al abrir la foto: {ex.Message}";
        }

        return Task.CompletedTask;
    }

    #endregion
}

