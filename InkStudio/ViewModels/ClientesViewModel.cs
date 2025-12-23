using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
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
    /// Indica si la ficha del cliente está visible.
    /// </summary>
    [ObservableProperty]
    private bool _mostrarFicha = false;

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

    /// <summary>
    /// Lista de trabajos del cliente seleccionado (para la ficha).
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<Trabajo> _trabajosCliente = new();

    /// <summary>
    /// Lista de consentimientos del cliente seleccionado (para la ficha).
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<Consentimiento> _consentimientosCliente = new();

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

    #region Métodos de ayuda para ficha de cliente

    /// <summary>
    /// Refresca la ficha del cliente actualmente seleccionada, si coincide con el ID indicado.
    /// </summary>
    private async Task RefrescarFichaClientePorIdAsync(int clienteId)
    {
        if (!MostrarFicha || ClienteSeleccionado == null) 
            return;

        if (ClienteSeleccionado.Id != clienteId)
            return;

        // Buscar el cliente en la lista actual para trabajar siempre con la instancia actual
        var clienteEnLista = Clientes.FirstOrDefault(c => c.Id == clienteId);
        if (clienteEnLista == null)
            return;

        await VerFichaCliente(clienteEnLista);
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
    /// Abre la ficha del cliente seleccionado.
    /// </summary>
    [RelayCommand]
    private async Task VerFichaCliente(Cliente? cliente = null)
    {
        var clienteAVer = cliente ?? ClienteSeleccionado;
        if (clienteAVer == null) return;

        try
        {
            ClienteSeleccionado = clienteAVer;
            
            // Recargar cliente con relaciones
            await _db.Entry(clienteAVer).ReloadAsync();
            await _db.Entry(clienteAVer).Collection(c => c.Trabajos).LoadAsync();

            // Cargar consentimientos incluyendo el trabajo asociado (para mostrar nombres más descriptivos)
            await _db.Entry(clienteAVer)
                .Collection(c => c.Consentimientos)
                .Query()
                .Include(ct => ct.Trabajo)
                .LoadAsync();

            // Cargar trabajos y consentimientos
            TrabajosCliente = new ObservableCollection<Trabajo>(
                clienteAVer.Trabajos.OrderByDescending(t => t.Fecha));
            
            ConsentimientosCliente = new ObservableCollection<Consentimiento>(
                clienteAVer.Consentimientos.OrderByDescending(c => c.FechaFirma));

            MostrarFicha = true;
            MostrarFormulario = false;
            
            Log.Information("Ficha del cliente abierta: {ClienteId}", clienteAVer.Id);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al abrir ficha del cliente {ClienteId}", clienteAVer?.Id);
            MensajeError = $"Error al cargar la ficha: {ex.Message}";
        }
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
        MostrarFicha = false;
    }

    /// <summary>
    /// Cierra la ficha del cliente.
    /// </summary>
    [RelayCommand]
    private void CerrarFicha()
    {
        MostrarFicha = false;
        ClienteSeleccionado = null;
        TrabajosCliente.Clear();
        ConsentimientosCliente.Clear();
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
                        ConsentimientoFirmaVM.FirmaCompletada += async (s, cliente) =>
                        {
                            await CargarClientes();
                            await RefrescarFichaClientePorIdAsync(cliente.Id);
                        };
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

            // Si la ficha de este cliente está abierta, refrescarla para reflejar cambios y consentimientos
            await RefrescarFichaClientePorIdAsync(clienteGuardado.Id);
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
        MostrarFicha = false;
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
                    await RefrescarFichaClientePorIdAsync(clienteCompletado.Id);
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

    /// <summary>
    /// Obtiene el consentimiento RGPD de un cliente.
    /// </summary>
    /// <param name="cliente">Cliente del que obtener el consentimiento.</param>
    /// <returns>Consentimiento RGPD si existe, null en caso contrario.</returns>
    public async Task<Consentimiento?> ObtenerConsentimientoRGPD(Cliente cliente)
    {
        try
        {
            var consentimiento = await _db.Consentimientos
                .FirstOrDefaultAsync(c => c.ClienteId == cliente.Id && 
                                          c.Tipo == TipoConsentimiento.RGPD && 
                                          c.Firmado);
            
            return consentimiento;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al obtener consentimiento RGPD del cliente {ClienteId}", cliente.Id);
            return null;
        }
    }

    /// <summary>
    /// Abre el PDF de un consentimiento específico.
    /// </summary>
    [RelayCommand]
    private Task AbrirConsentimiento(Consentimiento? consentimiento)
    {
        if (consentimiento == null || ClienteSeleccionado == null) return Task.CompletedTask;

        try
        {
            if (string.IsNullOrEmpty(consentimiento.RutaDocumento))
            {
                MensajeError = "No se encontró el documento PDF para este consentimiento.";
                return Task.CompletedTask;
            }

            if (!File.Exists(consentimiento.RutaDocumento))
            {
                MensajeError = $"El archivo PDF no existe en la ruta: {consentimiento.RutaDocumento}";
                Log.Warning("PDF no encontrado: {Ruta}", consentimiento.RutaDocumento);
                return Task.CompletedTask;
            }

            // Abrir el PDF con el visor predeterminado del sistema
            Process.Start(new ProcessStartInfo
            {
                FileName = consentimiento.RutaDocumento,
                UseShellExecute = true
            });

            Log.Information("PDF abierto: {Ruta}", consentimiento.RutaDocumento);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al abrir consentimiento {ConsentimientoId}", consentimiento.Id);
            MensajeError = $"Error al abrir el PDF: {ex.Message}";
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Exporta el PDF de un consentimiento a la carpeta de Descargas.
    /// </summary>
    [RelayCommand]
    private Task ExportarConsentimiento(Consentimiento? consentimiento)
    {
        if (consentimiento == null || ClienteSeleccionado == null) return Task.CompletedTask;

        try
        {
            if (string.IsNullOrEmpty(consentimiento.RutaDocumento))
            {
                MensajeError = "No se encontró el documento PDF para este consentimiento.";
                return Task.CompletedTask;
            }

            if (!File.Exists(consentimiento.RutaDocumento))
            {
                MensajeError = $"El archivo PDF no existe en la ruta: {consentimiento.RutaDocumento}";
                return Task.CompletedTask;
            }

            // Copiar a la carpeta de Descargas
            var descargas = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            var nombreArchivo = Path.GetFileName(consentimiento.RutaDocumento);
            var rutaDestino = Path.Combine(descargas, nombreArchivo);

            // Si el archivo ya existe, agregar un número
            var contador = 1;
            var nombreBase = Path.GetFileNameWithoutExtension(nombreArchivo);
            var extension = Path.GetExtension(nombreArchivo);
            while (File.Exists(rutaDestino))
            {
                nombreArchivo = $"{nombreBase}_{contador}{extension}";
                rutaDestino = Path.Combine(descargas, nombreArchivo);
                contador++;
            }

            File.Copy(consentimiento.RutaDocumento, rutaDestino);
            
            Log.Information("PDF exportado a: {Ruta}", rutaDestino);
            MensajeError = string.Empty;
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al exportar consentimiento {ConsentimientoId}", consentimiento.Id);
            MensajeError = $"Error al exportar el PDF: {ex.Message}";
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Envía el PDF de un consentimiento por correo electrónico.
    /// </summary>
    [RelayCommand]
    private Task EnviarConsentimientoPorCorreo(Consentimiento? consentimiento)
    {
        if (consentimiento == null || ClienteSeleccionado == null) return Task.CompletedTask;

        try
        {
            if (string.IsNullOrEmpty(consentimiento.RutaDocumento))
            {
                MensajeError = "No se encontró el documento PDF para este consentimiento.";
                return Task.CompletedTask;
            }

            if (!File.Exists(consentimiento.RutaDocumento))
            {
                MensajeError = $"El archivo PDF no existe en la ruta: {consentimiento.RutaDocumento}";
                return Task.CompletedTask;
            }

            if (string.IsNullOrEmpty(ClienteSeleccionado.Email))
            {
                MensajeError = "El cliente no tiene un email registrado. Por favor, añade un email al cliente primero.";
                return Task.CompletedTask;
            }

            // Abrir el cliente de correo predeterminado
            Process.Start(new ProcessStartInfo
            {
                FileName = $"mailto:{ClienteSeleccionado.Email}?subject=Consentimiento%20{consentimiento.NombreTipo}&body=Adjunto%20el%20consentimiento%20firmado.",
                UseShellExecute = true
            });

            // También abrimos el PDF para que el usuario pueda adjuntarlo
            Process.Start(new ProcessStartInfo
            {
                FileName = consentimiento.RutaDocumento,
                UseShellExecute = true
            });

            Log.Information("Cliente de correo abierto para enviar consentimiento {Tipo} a {Email}", 
                consentimiento.Tipo, ClienteSeleccionado.Email);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al abrir cliente de correo para consentimiento {ConsentimientoId}", consentimiento.Id);
            MensajeError = $"Error al abrir el cliente de correo: {ex.Message}";
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Abre el PDF del consentimiento RGPD del cliente seleccionado.
    /// </summary>
    [RelayCommand]
    private async Task AbrirConsentimientoRGPD(Cliente? cliente)
    {
        if (cliente == null) return;

        try
        {
            var consentimiento = await ObtenerConsentimientoRGPD(cliente);
            
            if (consentimiento == null || string.IsNullOrEmpty(consentimiento.RutaDocumento))
            {
                MensajeError = "No se encontró el consentimiento RGPD firmado para este cliente.";
                return;
            }

            if (!File.Exists(consentimiento.RutaDocumento))
            {
                MensajeError = $"El archivo PDF no existe en la ruta: {consentimiento.RutaDocumento}";
                Log.Warning("PDF no encontrado: {Ruta}", consentimiento.RutaDocumento);
                return;
            }

            // Abrir el PDF con el visor predeterminado del sistema
            Process.Start(new ProcessStartInfo
            {
                FileName = consentimiento.RutaDocumento,
                UseShellExecute = true
            });

            Log.Information("PDF abierto: {Ruta}", consentimiento.RutaDocumento);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al abrir consentimiento RGPD del cliente {ClienteId}", cliente.Id);
            MensajeError = $"Error al abrir el PDF: {ex.Message}";
        }
    }

    /// <summary>
    /// Exporta el PDF del consentimiento RGPD a una ubicación elegida por el usuario.
    /// </summary>
    [RelayCommand]
    private async Task ExportarConsentimientoRGPD(Cliente? cliente)
    {
        if (cliente == null) return;

        try
        {
            var consentimiento = await ObtenerConsentimientoRGPD(cliente);
            
            if (consentimiento == null || string.IsNullOrEmpty(consentimiento.RutaDocumento))
            {
                MensajeError = "No se encontró el consentimiento RGPD firmado para este cliente.";
                return;
            }

            if (!File.Exists(consentimiento.RutaDocumento))
            {
                MensajeError = $"El archivo PDF no existe en la ruta: {consentimiento.RutaDocumento}";
                return;
            }

            // Usar SaveFileDialog de Avalonia (necesitamos acceso a la ventana principal)
            // Por ahora, copiamos a la carpeta de Descargas
            var descargas = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            var nombreArchivo = Path.GetFileName(consentimiento.RutaDocumento);
            var rutaDestino = Path.Combine(descargas, nombreArchivo);

            // Si el archivo ya existe, agregar un número
            var contador = 1;
            var nombreBase = Path.GetFileNameWithoutExtension(nombreArchivo);
            var extension = Path.GetExtension(nombreArchivo);
            while (File.Exists(rutaDestino))
            {
                nombreArchivo = $"{nombreBase}_{contador}{extension}";
                rutaDestino = Path.Combine(descargas, nombreArchivo);
                contador++;
            }

            File.Copy(consentimiento.RutaDocumento, rutaDestino);
            
            Log.Information("PDF exportado a: {Ruta}", rutaDestino);
            MensajeError = string.Empty; // Limpiar mensajes de error previos
            // TODO: Mostrar mensaje de éxito (necesitamos un sistema de notificaciones)
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al exportar consentimiento RGPD del cliente {ClienteId}", cliente.Id);
            MensajeError = $"Error al exportar el PDF: {ex.Message}";
        }
    }

    /// <summary>
    /// Envía el PDF del consentimiento RGPD por correo electrónico.
    /// </summary>
    [RelayCommand]
    private async Task EnviarConsentimientoRGPDPorCorreo(Cliente? cliente)
    {
        if (cliente == null) return;

        try
        {
            var consentimiento = await ObtenerConsentimientoRGPD(cliente);
            
            if (consentimiento == null || string.IsNullOrEmpty(consentimiento.RutaDocumento))
            {
                MensajeError = "No se encontró el consentimiento RGPD firmado para este cliente.";
                return;
            }

            if (!File.Exists(consentimiento.RutaDocumento))
            {
                MensajeError = $"El archivo PDF no existe en la ruta: {consentimiento.RutaDocumento}";
                return;
            }

            if (string.IsNullOrEmpty(cliente.Email))
            {
                MensajeError = "El cliente no tiene un email registrado. Por favor, añade un email al cliente primero.";
                return;
            }

            // Abrir el cliente de correo predeterminado con el PDF adjunto
            // mailto: no soporta adjuntos directamente, así que usamos el protocolo file://
            // La mejor opción es abrir el cliente de correo con el PDF ya seleccionado
            // Por ahora, abrimos el PDF y el usuario puede adjuntarlo manualmente
            // TODO: Implementar envío automático cuando tengamos configuración SMTP
            
            Process.Start(new ProcessStartInfo
            {
                FileName = $"mailto:{cliente.Email}?subject=Consentimiento%20RGPD&body=Adjunto%20el%20consentimiento%20RGPD%20firmado.",
                UseShellExecute = true
            });

            // También abrimos el PDF para que el usuario pueda adjuntarlo
            Process.Start(new ProcessStartInfo
            {
                FileName = consentimiento.RutaDocumento,
                UseShellExecute = true
            });

            Log.Information("Cliente de correo abierto para enviar consentimiento RGPD a {Email}", cliente.Email);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al abrir cliente de correo para cliente {ClienteId}", cliente.Id);
            MensajeError = $"Error al abrir el cliente de correo: {ex.Message}";
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

