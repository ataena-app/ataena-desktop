using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using InkStudio.Data;
using InkStudio.Models;
using InkStudio.Services;
using Serilog;

namespace InkStudio.ViewModels;

/// <summary>
/// ViewModel para la gestión de clientes.
/// Implementa operaciones CRUD y búsqueda.
/// </summary>
public partial class ClientesViewModel : ViewModelBase
{
    private readonly InkStudioDbContext _db = new();

    #region Propiedades - Lista y Selección

    /// <summary>
    /// Colección observable de clientes mostrados en la lista.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<Cliente> _clientes = new();

    /// <summary>
    /// Cliente actualmente seleccionado en la lista.
    /// </summary>
    [ObservableProperty]
    private Cliente? _clienteSeleccionado;

    /// <summary>
    /// Texto de búsqueda para filtrar clientes.
    /// </summary>
    [ObservableProperty]
    private string _textoBusqueda = string.Empty;

    #endregion

    #region Propiedades - Formulario de Edición

    /// <summary>
    /// Indica si el panel de edición/creación está visible.
    /// </summary>
    [ObservableProperty]
    private bool _mostrarFormulario = false;

    /// <summary>
    /// Indica si estamos editando (true) o creando (false) un cliente.
    /// </summary>
    [ObservableProperty]
    private bool _esEdicion = false;

    /// <summary>
    /// Título del formulario (cambia según modo edición/creación).
    /// </summary>
    [ObservableProperty]
    private string _tituloFormulario = "Nuevo Cliente";

    // Campos del formulario
    [ObservableProperty]
    private string _nombre = string.Empty;

    [ObservableProperty]
    private string _apellidos = string.Empty;

    [ObservableProperty]
    private string _telefono = string.Empty;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _dni = string.Empty;

    [ObservableProperty]
    private DateTimeOffset? _fechaNacimiento;

    [ObservableProperty]
    private string _alergias = string.Empty;

    [ObservableProperty]
    private string _notas = string.Empty;

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
    /// Total de clientes (para mostrar en UI).
    /// </summary>
    [ObservableProperty]
    private int _totalClientes;

    /// <summary>
    /// Indica si se desea solicitar consentimiento de imágenes (opcional).
    /// </summary>
    [ObservableProperty]
    private bool _solicitarConsentimientoImagenes = false;

    /// <summary>
    /// Indica si se desea firmar el consentimiento RGPD al crear el cliente.
    /// </summary>
    [ObservableProperty]
    private bool _firmarConsentimientoRGPD = false;

    /// <summary>
    /// ViewModel del modal de firma de consentimientos.
    /// </summary>
    [ObservableProperty]
    private ConsentimientoFirmaViewModel? _consentimientoFirmaVM;

    #endregion

    #region Comandos - Carga de Datos

    /// <summary>
    /// Carga todos los clientes desde la base de datos.
    /// </summary>
    [RelayCommand]
    private async Task CargarClientes()
    {
        try
        {
            Log.Debug("Cargando lista de clientes");
            Cargando = true;
            MensajeError = string.Empty;

            var lista = await _db.Clientes
                .Include(c => c.Consentimientos)
                .OrderBy(c => c.Nombre)
                .ThenBy(c => c.Apellidos)
                .ToListAsync();

            Clientes = new ObservableCollection<Cliente>(lista);
            TotalClientes = lista.Count;
            Log.Information("Clientes cargados: {Count} clientes activos", lista.Count);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al cargar clientes desde la base de datos");
            MensajeError = $"Error al cargar clientes: {ex.Message}";
        }
        finally
        {
            Cargando = false;
        }
    }

    /// <summary>
    /// Busca clientes por nombre, apellidos o teléfono.
    /// </summary>
    [RelayCommand]
    private async Task Buscar()
    {
        try
        {
            Cargando = true;
            MensajeError = string.Empty;

            if (string.IsNullOrWhiteSpace(TextoBusqueda))
            {
                await CargarClientes();
                return;
            }

            Log.Debug("Buscando clientes con término: {Termino}", TextoBusqueda);
            var busqueda = TextoBusqueda.ToLower();
            var lista = await _db.Clientes
                .Where(c =>
                    c.Nombre.ToLower().Contains(busqueda) ||
                    c.Apellidos.ToLower().Contains(busqueda) ||
                    c.Telefono.Contains(busqueda) ||
                    (c.Email != null && c.Email.ToLower().Contains(busqueda))
                )
                .OrderBy(c => c.Nombre)
                .ToListAsync();

            Clientes = new ObservableCollection<Cliente>(lista);
            TotalClientes = lista.Count;
            Log.Information("Búsqueda completada: {Count} resultados para '{Termino}'", lista.Count, TextoBusqueda);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al buscar clientes con término: {Termino}", TextoBusqueda);
            MensajeError = $"Error en la búsqueda: {ex.Message}";
        }
        finally
        {
            Cargando = false;
        }
    }

    #endregion

    #region Comandos - CRUD

    /// <summary>
    /// Abre el formulario para crear un nuevo cliente.
    /// </summary>
    [RelayCommand]
    private void NuevoCliente()
    {
        LimpiarFormulario();
        EsEdicion = false;
        TituloFormulario = "✨ Nuevo Cliente";
        MostrarFormulario = true;
    }

    /// <summary>
    /// Abre el formulario para editar el cliente seleccionado.
    /// </summary>
    [RelayCommand]
    private void EditarCliente()
    {
        if (ClienteSeleccionado == null) return;

        CargarClienteEnFormulario(ClienteSeleccionado);
        EsEdicion = true;
        TituloFormulario = "✏️ Editar Cliente";
        MostrarFormulario = true;
    }

    /// <summary>
    /// Guarda el cliente (crea nuevo o actualiza existente).
    /// </summary>
    [RelayCommand]
    private async Task GuardarCliente()
    {
        try
        {
            // Validación básica
            if (string.IsNullOrWhiteSpace(Nombre))
            {
                MensajeError = "El nombre es obligatorio";
                return;
            }
            if (string.IsNullOrWhiteSpace(Telefono))
            {
                MensajeError = "El teléfono es obligatorio";
                return;
            }

            // Validar teléfono único (solo para clientes activos)
            var telefonoLimpio = Telefono.Trim();
            if (!EsEdicion)
            {
                var existeTelefono = await _db.Clientes
                    .AnyAsync(c => c.Telefono == telefonoLimpio && c.Activo);
                
                if (existeTelefono)
                {
                    MensajeError = "Ya existe un cliente activo con ese teléfono";
                    return;
                }
            }
            else if (ClienteSeleccionado != null && ClienteSeleccionado.Telefono != telefonoLimpio)
            {
                // Si está editando y cambió el teléfono, verificar que el nuevo no exista
                var existeTelefono = await _db.Clientes
                    .AnyAsync(c => c.Telefono == telefonoLimpio && c.Activo && c.Id != ClienteSeleccionado.Id);
                
                if (existeTelefono)
                {
                    MensajeError = "Ya existe un cliente activo con ese teléfono";
                    return;
                }
            }

            Cargando = true;
            MensajeError = string.Empty;

            Cliente clienteGuardado;

            if (EsEdicion && ClienteSeleccionado != null)
            {
                // Actualizar cliente existente
                Log.Information("Actualizando cliente ID: {ClienteId}, Nombre: {Nombre}, FechaNacimiento: {FechaNacimiento}", 
                    ClienteSeleccionado.Id, Nombre, FechaNacimiento);
                ClienteSeleccionado.Nombre = Nombre.Trim();
                ClienteSeleccionado.Apellidos = Apellidos.Trim();
                ClienteSeleccionado.Telefono = Telefono.Trim();
                ClienteSeleccionado.Email = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim();
                ClienteSeleccionado.Dni = string.IsNullOrWhiteSpace(Dni) ? null : Dni.Trim();
                ClienteSeleccionado.FechaNacimiento = FechaNacimiento?.DateTime;
                ClienteSeleccionado.Alergias = string.IsNullOrWhiteSpace(Alergias) ? null : Alergias.Trim();
                ClienteSeleccionado.Notas = string.IsNullOrWhiteSpace(Notas) ? null : Notas.Trim();
                clienteGuardado = ClienteSeleccionado;
            }
            else
            {
                // Crear nuevo cliente
                Log.Information("Creando nuevo cliente: {Nombre} {Apellidos}, Tel: {Telefono}", 
                    Nombre, Apellidos, Telefono);
                var nuevoCliente = new Cliente
                {
                    Nombre = Nombre.Trim(),
                    Apellidos = Apellidos.Trim(),
                    Telefono = Telefono.Trim(),
                    Email = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim(),
                    Dni = string.IsNullOrWhiteSpace(Dni) ? null : Dni.Trim(),
                    FechaNacimiento = FechaNacimiento?.DateTime,
                    Alergias = string.IsNullOrWhiteSpace(Alergias) ? null : Alergias.Trim(),
                    Notas = string.IsNullOrWhiteSpace(Notas) ? null : Notas.Trim(),
                    FechaRegistro = DateTime.Now,
                    Activo = true
                };
                _db.Clientes.Add(nuevoCliente);
                clienteGuardado = nuevoCliente;
            }

            await _db.SaveChangesAsync();
            Log.Information("Cliente guardado exitosamente");

            // Para nuevos clientes, verificar y solicitar consentimientos
            if (!EsEdicion)
            {
                // Recargar cliente desde BD para obtener el ID correcto
                await _db.Entry(clienteGuardado).ReloadAsync();
                
                // Verificar si ya tiene RGPD (no debería tenerlo si es nuevo)
                var tieneRGPD = await ConsentimientoService.ValidarConsentimientosRequeridos(clienteGuardado.Id);
                
                if (!tieneRGPD)
                {
                    // Cerrar formulario de cliente
                    MostrarFormulario = false;
                    
                    // Abrir modal de firma RGPD
                    if (ConsentimientoFirmaVM == null)
                    {
                        ConsentimientoFirmaVM = new ConsentimientoFirmaViewModel();
                        ConsentimientoFirmaVM.FirmaCompletada += async (s, cliente) => await CargarClientes();
                    }
                    await ConsentimientoFirmaVM.AbrirModal(clienteGuardado, TipoConsentimiento.RGPD);
                    
                    // El modal se cierra automáticamente cuando se confirma la firma
                }
                else if (SolicitarConsentimientoImagenes)
                {
                    // Si marcó el checkbox de imágenes, abrir modal de imágenes
                    MostrarFormulario = false;
                    if (ConsentimientoFirmaVM == null)
                    {
                        ConsentimientoFirmaVM = new ConsentimientoFirmaViewModel();
                    }
                    await ConsentimientoFirmaVM.AbrirModal(clienteGuardado, TipoConsentimiento.Imagenes);
                }
                else
                {
                    // Si ya tiene RGPD y no quiere imágenes, solo cerrar
                    MostrarFormulario = false;
                }
            }
            else
            {
                // Para edición, solo cerrar
                MostrarFormulario = false;
            }

            await CargarClientes();
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UNIQUE") == true || 
                                            ex.Message.Contains("UNIQUE") == true)
        {
            Log.Warning("Intento de crear cliente con teléfono duplicado: {Telefono}. Error: {Error}", 
                Telefono, ex.Message);
            MensajeError = "Ya existe un cliente con ese teléfono.";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al guardar cliente. Nombre: {Nombre}, Tel: {Telefono}", Nombre, Telefono);
            MensajeError = $"Error al guardar: {ex.Message}";
        }
        finally
        {
            Cargando = false;
        }
    }

    /// <summary>
    /// Elimina permanentemente el cliente seleccionado de la base de datos.
    /// También elimina todas sus citas, trabajos y consentimientos relacionados (cascada).
    /// </summary>
    [RelayCommand]
    private async Task EliminarCliente()
    {
        if (ClienteSeleccionado == null) return;

        try
        {
            Log.Warning("Eliminando permanentemente cliente ID: {ClienteId}, Nombre: {Nombre}", 
                ClienteSeleccionado.Id, ClienteSeleccionado.NombreCompleto);
            Cargando = true;
            
            // Eliminar permanentemente de la base de datos
            // Las relaciones en cascada eliminarán automáticamente:
            // - Todas las citas del cliente
            // - Todos los trabajos del cliente
            // - Todos los consentimientos del cliente
            _db.Clientes.Remove(ClienteSeleccionado);
            await _db.SaveChangesAsync();
            
            Log.Information("Cliente eliminado permanentemente: ID {ClienteId}, Nombre: {Nombre}", 
                ClienteSeleccionado.Id, ClienteSeleccionado.NombreCompleto);
            
            MostrarFormulario = false;
            ClienteSeleccionado = null;
            await CargarClientes();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al eliminar cliente ID: {ClienteId}", ClienteSeleccionado?.Id);
            MensajeError = $"Error al eliminar: {ex.Message}";
        }
        finally
        {
            Cargando = false;
        }
    }

    /// <summary>
    /// Elimina permanentemente TODOS los clientes de la base de datos.
    /// ADVERTENCIA: Esta operación es irreversible y eliminará también todas las citas,
    /// trabajos y consentimientos relacionados.
    /// </summary>
    [RelayCommand]
    private async Task EliminarTodosLosClientes()
    {
        try
        {
            Cargando = true;
            MensajeError = string.Empty;

            // Contar clientes antes de eliminar
            var totalClientes = await _db.Clientes.CountAsync();
            
            if (totalClientes == 0)
            {
                MensajeError = "No hay clientes para eliminar";
                Cargando = false;
                return;
            }

            Log.Warning("═══════════════════════════════════════════════════════");
            Log.Warning("ELIMINANDO TODOS LOS CLIENTES DE LA BASE DE DATOS");
            Log.Warning("Total de clientes a eliminar: {Total}", totalClientes);
            Log.Warning("═══════════════════════════════════════════════════════");

            // Eliminar todos los clientes
            // Las relaciones en cascada eliminarán automáticamente:
            // - Todas las citas de todos los clientes
            // - Todos los trabajos de todos los clientes
            // - Todos los consentimientos de todos los clientes
            _db.Clientes.RemoveRange(_db.Clientes);
            await _db.SaveChangesAsync();
            
            Log.Information("═══════════════════════════════════════════════════════");
            Log.Information("TODOS LOS CLIENTES ELIMINADOS PERMANENTEMENTE");
            Log.Information("Total eliminado: {Total} clientes", totalClientes);
            Log.Information("═══════════════════════════════════════════════════════");
            
            MostrarFormulario = false;
            ClienteSeleccionado = null;
            await CargarClientes();
            
            MensajeError = $"✅ Se eliminaron {totalClientes} clientes permanentemente";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al eliminar todos los clientes");
            MensajeError = $"Error al eliminar todos los clientes: {ex.Message}";
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
    /// Abre el modal para firmar el consentimiento RGPD del cliente seleccionado.
    /// </summary>
    [RelayCommand]
    private async Task FirmarConsentimientoCliente(Cliente? cliente = null)
    {
        var clienteAFirmar = cliente ?? ClienteSeleccionado;
        if (clienteAFirmar == null) return;

        try
        {
            // Recargar cliente con consentimientos para verificar estado actual
            await _db.Entry(clienteAFirmar).ReloadAsync();
            await _db.Entry(clienteAFirmar).Collection(c => c.Consentimientos).LoadAsync();
            
            // Verificar si ya tiene RGPD firmado
            var tieneRGPD = clienteAFirmar.Consentimientos
                .Any(c => c.Tipo == TipoConsentimiento.RGPD && c.Firmado);
            
            if (tieneRGPD)
            {
                MensajeError = "Este cliente ya tiene el consentimiento RGPD firmado";
                await CargarClientes(); // Recargar para actualizar la vista
                return;
            }

            // Cerrar formulario si está abierto
            MostrarFormulario = false;
            
            // Abrir modal de firma RGPD
            if (ConsentimientoFirmaVM == null)
            {
                ConsentimientoFirmaVM = new ConsentimientoFirmaViewModel();
                ConsentimientoFirmaVM.FirmaCompletada += async (s, clienteCompletado) => 
                {
                    await CargarClientes(); // Recargar lista después de firmar
                };
            }
            await ConsentimientoFirmaVM.AbrirModal(clienteAFirmar, TipoConsentimiento.RGPD);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al abrir modal de firma de consentimiento");
            MensajeError = $"Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Filtra la lista para mostrar solo clientes sin consentimiento RGPD firmado.
    /// </summary>
    [RelayCommand]
    private async Task FiltrarSinConsentimiento()
    {
        try
        {
            Log.Debug("Filtrando clientes sin consentimiento RGPD");
            Cargando = true;
            MensajeError = string.Empty;

            // Cargar todos los clientes con sus consentimientos
            var todosClientes = await _db.Clientes
                .Include(c => c.Consentimientos)
                .OrderBy(c => c.Nombre)
                .ThenBy(c => c.Apellidos)
                .ToListAsync();

            // Filtrar solo los que no tienen RGPD firmado
            var clientesSinRGPD = todosClientes
                .Where(c => !c.Consentimientos.Any(cons => cons.Tipo == TipoConsentimiento.RGPD && cons.Firmado))
                .ToList();

            Clientes = new ObservableCollection<Cliente>(clientesSinRGPD);
            TotalClientes = clientesSinRGPD.Count;
            
            Log.Information("Filtrado completado: {Count} clientes sin consentimiento RGPD", clientesSinRGPD.Count);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al filtrar clientes sin consentimiento");
            MensajeError = $"Error en el filtrado: {ex.Message}";
        }
        finally
        {
            Cargando = false;
        }
    }

    #endregion

    #region Métodos Privados

    /// <summary>
    /// Limpia todos los campos del formulario.
    /// </summary>
    private void LimpiarFormulario()
    {
        Nombre = string.Empty;
        Apellidos = string.Empty;
        Telefono = string.Empty;
        Email = string.Empty;
        Dni = string.Empty;
        FechaNacimiento = DateTimeOffset.Now.Date;
        Alergias = string.Empty;
        Notas = string.Empty;
        MensajeError = string.Empty;
        SolicitarConsentimientoImagenes = false;
        FirmarConsentimientoRGPD = false;
    }

    /// <summary>
    /// Carga los datos de un cliente en el formulario.
    /// </summary>
    /// <param name="cliente">Cliente a cargar.</param>
    private void CargarClienteEnFormulario(Cliente cliente)
    {
        Nombre = cliente.Nombre;
        Apellidos = cliente.Apellidos;
        Telefono = cliente.Telefono;
        Email = cliente.Email ?? string.Empty;
        Dni = cliente.Dni ?? string.Empty;
        FechaNacimiento = cliente.FechaNacimiento.HasValue 
            ? new DateTimeOffset(cliente.FechaNacimiento.Value) 
            : DateTimeOffset.Now.Date;
        Alergias = cliente.Alergias ?? string.Empty;
        Notas = cliente.Notas ?? string.Empty;
        MensajeError = string.Empty;
    }

    #endregion
}

