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

    [ObservableProperty]
    private bool _esVip = false;

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

    #endregion

    #region Comandos - Carga de Datos

    /// <summary>
    /// Carga todos los clientes activos desde la base de datos.
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
                .Where(c => c.Activo)
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
                .Where(c => c.Activo && (
                    c.Nombre.ToLower().Contains(busqueda) ||
                    c.Apellidos.ToLower().Contains(busqueda) ||
                    c.Telefono.Contains(busqueda) ||
                    (c.Email != null && c.Email.ToLower().Contains(busqueda))
                ))
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

            Cargando = true;
            MensajeError = string.Empty;

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
                ClienteSeleccionado.EsVip = EsVip;
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
                    EsVip = EsVip,
                    FechaRegistro = DateTime.Now,
                    Activo = true
                };
                _db.Clientes.Add(nuevoCliente);
            }

            await _db.SaveChangesAsync();
            Log.Information("Cliente guardado exitosamente");
            
            MostrarFormulario = false;
            await CargarClientes();
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UNIQUE") == true)
        {
            Log.Warning("Intento de crear cliente con teléfono duplicado: {Telefono}", Telefono);
            MensajeError = "Ya existe un cliente con ese teléfono";
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
    /// Elimina (desactiva) el cliente seleccionado.
    /// </summary>
    [RelayCommand]
    private async Task EliminarCliente()
    {
        if (ClienteSeleccionado == null) return;

        try
        {
            Log.Warning("Eliminando (desactivando) cliente ID: {ClienteId}, Nombre: {Nombre}", 
                ClienteSeleccionado.Id, ClienteSeleccionado.NombreCompleto);
            Cargando = true;
            
            // Soft delete (solo desactivamos)
            ClienteSeleccionado.Activo = false;
            await _db.SaveChangesAsync();
            
            Log.Information("Cliente eliminado exitosamente: ID {ClienteId}", ClienteSeleccionado.Id);
            MostrarFormulario = false;
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
    /// Cancela la edición y cierra el formulario.
    /// </summary>
    [RelayCommand]
    private void CancelarEdicion()
    {
        MostrarFormulario = false;
        MensajeError = string.Empty;
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
        EsVip = false;
        MensajeError = string.Empty;
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
        EsVip = cliente.EsVip;
        MensajeError = string.Empty;
    }

    #endregion
}

