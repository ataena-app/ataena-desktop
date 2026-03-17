using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
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
/// ViewModel para la gestión de clientes.
/// Implementa operaciones CRUD y búsqueda.
/// </summary>
public partial class ClientesViewModel : ViewModelBase
{
    private readonly AtaenaDbContext _db = new();
    private TrabajosViewModel? _trabajosVM;
    private MainWindowViewModel? _mainWindowVM;

        /// <summary>
        /// Si se marca RGPD + Imágenes al crear un cliente nuevo,
        /// usamos este campo para recordar que, tras firmar RGPD,
        /// hay que abrir automáticamente el consentimiento de imágenes.
        /// </summary>
        private int? _clientePendienteImagenesDespuesRgpdId;

    /// <summary>
    /// Permite inyectar el ViewModel de Trabajos para navegar a trabajos desde la ficha de cliente.
    /// </summary>
    public void SetTrabajosViewModel(TrabajosViewModel trabajosViewModel)
    {
        _trabajosVM = trabajosViewModel;
    }

    /// <summary>
    /// Permite inyectar el MainWindowViewModel para navegación.
    /// </summary>
    public void SetMainWindowViewModel(MainWindowViewModel mainWindowViewModel)
    {
        _mainWindowVM = mainWindowViewModel;
    }

    #region Propiedades - Lista y Selección

    /// <summary>
    /// Colección completa de clientes (sin paginación).
    /// </summary>
    private ObservableCollection<Cliente> _todosLosClientes = new();

    /// <summary>
    /// Colección observable de clientes mostrados en la página actual.
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

    /// <summary>
    /// Texto de la fecha de nacimiento para entrada por teclado (formato DD/MM/YYYY o DD-MM-YYYY).
    /// Se sincroniza automáticamente con FechaNacimiento.
    /// </summary>
    [ObservableProperty]
    private string _fechaNacimientoTexto = string.Empty;

    /// <summary>
    /// Se ejecuta cuando cambia FechaNacimientoTexto.
    /// Intenta parsear el texto a fecha y actualizar FechaNacimiento.
    /// Esto permite que el DatePicker acepte entrada de texto manual.
    /// </summary>
    partial void OnFechaNacimientoTextoChanged(string value)
    {
        // Esta propiedad se mantiene para compatibilidad, pero el DatePicker maneja la entrada directamente
        // Solo actualizamos si el texto es válido y diferente de lo que ya está en FechaNacimiento
        if (string.IsNullOrWhiteSpace(value))
        {
            if (FechaNacimiento.HasValue)
            {
                _actualizandoFechaDesdeTexto = true;
                FechaNacimiento = null;
                _actualizandoFechaDesdeTexto = false;
            }
            return;
        }

        // Intentar parsear en varios formatos comunes
        var formatos = new[] { "dd/MM/yyyy", "dd-MM-yyyy", "d/M/yyyy", "d-M-yyyy", "dd/MM/yy", "dd-MM-yy" };
        foreach (var formato in formatos)
        {
            if (DateTime.TryParseExact(value.Trim(), formato, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var fecha))
            {
                // Si el año tiene 2 dígitos, asumir 19xx o 20xx según el valor
                if (formato.Contains("yy") && !formato.Contains("yyyy"))
                {
                    if (fecha.Year < 50)
                        fecha = fecha.AddYears(2000);
                    else if (fecha.Year < 100)
                        fecha = fecha.AddYears(1900);
                }

                // Validar año bisiesto: si el día es 29 y el mes es febrero, verificar que el año sea bisiesto
                if (fecha.Day == 29 && fecha.Month == 2)
                {
                    if (!DateTime.IsLeapYear(fecha.Year))
                    {
                        // Año no bisiesto con 29 de febrero: mostrar error pero no bloquear
                        Log.Warning("Fecha inválida: 29 de febrero en año no bisiesto ({Año})", fecha.Year);
                        MensajeError = $"⚠️ El año {fecha.Year} no es bisiesto. El 29 de febrero no existe en ese año.";
                        return; // No actualizar la fecha
                    }
                }

                var nuevaFecha = new DateTimeOffset(fecha);
                // Solo actualizar si es diferente para evitar bucles
                if (!FechaNacimiento.HasValue || FechaNacimiento.Value.Date != nuevaFecha.Date)
                {
                    _actualizandoFechaDesdeTexto = true;
                    FechaNacimiento = nuevaFecha;
                    _actualizandoFechaDesdeTexto = false;
                    MensajeError = string.Empty; // Limpiar error si la fecha es válida
                }
                return;
            }
        }

        // Si no se pudo parsear, intentar parseo flexible
        if (DateTime.TryParse(value.Trim(), System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var fechaFlexible))
        {
            // Validar año bisiesto: si el día es 29 y el mes es febrero, verificar que el año sea bisiesto
            if (fechaFlexible.Day == 29 && fechaFlexible.Month == 2)
            {
                if (!DateTime.IsLeapYear(fechaFlexible.Year))
                {
                    // Año no bisiesto con 29 de febrero: mostrar error pero no bloquear
                    Log.Warning("Fecha inválida: 29 de febrero en año no bisiesto ({Año})", fechaFlexible.Year);
                    MensajeError = $"⚠️ El año {fechaFlexible.Year} no es bisiesto. El 29 de febrero no existe en ese año.";
                    return; // No actualizar la fecha
                }
            }

            var nuevaFecha = new DateTimeOffset(fechaFlexible);
            if (!FechaNacimiento.HasValue || FechaNacimiento.Value.Date != nuevaFecha.Date)
            {
                _actualizandoFechaDesdeTexto = true;
                FechaNacimiento = nuevaFecha;
                _actualizandoFechaDesdeTexto = false;
                MensajeError = string.Empty; // Limpiar error si la fecha es válida
            }
        }
        // Si no se puede parsear, no actualizamos FechaNacimiento (permitimos texto parcial mientras se escribe)
    }

    private bool _actualizandoFechaDesdeTexto = false;

    /// <summary>
    /// Se ejecuta cuando cambia FechaNacimiento.
    /// Actualiza FechaNacimientoTexto para mantener sincronización.
    /// </summary>
    partial void OnFechaNacimientoChanged(DateTimeOffset? value)
    {
        // Notificar cambio en EsMenorFormulario para mostrar/ocultar datos del tutor
        OnPropertyChanged(nameof(EsMenorFormulario));
        
        // Evitar bucle infinito: si estamos actualizando desde el texto, no actualizar el texto
        if (_actualizandoFechaDesdeTexto)
            return;

        if (value.HasValue)
        {
            var texto = value.Value.ToString("dd/MM/yyyy");
            if (FechaNacimientoTexto != texto)
            {
                FechaNacimientoTexto = texto;
            }
        }
        else
        {
            // Solo limpiar si el texto actual no parece ser una fecha válida parcial
            if (!string.IsNullOrWhiteSpace(FechaNacimientoTexto))
            {
                var formatos = new[] { "dd/MM/yyyy", "dd-MM-yyyy", "d/M/yyyy", "d-M-yyyy" };
                bool esFormatoValido = formatos.Any(f => 
                    DateTime.TryParseExact(FechaNacimientoTexto.Trim(), f, 
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None, out _));
                
                // Solo limpiar si no es un formato válido y no parece ser una fecha parcial en escritura
                if (!esFormatoValido && !FechaNacimientoTexto.Contains("/") && !FechaNacimientoTexto.Contains("-"))
                {
                    FechaNacimientoTexto = string.Empty;
                }
            }
        }
    }

    [ObservableProperty]
    private string _alergias = string.Empty;

    [ObservableProperty]
    private string _notas = string.Empty;

    #endregion

    #region Propiedades - Datos del Tutor (para menores)

    [ObservableProperty]
    private string _nombreTutor = string.Empty;

    [ObservableProperty]
    private string _apellidosTutor = string.Empty;

    [ObservableProperty]
    private string _dniTutor = string.Empty;

    [ObservableProperty]
    private string _telefonoTutor = string.Empty;

    /// <summary>
    /// Indica si el cliente del formulario es menor de edad.
    /// Se calcula a partir de la fecha de nacimiento ingresada.
    /// </summary>
    public bool EsMenorFormulario
    {
        get
        {
            if (!FechaNacimiento.HasValue) return false;
            var edad = (int)((DateTime.Today - FechaNacimiento.Value).TotalDays / 365.25);
            return edad < 18;
        }
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
    /// Total de clientes (para mostrar en UI).
    /// </summary>
    [ObservableProperty]
    private int _totalClientes;

    /// <summary>
    /// Página actual (empezando en 1).
    /// </summary>
    [ObservableProperty]
    private int _paginaActual = 1;

    /// <summary>
    /// Tamaño de página (número de clientes por página).
    /// </summary>
    [ObservableProperty]
    private int _tamanoPagina = 10;

    /// <summary>
    /// Total de páginas.
    /// </summary>
    [ObservableProperty]
    private int _totalPaginas = 1;

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
    /// ViewModel del modal para subir fotos de DNI.
    /// </summary>
    [ObservableProperty]
    private FotoDniViewModel? _fotoDniVM;

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

            // AsNoTracking() evita que EF Core use entidades en caché,
            // asegurando datos frescos de la BD después de actualizaciones
            var lista = await _db.Clientes
                .AsNoTracking()
                .Include(c => c.Consentimientos)
                .OrderBy(c => c.Nombre)
                .ThenBy(c => c.Apellidos)
                .ToListAsync();

            _todosLosClientes = new ObservableCollection<Cliente>(lista);
            TotalClientes = lista.Count;
            ActualizarPaginacion();
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
                .AsNoTracking()
                .Include(c => c.Consentimientos)
                .Where(c =>
                    c.Nombre.ToLower().Contains(busqueda) ||
                    c.Apellidos.ToLower().Contains(busqueda) ||
                    c.Telefono.Contains(busqueda) ||
                    (c.Email != null && c.Email.ToLower().Contains(busqueda))
                )
                .OrderBy(c => c.Nombre)
                .ToListAsync();

            _todosLosClientes = new ObservableCollection<Cliente>(lista);
            TotalClientes = lista.Count;
            PaginaActual = 1; // Resetear a primera página
            ActualizarPaginacion();
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
            // Cargar cliente fresco desde BD con todas las relaciones
            // (necesario porque CargarClientes usa AsNoTracking)
            var clienteFresco = await _db.Clientes
                .Include(c => c.Trabajos)
                .Include(c => c.Consentimientos)
                    .ThenInclude(ct => ct.Trabajo)
                .FirstOrDefaultAsync(c => c.Id == clienteAVer.Id);

            if (clienteFresco == null)
            {
                MensajeError = "Cliente no encontrado";
                return;
            }

            ClienteSeleccionado = clienteFresco;

            // Cargar trabajos y consentimientos
            TrabajosCliente = new ObservableCollection<Trabajo>(
                clienteFresco.Trabajos.OrderByDescending(t => t.Fecha));
            
            ConsentimientosCliente = new ObservableCollection<Consentimiento>(
                clienteFresco.Consentimientos.OrderByDescending(c => c.FechaFirma));

            MostrarFicha = true;
            MostrarFormulario = false;
            
            Log.Information("Ficha del cliente abierta: {ClienteId}", clienteFresco.Id);
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
    /// Abre el modal de trabajo desde la ficha de cliente, sin cambiar de vista.
    /// El modal se mostrará por encima del modal de cliente.
    /// </summary>
    [RelayCommand]
    private async Task AbrirTrabajoDesdeFicha(Trabajo? trabajo)
    {
        if (trabajo == null || _trabajosVM == null)
        {
            Log.Warning("No se puede abrir trabajo: trabajo={Trabajo}, trabajosVM={TrabajosVM}", 
                trabajo != null, _trabajosVM != null);
            return;
        }

        try
        {
            // Recargar trabajos para asegurar que tenemos la instancia actualizada
            await _trabajosVM.CargarTrabajosCommand.ExecuteAsync(null);

            // Buscar el trabajo en la lista de trabajos
            var trabajoEnLista = _trabajosVM.Trabajos.FirstOrDefault(t => t.Id == trabajo.Id);
            if (trabajoEnLista == null)
            {
                Log.Warning("No se encontró el trabajo {TrabajoId} en la lista de trabajos", trabajo.Id);
                return;
            }

            // Seleccionar el trabajo y abrir el modal de edición
            // NO navegamos a Trabajos, solo abrimos el modal como overlay
            _trabajosVM.TrabajoSeleccionado = trabajoEnLista;
            await _trabajosVM.EditarTrabajoCommand.ExecuteAsync(null);

            Log.Information("Trabajo {TrabajoId} abierto desde la ficha de cliente {ClienteId} (modal overlay)", trabajo.Id, ClienteSeleccionado?.Id);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al abrir trabajo {TrabajoId} desde la ficha de cliente", trabajo.Id);
            MensajeError = $"Error al abrir el trabajo: {ex.Message}";
        }
    }

    /// <summary>
    /// Guarda el cliente (crea nuevo o actualiza existente).
    /// </summary>
    [RelayCommand]
    private async Task GuardarCliente()
    {
        try
        {
            // Validación de campos obligatorios
            if (string.IsNullOrWhiteSpace(Nombre))
            {
                MensajeError = "El nombre es obligatorio";
                return;
            }

            var nombreTrimmed = Nombre.Trim();
            if (nombreTrimmed.Length < 2)
            {
                MensajeError = "El nombre debe tener al menos 2 caracteres";
                return;
            }

            if (string.IsNullOrWhiteSpace(Apellidos))
            {
                MensajeError = "Los apellidos son obligatorios";
                return;
            }

            var apellidosTrimmed = Apellidos.Trim();
            if (apellidosTrimmed.Length < 2)
            {
                MensajeError = "Los apellidos deben tener al menos 2 caracteres";
                return;
            }

            // DNI/NIE/Pasaporte es obligatorio
            if (string.IsNullOrWhiteSpace(Dni))
            {
                MensajeError = "El documento de identidad (DNI/NIE/Pasaporte) es obligatorio";
                return;
            }

            // Normalizar DNI: eliminar espacios y convertir a mayúsculas
            var dniTrimmed = Dni.Trim().ToUpperInvariant();

            // Validar formato de DNI/NIE (si no es válido, puede ser un pasaporte)
            if (!EsDniNieValido(dniTrimmed))
            {
                // Si no es DNI/NIE válido, verificar que tenga al menos algún formato razonable (mínimo 5 caracteres)
                if (dniTrimmed.Length < 5)
                {
                    MensajeError = "El documento de identidad debe tener al menos 5 caracteres. Formato: 12345678A (DNI), X1234567L (NIE) o número de pasaporte";
                    return;
                }
                // Si tiene más de 5 caracteres, asumimos que es un pasaporte (no validamos formato específico)
            }

            // Validar teléfono (opcional, pero si se proporciona debe tener formato válido)
            if (!string.IsNullOrWhiteSpace(Telefono))
            {
                var telefonoTrimmed = Telefono.Trim();
                if (!EsTelefonoValido(telefonoTrimmed))
                {
                    MensajeError = "El formato del teléfono no es válido. Formato: 612345678 o +34612345678";
                    return;
                }
            }

            // Validar email (opcional, pero si se proporciona debe tener formato válido)
            if (!string.IsNullOrWhiteSpace(Email))
            {
                var emailTrimmed = Email.Trim();
                if (!EsEmailValido(emailTrimmed))
                {
                    MensajeError = "El formato del email no es válido. Ejemplo: cliente@email.com";
                    return;
                }
            }

            // Verificar que el DNI/Pasaporte no esté duplicado (para nuevos clientes o si cambió)
            var clienteIdActual = EsEdicion && ClienteSeleccionado != null ? ClienteSeleccionado.Id : 0;
            
            if (!EsEdicion || (EsEdicion && ClienteSeleccionado != null && ClienteSeleccionado.Dni != dniTrimmed))
            {
                var dniDuplicado = await _db.Clientes
                    .AnyAsync(c => c.Dni == dniTrimmed && c.Id != clienteIdActual);
                
                if (dniDuplicado)
                {
                    MensajeError = "Ya existe un cliente con ese documento de identidad.";
                    return;
                }
            }

            // Teléfono y email son opcionales; no se valida unicidad de teléfono.

            Cargando = true;
            MensajeError = string.Empty;

            Cliente clienteGuardado;

            // Capitalizar nombre y apellidos (primera letra en mayúscula)
            var nombreCapitalizado = CapitalizarTexto(nombreTrimmed);
            var apellidosCapitalizados = CapitalizarTexto(apellidosTrimmed);

            if (EsEdicion && ClienteSeleccionado != null)
            {
                // Actualizar cliente existente
                Log.Information("Actualizando cliente ID: {ClienteId}, Nombre: {Nombre}, FechaNacimiento: {FechaNacimiento}", 
                    ClienteSeleccionado.Id, Nombre, FechaNacimiento);
                ClienteSeleccionado.Nombre = nombreCapitalizado;
                ClienteSeleccionado.Apellidos = apellidosCapitalizados;
                ClienteSeleccionado.Telefono = string.IsNullOrWhiteSpace(Telefono) ? string.Empty : Telefono.Trim();
                ClienteSeleccionado.Email = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim();
                ClienteSeleccionado.Dni = dniTrimmed; // DNI/NIE/Pasaporte validado y en mayúsculas
                ClienteSeleccionado.FechaNacimiento = FechaNacimiento?.DateTime;
                ClienteSeleccionado.Alergias = string.IsNullOrWhiteSpace(Alergias) ? null : Alergias.Trim();
                ClienteSeleccionado.Notas = string.IsNullOrWhiteSpace(Notas) ? null : Notas.Trim();
                
                // Datos del tutor (solo si es menor de edad)
                ClienteSeleccionado.NombreTutor = string.IsNullOrWhiteSpace(NombreTutor) ? null : NombreTutor.Trim();
                ClienteSeleccionado.ApellidosTutor = string.IsNullOrWhiteSpace(ApellidosTutor) ? null : ApellidosTutor.Trim();
                ClienteSeleccionado.DniTutor = string.IsNullOrWhiteSpace(DniTutor) ? null : DniTutor.Trim().ToUpperInvariant();
                ClienteSeleccionado.TelefonoTutor = string.IsNullOrWhiteSpace(TelefonoTutor) ? null : TelefonoTutor.Trim();
                
                clienteGuardado = ClienteSeleccionado;
            }
            else
            {
                // Crear nuevo cliente
                Log.Information("Creando nuevo cliente: {Nombre} {Apellidos}, Tel: {Telefono}", 
                    Nombre, Apellidos ?? string.Empty, Telefono ?? string.Empty);
                var nuevoCliente = new Cliente
                {
                    Nombre = nombreCapitalizado,
                    Apellidos = apellidosCapitalizados,
                    Telefono = string.IsNullOrWhiteSpace(Telefono) ? string.Empty : Telefono.Trim(),
                    Email = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim(),
                    Dni = dniTrimmed, // DNI/NIE/Pasaporte validado y en mayúsculas
                    FechaNacimiento = FechaNacimiento?.DateTime,
                    Alergias = string.IsNullOrWhiteSpace(Alergias) ? null : Alergias.Trim(),
                    Notas = string.IsNullOrWhiteSpace(Notas) ? null : Notas.Trim(),
                    // Datos del tutor (solo si es menor de edad)
                    NombreTutor = string.IsNullOrWhiteSpace(NombreTutor) ? null : NombreTutor.Trim(),
                    ApellidosTutor = string.IsNullOrWhiteSpace(ApellidosTutor) ? null : ApellidosTutor.Trim(),
                    DniTutor = string.IsNullOrWhiteSpace(DniTutor) ? null : DniTutor.Trim().ToUpperInvariant(),
                    TelefonoTutor = string.IsNullOrWhiteSpace(TelefonoTutor) ? null : TelefonoTutor.Trim(),
                    FechaRegistro = DateTime.Now,
                    Activo = true
                };
                _db.Clientes.Add(nuevoCliente);
                clienteGuardado = nuevoCliente;
            }

            await _db.SaveChangesAsync();
            Log.Information("Cliente guardado exitosamente");

            // Para nuevos clientes, abrir flujos de consentimientos SOLO si el usuario lo ha marcado en el formulario.
            // Si no marca nada, se crea el cliente sin consentimientos y el sistema avisará desde otros flujos (trabajos/citas).
            if (!EsEdicion)
            {
                // Cerrar formulario de cliente antes de abrir el modal de firma
                MostrarFormulario = false;

                // Asegurar instancia única del ViewModel de firma con manejador de refresco
                if (ConsentimientoFirmaVM == null)
                {
                    ConsentimientoFirmaVM = new ConsentimientoFirmaViewModel();
                    ConsentimientoFirmaVM.FirmaCompletada += async (s, cliente) =>
                    {
                        await CargarClientes();
                        await RefrescarFichaClientePorIdAsync(cliente.Id);

                        // Si había pendiente abrir consentimiento de imágenes tras RGPD, hazlo ahora
                        if (_clientePendienteImagenesDespuesRgpdId.HasValue &&
                            _clientePendienteImagenesDespuesRgpdId.Value == cliente.Id)
                        {
                            var clienteId = _clientePendienteImagenesDespuesRgpdId.Value;
                            _clientePendienteImagenesDespuesRgpdId = null; // limpiar antes para evitar bucles

                            try
                            {
                                var clienteDb = await _db.Clientes.FirstOrDefaultAsync(c => c.Id == clienteId);
                                if (clienteDb != null)
                                {
                                    await ConsentimientoFirmaVM.AbrirModal(clienteDb, TipoConsentimiento.Imagenes);
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex, "Error al abrir consentimiento de imágenes encadenado tras RGPD para cliente {ClienteId}", clienteId);
                                MensajeError = $"Error al abrir el consentimiento de imágenes: {ex.Message}";
                            }
                        }
                    };
                }

                // IMPORTANTE: aquí SIEMPRE estamos en un cliente NUEVO (no edición).
                // Si el usuario marca RGPD/Imágenes, abrimos en el orden elegido; si no marca nada, no se abre nada.

                // Reset de cadena por defecto
                _clientePendienteImagenesDespuesRgpdId = null;

                // 1) RGPD: si el usuario ha marcado la casilla
                if (FirmarConsentimientoRGPD)
                {
                    // Si también quiere imágenes, marcar que después de RGPD hay que abrir imágenes
                    if (SolicitarConsentimientoImagenes)
                    {
                        _clientePendienteImagenesDespuesRgpdId = clienteGuardado.Id;
                    }

                    await _db.Entry(clienteGuardado).ReloadAsync();
                    await ConsentimientoFirmaVM.AbrirModal(clienteGuardado, TipoConsentimiento.RGPD);
                }
                // 2) Solo imágenes (sin RGPD marcado)
                else if (SolicitarConsentimientoImagenes)
                {
                    await _db.Entry(clienteGuardado).ReloadAsync();
                    await ConsentimientoFirmaVM.AbrirModal(clienteGuardado, TipoConsentimiento.Imagenes);
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
            Log.Warning("Intento de crear cliente con clave única duplicada. DNI: {Dni}. Error: {Error}", 
                Dni, ex.Message);

            // Verificar específicamente si es el DNI/Pasaporte el que está duplicado
            var dniTrimmed = Dni?.Trim()?.ToUpperInvariant();
            var clienteIdActual = 0;
            if (EsEdicion && ClienteSeleccionado != null)
            {
                clienteIdActual = ClienteSeleccionado.Id;
            }
            
            if (!string.IsNullOrWhiteSpace(dniTrimmed))
            {
                var dniTrimmedValue = dniTrimmed; // Variable local para evitar problema con expresión lambda
                var dniDuplicado = await _db.Clientes
                    .AnyAsync(c => c.Dni == dniTrimmedValue && c.Id != clienteIdActual);
                
                if (dniDuplicado)
                {
                    MensajeError = "Ya existe un cliente con ese documento de identidad.";
                }
                else
                {
                    MensajeError = "Error al guardar: conflicto con datos únicos.";
                }
            }
            else
            {
                MensajeError = "Error al guardar: conflicto con datos únicos.";
            }
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

    #region Comandos - Firma desde ficha de cliente

    /// <summary>
    /// Abre el flujo de firma de RGPD desde la ficha del cliente.
    /// Para menores se usa RGPD_Menor que requiere doble firma.
    /// </summary>
    [RelayCommand]
    private async Task FirmarConsentimientoRGPDDesdeFicha(Cliente cliente)
    {
        try
        {
            // Limpiar cualquier mensaje de error previo
            MensajeError = string.Empty;
            
            // No validamos aquí los datos del tutor, el modal de firma lo hace internamente
            // y muestra un mensaje prominente si faltan datos

            if (ConsentimientoFirmaVM == null)
            {
                ConsentimientoFirmaVM = new ConsentimientoFirmaViewModel();
                ConsentimientoFirmaVM.FirmaCompletada += async (s, c) =>
                {
                    await CargarClientes();
                    await RefrescarFichaClientePorIdAsync(c.Id);
                };
            }

            // Usar tipo de consentimiento correcto según edad
            var tipoConsentimiento = cliente.EsMenorDeEdad 
                ? TipoConsentimiento.RGPD_Menor 
                : TipoConsentimiento.RGPD;

            await ConsentimientoFirmaVM.AbrirModal(cliente, tipoConsentimiento);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al abrir firma de RGPD desde ficha de cliente {ClienteId}", cliente.Id);
            MensajeError = $"Error al abrir el consentimiento RGPD: {ex.Message}";
        }
    }

    /// <summary>
    /// Abre el flujo de firma de consentimiento de imágenes desde la ficha del cliente.
    /// NOTA: Los menores de edad no pueden firmar consentimiento de imágenes.
    /// </summary>
    [RelayCommand]
    private async Task FirmarConsentimientoImagenesDesdeFicha(Cliente cliente)
    {
        try
        {
            // Validar que no sea menor de edad
            if (cliente.EsMenorDeEdad)
            {
                MensajeError = "⚠️ Los menores de edad no pueden firmar consentimiento de uso de imágenes.";
                Log.Warning("Intento de firma de imágenes bloqueado para cliente menor: {ClienteId}", cliente.Id);
                return;
            }

            if (ConsentimientoFirmaVM == null)
            {
                ConsentimientoFirmaVM = new ConsentimientoFirmaViewModel();
                ConsentimientoFirmaVM.FirmaCompletada += async (s, c) =>
                {
                    await CargarClientes();
                    await RefrescarFichaClientePorIdAsync(c.Id);
                };
            }

            await ConsentimientoFirmaVM.AbrirModal(cliente, TipoConsentimiento.Imagenes);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al abrir firma de imágenes desde ficha de cliente {ClienteId}", cliente.Id);
            MensajeError = $"Error al abrir el consentimiento de imágenes: {ex.Message}";
        }
    }

    /// <summary>
    /// Abre el modal para subir foto del DNI del cliente.
    /// </summary>
    [RelayCommand]
    private async Task SubirFotoDniCliente(Cliente cliente)
    {
        try
        {
            MensajeError = string.Empty;

            if (FotoDniVM == null)
            {
                FotoDniVM = new FotoDniViewModel();
                FotoDniVM.FotoGuardada += async (s, c) =>
                {
                    await CargarClientes();
                    await RefrescarFichaClientePorIdAsync(c.Id);
                };
            }

            await FotoDniVM.AbrirModalAsync(cliente, esDniTutor: false);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al abrir modal de foto DNI para cliente {ClienteId}", cliente.Id);
            MensajeError = $"Error al abrir el modal de foto de DNI: {ex.Message}";
        }
    }

    /// <summary>
    /// Abre el modal para subir foto del DNI del tutor (para menores).
    /// </summary>
    [RelayCommand]
    private async Task SubirFotoDniTutor(Cliente cliente)
    {
        try
        {
            MensajeError = string.Empty;

            if (!cliente.EsMenorDeEdad)
            {
                MensajeError = "⚠️ Solo se puede subir foto de DNI de tutor para clientes menores de edad.";
                return;
            }

            if (!cliente.TieneDatosTutor)
            {
                MensajeError = "⚠️ Primero debes rellenar los datos del tutor (nombre, apellidos, DNI).";
                return;
            }

            if (FotoDniVM == null)
            {
                FotoDniVM = new FotoDniViewModel();
                FotoDniVM.FotoGuardada += async (s, c) =>
                {
                    await CargarClientes();
                    await RefrescarFichaClientePorIdAsync(c.Id);
                };
            }

            await FotoDniVM.AbrirModalAsync(cliente, esDniTutor: true);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al abrir modal de foto DNI tutor para cliente {ClienteId}", cliente.Id);
            MensajeError = $"Error al abrir el modal de foto de DNI del tutor: {ex.Message}";
        }
    }

    #endregion

    /// <summary>
    /// Elimina permanentemente el cliente seleccionado de la base de datos.
    /// También elimina todas sus citas, trabajos y consentimientos relacionados (cascada).
    /// Muestra un diálogo de confirmación antes de proceder.
    /// </summary>
    [RelayCommand]
    private async Task EliminarCliente()
    {
        if (ClienteSeleccionado == null) return;

        // Contar elementos relacionados para mostrar en la confirmación
        var numTrabajos = await _db.Trabajos.CountAsync(t => t.ClienteId == ClienteSeleccionado.Id);
        var numCitas = await _db.Citas.CountAsync(c => c.ClienteId == ClienteSeleccionado.Id);
        
        var advertencia = numTrabajos > 0 || numCitas > 0
            ? $"También se eliminarán {numTrabajos} trabajo(s) y {numCitas} cita(s) del cliente."
            : "Esta acción no se puede deshacer.";

        // Mostrar diálogo de confirmación
        var confirmado = await DialogService.ConfirmarEliminarAsync(
            tipoElemento: "el cliente",
            nombreElemento: ClienteSeleccionado.NombreCompleto,
            advertenciaAdicional: advertencia
        );

        if (!confirmado)
        {
            Log.Debug("Eliminación de cliente cancelada por el usuario: {ClienteId}", ClienteSeleccionado.Id);
            return;
        }

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
    /// Muestra un diálogo de confirmación antes de proceder.
    /// </summary>
    [RelayCommand]
    private async Task EliminarTodosLosClientes()
    {
        // Contar clientes antes de mostrar confirmación
        var totalClientes = await _db.Clientes.CountAsync();
        
        if (totalClientes == 0)
        {
            MensajeError = "No hay clientes para eliminar";
            return;
        }

        var totalTrabajos = await _db.Trabajos.CountAsync();
        var totalCitas = await _db.Citas.CountAsync();

        // Mostrar diálogo de confirmación MUY explícito
        var confirmado = await DialogService.ConfirmarAccionAsync(
            titulo: "⚠️ ELIMINAR TODOS LOS DATOS",
            mensaje: $"¿Estás SEGURO de que deseas eliminar TODOS los datos?\n\n" +
                     $"📊 Se eliminarán:\n" +
                     $"  • {totalClientes} cliente(s)\n" +
                     $"  • {totalTrabajos} trabajo(s)\n" +
                     $"  • {totalCitas} cita(s)\n" +
                     $"  • Todos los consentimientos asociados\n\n" +
                     $"❌ ESTA ACCIÓN NO SE PUEDE DESHACER",
            botonConfirmar: "Eliminar todo",
            esPeligroso: true
        );

        if (!confirmado)
        {
            Log.Debug("Eliminación de todos los clientes cancelada por el usuario");
            return;
        }

        try
        {
            Cargando = true;
            MensajeError = string.Empty;

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

            _todosLosClientes = new ObservableCollection<Cliente>(clientesSinRGPD);
            TotalClientes = clientesSinRGPD.Count;
            PaginaActual = 1; // Resetear a primera página al filtrar
            ActualizarPaginacion();
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
    /// Renueva un consentimiento existente.
    /// Marca el consentimiento anterior como renovado y abre el modal para firmar uno nuevo.
    /// </summary>
    [RelayCommand]
    private async Task RenovarConsentimiento(Consentimiento? consentimientoAnterior)
    {
        if (consentimientoAnterior == null || ClienteSeleccionado == null) return;

        try
        {
            Log.Information("Iniciando renovación de consentimiento {Id} para cliente {ClienteId}", 
                consentimientoAnterior.Id, ClienteSeleccionado.Id);

            // Determinar el tipo de consentimiento a crear
            // Si era de menor y ahora es mayor, usar el tipo normal
            var tipoNuevo = consentimientoAnterior.Tipo;
            if (consentimientoAnterior.EsConsentimientoMenor && !ClienteSeleccionado.EsMenorDeEdad)
            {
                tipoNuevo = consentimientoAnterior.Tipo switch
                {
                    TipoConsentimiento.RGPD_Menor => TipoConsentimiento.RGPD,
                    TipoConsentimiento.Trabajo_Menor => TipoConsentimiento.Trabajo,
                    _ => consentimientoAnterior.Tipo
                };
                Log.Information("Cliente ahora es mayor de edad, cambiando tipo de {TipoAnterior} a {TipoNuevo}", 
                    consentimientoAnterior.Tipo, tipoNuevo);
            }

            // Abrir modal de firma para el nuevo consentimiento
            if (ConsentimientoFirmaVM == null)
            {
                ConsentimientoFirmaVM = new ConsentimientoFirmaViewModel();
                ConsentimientoFirmaVM.FirmaCompletada += async (s, cliente) =>
                {
                    // Marcar el consentimiento anterior como renovado
                    consentimientoAnterior.Renovado = true;
                    await _db.SaveChangesAsync();
                    
                    await CargarClientes();
                    await RefrescarFichaClientePorIdAsync(cliente.Id);
                };
            }

            // Si es consentimiento de trabajo, necesitamos el trabajo asociado
            var trabajo = consentimientoAnterior.Trabajo;
            
            await ConsentimientoFirmaVM.AbrirModal(ClienteSeleccionado, tipoNuevo, trabajo);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al renovar consentimiento {ConsentimientoId}", consentimientoAnterior.Id);
            MensajeError = $"Error al renovar consentimiento: {ex.Message}";
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
        FechaNacimiento = null;
        FechaNacimientoTexto = string.Empty;
        Alergias = string.Empty;
        Notas = string.Empty;
        
        // Datos del tutor
        NombreTutor = string.Empty;
        ApellidosTutor = string.Empty;
        DniTutor = string.Empty;
        TelefonoTutor = string.Empty;
        
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
            : null;
        // FechaNacimientoTexto se actualizará automáticamente por OnFechaNacimientoChanged
        Alergias = cliente.Alergias ?? string.Empty;
        Notas = cliente.Notas ?? string.Empty;
        
        // Datos del tutor
        NombreTutor = cliente.NombreTutor ?? string.Empty;
        ApellidosTutor = cliente.ApellidosTutor ?? string.Empty;
        DniTutor = cliente.DniTutor ?? string.Empty;
        TelefonoTutor = cliente.TelefonoTutor ?? string.Empty;
        
        MensajeError = string.Empty;
    }

    #endregion

    #region Métodos Auxiliares

    /// <summary>
    /// Capitaliza un texto: primera letra de cada palabra en mayúscula, resto en minúscula.
    /// Ejemplo: "juan pérez garcía" -> "Juan Pérez García"
    /// </summary>
    private static string CapitalizarTexto(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return string.Empty;
        }

        var cultureInfo = CultureInfo.CurrentCulture;
        var textInfo = cultureInfo.TextInfo;
        
        // Usar ToTitleCase para capitalizar cada palabra
        return textInfo.ToTitleCase(texto.ToLower(cultureInfo));
    }

    /// <summary>
    /// Valida el formato de un DNI o NIE español.
    /// DNI: 8 dígitos seguidos de una letra (ej: 12345678A)
    /// NIE: X, Y o Z seguido de 7 dígitos y una letra (ej: X1234567L)
    /// </summary>
    private static bool EsDniNieValido(string dni)
    {
        if (string.IsNullOrWhiteSpace(dni))
        {
            return false;
        }

        // Eliminar espacios y convertir a mayúsculas
        dni = dni.Trim().ToUpperInvariant();

        // Validar formato DNI: 8 dígitos + 1 letra
        if (System.Text.RegularExpressions.Regex.IsMatch(dni, @"^\d{8}[A-Z]$"))
        {
            return ValidarLetraDni(dni);
        }

        // Validar formato NIE: X/Y/Z + 7 dígitos + 1 letra
        if (System.Text.RegularExpressions.Regex.IsMatch(dni, @"^[XYZ]\d{7}[A-Z]$"))
        {
            return ValidarLetraNie(dni);
        }

        return false;
    }

    /// <summary>
    /// Valida la letra de control de un DNI español.
    /// </summary>
    private static bool ValidarLetraDni(string dni)
    {
        if (dni.Length != 9)
            return false;

        // Extraer los 8 dígitos
        if (!int.TryParse(dni.Substring(0, 8), out int numero))
            return false;

        // Letras válidas para DNI (en orden)
        string letras = "TRWAGMYFPDXBNJZSQVHLCKE";
        int resto = numero % 23;
        char letraEsperada = letras[resto];
        char letraRecibida = dni[8];

        return letraEsperada == letraRecibida;
    }

    /// <summary>
    /// Valida la letra de control de un NIE español.
    /// </summary>
    private static bool ValidarLetraNie(string nie)
    {
        if (nie.Length != 9)
            return false;

        // Reemplazar X, Y, Z por 0, 1, 2 respectivamente
        char primeraLetra = nie[0];
        string numeroStr = nie.Substring(1, 7);
        
        if (!int.TryParse(numeroStr, out int numero))
            return false;

        // Convertir primera letra a número
        int numeroInicial = primeraLetra switch
        {
            'X' => 0,
            'Y' => 1,
            'Z' => 2,
            _ => -1
        };

        if (numeroInicial == -1)
            return false;

        // Construir el número completo: primera letra como número + 7 dígitos
        string numeroCompleto = numeroInicial.ToString() + numeroStr;
        
        if (!int.TryParse(numeroCompleto, out int numeroTotal))
            return false;

        // Letras válidas para NIE (mismo algoritmo que DNI)
        string letras = "TRWAGMYFPDXBNJZSQVHLCKE";
        int resto = numeroTotal % 23;
        char letraEsperada = letras[resto];
        char letraRecibida = nie[8];

        return letraEsperada == letraRecibida;
    }

    /// <summary>
    /// Valida el formato de un teléfono español.
    /// Acepta formatos como: 612345678, +34612345678, 612 345 678, 612-345-678
    /// </summary>
    private static bool EsTelefonoValido(string telefono)
    {
        if (string.IsNullOrWhiteSpace(telefono))
            return false;

        // Eliminar espacios, guiones y paréntesis para validar
        string telefonoLimpio = System.Text.RegularExpressions.Regex.Replace(telefono, @"[\s\-\(\)]", "");

        // Formato con prefijo internacional: +34 seguido de 9 dígitos
        if (System.Text.RegularExpressions.Regex.IsMatch(telefonoLimpio, @"^\+34\d{9}$"))
            return true;

        // Formato nacional: 9 dígitos (puede empezar con 6, 7, 8 o 9)
        if (System.Text.RegularExpressions.Regex.IsMatch(telefonoLimpio, @"^[6789]\d{8}$"))
            return true;

        // Formato con prefijo 0034: 0034 seguido de 9 dígitos
        if (System.Text.RegularExpressions.Regex.IsMatch(telefonoLimpio, @"^0034\d{9}$"))
            return true;

        return false;
    }

    /// <summary>
    /// Valida el formato de un email.
    /// </summary>
    private static bool EsEmailValido(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            var mailAddress = new System.Net.Mail.MailAddress(email);
            return mailAddress.Address == email; // Verifica que no haya espacios o caracteres adicionales
        }
        catch
        {
            return false;
        }
    }

    #endregion

    #region Paginación

    /// <summary>
    /// Actualiza la lista de clientes mostrados según la página actual.
    /// </summary>
    private void ActualizarPaginacion()
    {
        if (_todosLosClientes == null || _todosLosClientes.Count == 0)
        {
            Clientes.Clear();
            TotalPaginas = 1;
            return;
        }

        // Calcular total de páginas
        TotalPaginas = (int)Math.Ceiling((double)_todosLosClientes.Count / TamanoPagina);
        
        // Asegurar que la página actual sea válida
        if (PaginaActual < 1)
            PaginaActual = 1;
        if (PaginaActual > TotalPaginas)
            PaginaActual = TotalPaginas;

        // Obtener los clientes de la página actual
        var inicio = (PaginaActual - 1) * TamanoPagina;
        var fin = Math.Min(inicio + TamanoPagina, _todosLosClientes.Count);
        var clientesPagina = _todosLosClientes.Skip(inicio).Take(fin - inicio).ToList();

        Clientes = new ObservableCollection<Cliente>(clientesPagina);
        
        // Notificar cambios en los comandos de navegación
        PaginaAnteriorCommand.NotifyCanExecuteChanged();
        PaginaSiguienteCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Se ejecuta cuando cambia la página actual.
    /// </summary>
    partial void OnPaginaActualChanged(int value)
    {
        ActualizarPaginacion();
    }

    /// <summary>
    /// Se ejecuta cuando cambia el tamaño de página.
    /// </summary>
    partial void OnTamanoPaginaChanged(int value)
    {
        PaginaActual = 1; // Resetear a primera página
        ActualizarPaginacion();
    }

    /// <summary>
    /// Navega a la página anterior.
    /// </summary>
    [RelayCommand(CanExecute = nameof(PuedeIrAPaginaAnterior))]
    private void PaginaAnterior()
    {
        if (PaginaActual > 1)
        {
            PaginaActual--;
        }
    }

    /// <summary>
    /// Navega a la página siguiente.
    /// </summary>
    [RelayCommand(CanExecute = nameof(PuedeIrAPaginaSiguiente))]
    private void PaginaSiguiente()
    {
        if (PaginaActual < TotalPaginas)
        {
            PaginaActual++;
        }
    }

    /// <summary>
    /// Determina si se puede ir a la página anterior.
    /// </summary>
    private bool PuedeIrAPaginaAnterior() => PaginaActual > 1;

    /// <summary>
    /// Determina si se puede ir a la página siguiente.
    /// </summary>
    private bool PuedeIrAPaginaSiguiente() => PaginaActual < TotalPaginas;

    #endregion
}

