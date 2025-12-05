using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using InkStudio.Data;
using InkStudio.Models;

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
    private DateTime? _fechaNacimiento;

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
            Cargando = true;
            MensajeError = string.Empty;

            var lista = await _db.Clientes
                .Where(c => c.Activo)
                .OrderBy(c => c.Nombre)
                .ThenBy(c => c.Apellidos)
                .ToListAsync();

            Clientes = new ObservableCollection<Cliente>(lista);
            TotalClientes = lista.Count;
        }
        catch (Exception ex)
        {
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
        }
        catch (Exception ex)
        {
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
                ClienteSeleccionado.Nombre = Nombre.Trim();
                ClienteSeleccionado.Apellidos = Apellidos.Trim();
                ClienteSeleccionado.Telefono = Telefono.Trim();
                ClienteSeleccionado.Email = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim();
                ClienteSeleccionado.FechaNacimiento = FechaNacimiento;
                ClienteSeleccionado.Alergias = string.IsNullOrWhiteSpace(Alergias) ? null : Alergias.Trim();
                ClienteSeleccionado.Notas = string.IsNullOrWhiteSpace(Notas) ? null : Notas.Trim();
                ClienteSeleccionado.EsVip = EsVip;
            }
            else
            {
                // Crear nuevo cliente
                var nuevoCliente = new Cliente
                {
                    Nombre = Nombre.Trim(),
                    Apellidos = Apellidos.Trim(),
                    Telefono = Telefono.Trim(),
                    Email = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim(),
                    FechaNacimiento = FechaNacimiento,
                    Alergias = string.IsNullOrWhiteSpace(Alergias) ? null : Alergias.Trim(),
                    Notas = string.IsNullOrWhiteSpace(Notas) ? null : Notas.Trim(),
                    EsVip = EsVip,
                    FechaRegistro = DateTime.Now,
                    Activo = true
                };
                _db.Clientes.Add(nuevoCliente);
            }

            await _db.SaveChangesAsync();
            
            MostrarFormulario = false;
            await CargarClientes();
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UNIQUE") == true)
        {
            MensajeError = "Ya existe un cliente con ese teléfono";
        }
        catch (Exception ex)
        {
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
            Cargando = true;
            
            // Soft delete (solo desactivamos)
            ClienteSeleccionado.Activo = false;
            await _db.SaveChangesAsync();
            
            MostrarFormulario = false;
            await CargarClientes();
        }
        catch (Exception ex)
        {
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
        FechaNacimiento = null;
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
        FechaNacimiento = cliente.FechaNacimiento;
        Alergias = cliente.Alergias ?? string.Empty;
        Notas = cliente.Notas ?? string.Empty;
        EsVip = cliente.EsVip;
        MensajeError = string.Empty;
    }

    #endregion
}

