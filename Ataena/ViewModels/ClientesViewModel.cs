using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
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
    /// Handlers temporarios al renovar un consentimiento (evita fugas y duplicados al cerrar sin firmar).
    /// </summary>
    private EventHandler<Cliente>? _renovacionConsentimientoFirmaHandler;
    private EventHandler? _renovacionModalCerradoHandler;

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

    public ClientesViewModel()
    {
        _ordenSeleccionado = OpcionesOrden.First(o => o.Valor == OrdenClientes.MasReciente);
    }

    #region Propiedades - Lista y Selección

    /// <summary>
    /// Colección completa de clientes (sin paginación).
    /// </summary>
    private ObservableCollection<Cliente> _todosLosClientes = new();

    /// <summary>
    /// Caché en memoria para búsqueda dinámica sin consultar la BD en cada tecla.
    /// </summary>
    private List<Cliente> _clientesEnCache = new();

    /// <summary>
    /// Indica si la caché actual solo contiene clientes sin RGPD.
    /// </summary>
    private bool _filtroSinRgpdActivo;

    private CancellationTokenSource? _busquedaCts;

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

    partial void OnTextoBusquedaChanged(string value) => ProgramarBusquedaAutomatica();

    /// <summary>
    /// Opciones del desplegable de ordenación.
    /// </summary>
    public IReadOnlyList<OpcionOrdenCliente> OpcionesOrden { get; } =
    [
        new(OrdenClientes.MasReciente, "Más reciente"),
        new(OrdenClientes.MasAntiguo, "Más antiguo"),
        new(OrdenClientes.Nombre, "Nombre (A-Z)"),
        new(OrdenClientes.Edad, "Edad (mayor a menor)")
    ];

    /// <summary>
    /// Ordenación seleccionada en la UI (por defecto: más reciente).
    /// </summary>
    [ObservableProperty]
    private OpcionOrdenCliente _ordenSeleccionado;

    partial void OnOrdenSeleccionadoChanged(OpcionOrdenCliente value)
    {
        if (value != null && _todosLosClientes.Count > 0)
            ReordenarClientesActuales();
    }

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
                        var msgBisiesto = $"El año {fecha.Year} no es bisiesto (29/02 no existe).";
                        MensajeError = msgBisiesto;
                        ErrorFechaNacimiento = msgBisiesto;
                        return; // No actualizar la fecha
                    }
                }

                if (AplicarFechaNacimientoSiValida(fecha))
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
                    var msgBisiestoFlex = $"El año {fechaFlexible.Year} no es bisiesto (29/02 no existe).";
                    MensajeError = msgBisiestoFlex;
                    ErrorFechaNacimiento = msgBisiestoFlex;
                    return; // No actualizar la fecha
                }
            }

            AplicarFechaNacimientoSiValida(fechaFlexible);
        }

        ValidarFechaNacimiento();
        // Si no se puede parsear, no actualizamos FechaNacimiento (permitimos texto parcial mientras se escribe)
    }

    /// <summary>
    /// Aplica la fecha de nacimiento si es válida (no futura). Devuelve true si se aplicó.
    /// </summary>
    private bool AplicarFechaNacimientoSiValida(DateTime fecha)
    {
        if (fecha.Date > DateTime.Today)
        {
            const string msg = "La fecha de nacimiento no puede ser posterior a hoy.";
            MensajeError = msg;
            ErrorFechaNacimiento = msg;
            return true;
        }

        var nuevaFecha = new DateTimeOffset(fecha.Date);
        if (!FechaNacimiento.HasValue || FechaNacimiento.Value.Date != nuevaFecha.Date)
        {
            _actualizandoFechaDesdeTexto = true;
            FechaNacimiento = nuevaFecha;
            _actualizandoFechaDesdeTexto = false;
            MensajeError = string.Empty;
            ErrorFechaNacimiento = string.Empty;
        }

        ValidarFechaNacimiento();
        return true;
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
        ValidarFechaNacimiento();
        if (EsMenorFormulario)
        {
            ValidarNombreTutor();
            ValidarApellidosTutor();
            ValidarDniTutor();
        }
        
        // Evitar bucle infinito: si estamos actualizando desde el texto, no actualizar el texto
        if (_actualizandoFechaDesdeTexto)
            return;

        if (value.HasValue && value.Value.Date > DateTime.Today)
        {
            const string msg = "La fecha de nacimiento no puede ser posterior a hoy.";
            MensajeError = msg;
            ErrorFechaNacimiento = msg;
            _actualizandoFechaDesdeTexto = true;
            FechaNacimiento = null;
            _actualizandoFechaDesdeTexto = false;
            return;
        }

        if (value.HasValue)
        {
            MensajeError = string.Empty;
            ErrorFechaNacimiento = string.Empty;
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

    /// <summary>
    /// Tras intentar guardar, se muestran errores también en campos vacíos obligatorios.
    /// Obligatorios: nombre, apellidos, DNI, fecha de nacimiento (+ tutor si es menor).
    /// Opcionales con formato: teléfono, email, teléfono del tutor.
    /// </summary>
    [ObservableProperty]
    private bool _validacionFormularioActiva;

    /// <summary>
    /// Aviso modal centrado cuando faltan campos obligatorios al guardar.
    /// </summary>
    [ObservableProperty]
    private bool _mostrarAlertaValidacionFormulario;

    [ObservableProperty]
    private string _mensajeAlertaValidacionFormulario = string.Empty;

    [RelayCommand]
    private void CerrarAlertaValidacionFormulario()
    {
        MostrarAlertaValidacionFormulario = false;
    }

    private static readonly string[] FormatosFechaNacimiento =
        { "dd/MM/yyyy", "dd-MM-yyyy", "d/M/yyyy", "d-M-yyyy" };

    [ObservableProperty]
    private string _errorNombre = string.Empty;

    [ObservableProperty]
    private string _errorApellidos = string.Empty;

    [ObservableProperty]
    private string _errorTelefono = string.Empty;

    [ObservableProperty]
    private string _errorEmail = string.Empty;

    [ObservableProperty]
    private string _errorDni = string.Empty;

    [ObservableProperty]
    private string _errorFechaNacimiento = string.Empty;

    [ObservableProperty]
    private string _errorNombreTutor = string.Empty;

    [ObservableProperty]
    private string _errorApellidosTutor = string.Empty;

    [ObservableProperty]
    private string _errorDniTutor = string.Empty;

    [ObservableProperty]
    private string _errorTelefonoTutor = string.Empty;

    partial void OnNombreChanged(string value) => ValidarNombre();
    partial void OnApellidosChanged(string value) => ValidarApellidos();
    partial void OnTelefonoChanged(string value) => ValidarTelefono();
    partial void OnEmailChanged(string value) => ValidarEmail();
    partial void OnDniChanged(string value) => ValidarDni();

    partial void OnNombreTutorChanged(string value) => ValidarNombreTutor();
    partial void OnApellidosTutorChanged(string value) => ValidarApellidosTutor();
    partial void OnDniTutorChanged(string value) => ValidarDniTutor();
    partial void OnTelefonoTutorChanged(string value) => ValidarTelefonoTutor();

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
    /// Si es false, el cliente no quiere fotos de trabajo ni el consentimiento de imágenes asociado.
    /// </summary>
    [ObservableProperty]
    private bool _permiteFotosTrabajo = true;

    partial void OnPermiteFotosTrabajoChanged(bool value)
    {
        if (!value)
            SolicitarConsentimientoImagenes = false;
    }

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
            await EnUiThreadAsync(() =>
            {
                Cargando = true;
                MensajeError = string.Empty;
            });

            // AsNoTracking() evita que EF Core use entidades en caché,
            // asegurando datos frescos de la BD después de actualizaciones
            var lista = await _db.Clientes
                .AsNoTracking()
                .Include(c => c.Consentimientos)
                .ToListAsync();

            _clientesEnCache = lista;
            _filtroSinRgpdActivo = false;
            await EnUiThreadAsync(() => EstablecerListaClientes(lista));
            Log.Information("Clientes cargados: {Count} clientes activos", lista.Count);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al cargar clientes desde la base de datos");
            await EnUiThreadAsync(() =>
                MensajeError = $"Error al cargar clientes: {ex.Message}");
        }
        finally
        {
            await EnUiThreadAsync(() => Cargando = false);
        }
    }

    /// <summary>
    /// Programa la búsqueda automática con un breve retardo al escribir.
    /// </summary>
    private void ProgramarBusquedaAutomatica()
    {
        _busquedaCts?.Cancel();
        _busquedaCts?.Dispose();
        _busquedaCts = new CancellationTokenSource();
        var token = _busquedaCts.Token;
        _ = AplicarBusquedaAutomaticaAsync(token);
    }

    /// <summary>
    /// Filtra la lista en memoria según <see cref="TextoBusqueda"/>.
    /// </summary>
    private async Task AplicarBusquedaAutomaticaAsync(CancellationToken token)
    {
        try
        {
            await Task.Delay(250, token);

            if (string.IsNullOrWhiteSpace(TextoBusqueda) && !_filtroSinRgpdActivo)
            {
                await CargarClientes();
                return;
            }

            if (_clientesEnCache.Count == 0)
            {
                await CargarClientes();
                return;
            }

            var lista = FiltrarClientesEnCache(TextoBusqueda);
            await EnUiThreadAsync(() =>
            {
                PaginaActual = 1;
                EstablecerListaClientes(lista);
            });
        }
        catch (OperationCanceledException)
        {
            // Nueva pulsación de tecla: se cancela la búsqueda anterior.
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error en búsqueda automática de clientes");
            await EnUiThreadAsync(() =>
                MensajeError = $"Error en la búsqueda: {ex.Message}");
        }
    }

    /// <summary>
    /// Aplica el texto de búsqueda sobre la caché actual.
    /// </summary>
    private List<Cliente> FiltrarClientesEnCache(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return _clientesEnCache;

        var termino = TextoBusquedaHelper.Normalizar(texto);
        var terminoDigitos = TextoBusquedaHelper.SoloDigitos(texto);
        return _clientesEnCache
            .Where(c => TextoBusquedaHelper.ClienteCoincide(c, termino, terminoDigitos))
            .ToList();
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

        if (_trabajosVM != null)
            await _trabajosVM.RefrescarTrasConsentimientoClienteAsync(clienteId);
    }

    /// <summary>
    /// Tras actualizar datos de cliente, recarga trabajos (y cliente embebido) para que RGPD/imágenes y botones de foto coincidan con la BD.
    /// </summary>
    private async Task RefrescarTrabajosTrasClienteAsync()
    {
        if (_trabajosVM == null || ClienteSeleccionado == null)
            return;

        var trabajoSeleccionadoId = _trabajosVM.TrabajoSeleccionado?.Id;
        await _trabajosVM.RefrescarTrasConsentimientoClienteAsync(ClienteSeleccionado.Id);

        if (trabajoSeleccionadoId.HasValue)
        {
            var t = _trabajosVM.Trabajos.FirstOrDefault(x => x.Id == trabajoSeleccionadoId.Value);
            _trabajosVM.TrabajoSeleccionado = t;
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
    /// Abre la ficha del cliente seleccionado.
    /// </summary>
    [RelayCommand]
    private async Task VerFichaCliente(Cliente? cliente = null)
    {
        var clienteAVer = cliente ?? ClienteSeleccionado;
        if (clienteAVer == null) return;

        try
        {
            // Limpiar el ChangeTracker para evitar que entidades cacheadas (de otras consultas
            // o de otros DbContext) impidan ver cambios recientes (p.ej. un consentimiento
            // recién firmado y guardado en otro contexto).
            _db.ChangeTracker.Clear();

            // Cargar cliente fresco desde BD con todas las relaciones, sin trackear:
            // así cada vez que llamemos a este método obtenemos un snapshot 100% fresco.
            var clienteFresco = await _db.Clientes
                .AsNoTracking()
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

            Log.Information("Ficha del cliente abierta: {ClienteId} (Consentimientos: {Count})",
                clienteFresco.Id, ConsentimientosCliente.Count);
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
    /// Abre la sección Trabajos con un trabajo nuevo y el cliente de la ficha ya seleccionado.
    /// </summary>
    [RelayCommand]
    private async Task NuevoTrabajoDesdeFichaCliente()
    {
        if (ClienteSeleccionado == null || _trabajosVM == null || _mainWindowVM == null)
        {
            Log.Warning("No se puede crear trabajo desde ficha (cliente o referencias incompletas)");
            return;
        }

        try
        {
            MensajeError = string.Empty;
            _mainWindowVM.IrATrabajosCommand.Execute(null);
            await _trabajosVM.NuevoTrabajoParaCliente(ClienteSeleccionado);
            Log.Information("Nuevo trabajo abierto desde ficha del cliente {ClienteId}", ClienteSeleccionado.Id);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al abrir nuevo trabajo desde ficha de cliente");
            MensajeError = $"Error al abrir el formulario de trabajo: {ex.Message}";
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
            ValidacionFormularioActiva = true;
            if (!ValidarFormularioCompleto())
            {
                MensajeAlertaValidacionFormulario = ObtenerPrimerMensajeErrorFormulario();
                MostrarAlertaValidacionFormulario = true;
                MensajeError = string.Empty;
                return;
            }

            if (!FechaNacimiento.HasValue && TryParseFechaNacimientoTexto(out var fechaDesdeTexto))
                FechaNacimiento = new DateTimeOffset(fechaDesdeTexto.Date);

            var nombreTrimmed = Nombre.Trim();
            var apellidosTrimmed = Apellidos.Trim();
            var dniTrimmed = Dni.Trim().ToUpperInvariant();

            var clienteIdActual = EsEdicion && ClienteSeleccionado != null ? ClienteSeleccionado.Id : 0;

            if (!EsEdicion || (EsEdicion && ClienteSeleccionado != null && ClienteSeleccionado.Dni != dniTrimmed))
            {
                var dniDuplicado = await _db.Clientes
                    .AnyAsync(c => c.Dni == dniTrimmed && c.Id != clienteIdActual);

                if (dniDuplicado)
                {
                    ErrorDni = "Ya existe un cliente con ese documento de identidad.";
                    MensajeAlertaValidacionFormulario = ErrorDni;
                    MostrarAlertaValidacionFormulario = true;
                    MensajeError = string.Empty;
                    return;
                }
            }

            Cargando = true;
            MensajeError = string.Empty;
            LimpiarErroresCampos();
            ValidacionFormularioActiva = false;

            Cliente clienteGuardado;

            // Capitalizar nombre y apellidos (primera letra en mayúscula)
            var nombreCapitalizado = CapitalizarTexto(nombreTrimmed);
            var apellidosCapitalizados = CapitalizarTexto(apellidosTrimmed);

            if (EsEdicion && ClienteSeleccionado != null)
            {
                // IMPORTANTE: ClienteSeleccionado suele ser AsNoTracking (lista/ficha). Hay que persistir contra una entidad rastreada.
                Log.Information("Actualizando cliente ID: {ClienteId}, Nombre: {Nombre}, FechaNacimiento: {FechaNacimiento}",
                    ClienteSeleccionado.Id, Nombre, FechaNacimiento);

                var tracked = await _db.Clientes.FindAsync(ClienteSeleccionado.Id);
                if (tracked == null)
                {
                    MensajeError = "Cliente no encontrado.";
                    return;
                }

                tracked.Nombre = nombreCapitalizado;
                tracked.Apellidos = apellidosCapitalizados;
                tracked.Telefono = string.IsNullOrWhiteSpace(Telefono) ? string.Empty : Telefono.Trim();
                tracked.Email = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim();
                tracked.Dni = dniTrimmed;
                tracked.FechaNacimiento = FechaNacimiento?.DateTime;
                tracked.Alergias = string.IsNullOrWhiteSpace(Alergias) ? null : Alergias.Trim();
                tracked.Notas = string.IsNullOrWhiteSpace(Notas) ? null : Notas.Trim();
                tracked.NombreTutor = string.IsNullOrWhiteSpace(NombreTutor) ? null : NombreTutor.Trim();
                tracked.ApellidosTutor = string.IsNullOrWhiteSpace(ApellidosTutor) ? null : ApellidosTutor.Trim();
                tracked.DniTutor = string.IsNullOrWhiteSpace(DniTutor) ? null : DniTutor.Trim().ToUpperInvariant();
                tracked.TelefonoTutor = string.IsNullOrWhiteSpace(TelefonoTutor) ? null : TelefonoTutor.Trim();
                tracked.PermiteFotosTrabajo = PermiteFotosTrabajo;

                // Mantener la instancia de vista alineada con lo que se guardará
                ClienteSeleccionado.Nombre = tracked.Nombre;
                ClienteSeleccionado.Apellidos = tracked.Apellidos;
                ClienteSeleccionado.Telefono = tracked.Telefono;
                ClienteSeleccionado.Email = tracked.Email;
                ClienteSeleccionado.Dni = tracked.Dni;
                ClienteSeleccionado.FechaNacimiento = tracked.FechaNacimiento;
                ClienteSeleccionado.Alergias = tracked.Alergias;
                ClienteSeleccionado.Notas = tracked.Notas;
                ClienteSeleccionado.NombreTutor = tracked.NombreTutor;
                ClienteSeleccionado.ApellidosTutor = tracked.ApellidosTutor;
                ClienteSeleccionado.DniTutor = tracked.DniTutor;
                ClienteSeleccionado.TelefonoTutor = tracked.TelefonoTutor;
                ClienteSeleccionado.PermiteFotosTrabajo = tracked.PermiteFotosTrabajo;

                clienteGuardado = tracked;
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
                    Activo = true,
                    PermiteFotosTrabajo = PermiteFotosTrabajo
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
                                if (clienteDb != null && clienteDb.PermiteFotosTrabajo)
                                {
                                    await ConsentimientoFirmaVM.AbrirModal(clienteDb,
                                        Consentimiento.TipoConsentimientoImagenesSegunEdad(clienteDb.EsMenorDeEdad));
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
                        if (SolicitarConsentimientoImagenes && PermiteFotosTrabajo)
                        {
                            _clientePendienteImagenesDespuesRgpdId = clienteGuardado.Id;
                        }

                        await _db.Entry(clienteGuardado).ReloadAsync();
                        await ConsentimientoFirmaVM.AbrirModal(clienteGuardado, TipoConsentimiento.RGPD);
                    }
                    // 2) Solo imágenes (sin RGPD marcado)
                    else if (SolicitarConsentimientoImagenes && PermiteFotosTrabajo)
                {
                    await _db.Entry(clienteGuardado).ReloadAsync();
                    await ConsentimientoFirmaVM.AbrirModal(clienteGuardado,
                        Consentimiento.TipoConsentimientoImagenesSegunEdad(clienteGuardado.EsMenorDeEdad));
                }
            }
            else
            {
                // Para edición, solo cerrar
                MostrarFormulario = false;
            }

            await CargarClientes();

            if (EsEdicion)
                await RefrescarTrabajosTrasClienteAsync();

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
    /// Los menores firman el tipo menor con autorización del tutor (doble firma).
    /// </summary>
    [RelayCommand]
    private async Task FirmarConsentimientoImagenesDesdeFicha(Cliente cliente)
    {
        try
        {
            if (!cliente.PermiteFotosTrabajo)
            {
                OverlayNotificationService.Mostrar(
                    "Este cliente no autoriza fotos de trabajo. Activa la opción en editar cliente si debe firmarse el consentimiento de imágenes.",
                    OverlayNotificationKind.Warning);
                return;
            }

            if (cliente.EsMenorDeEdad && !cliente.TieneDatosTutor)
            {
                OverlayNotificationService.Mostrar(
                    "Para firmar el consentimiento de imágenes de un menor necesitas nombre, apellidos y DNI del tutor en la ficha del cliente.",
                    OverlayNotificationKind.Warning);
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

            await ConsentimientoFirmaVM.AbrirModal(cliente,
                Consentimiento.TipoConsentimientoImagenesSegunEdad(cliente.EsMenorDeEdad));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al abrir firma de imágenes desde ficha de cliente {ClienteId}", cliente.Id);
            MensajeError = $"Error al abrir el consentimiento de imágenes: {ex.Message}";
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
        MostrarAlertaValidacionFormulario = false;
        MensajeAlertaValidacionFormulario = string.Empty;
        LimpiarErroresCampos();
        ValidacionFormularioActiva = false;
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
            await EnUiThreadAsync(() =>
            {
                Cargando = true;
                MensajeError = string.Empty;
            });

            // Cargar todos los clientes con sus consentimientos
            var todosClientes = await _db.Clientes
                .Include(c => c.Consentimientos)
                .ToListAsync();

            // Sin RGPD vigente: ni RGPD de adulto ni RGPD_Menor firmado (menores incluidos)
            var clientesSinRGPD = todosClientes
                .Where(c => !c.TieneConsentimientoRGPD)
                .ToList();

            _clientesEnCache = clientesSinRGPD;
            _filtroSinRgpdActivo = true;
            var filtrados = FiltrarClientesEnCache(TextoBusqueda);
            await EnUiThreadAsync(() =>
            {
                PaginaActual = 1;
                EstablecerListaClientes(filtrados);
            });

            Log.Information("Filtrado completado: {Count} clientes sin consentimiento RGPD", clientesSinRGPD.Count);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al filtrar clientes sin consentimiento");
            await EnUiThreadAsync(() =>
                MensajeError = $"Error en el filtrado: {ex.Message}");
        }
        finally
        {
            await EnUiThreadAsync(() => Cargando = false);
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
    /// Elimina handlers temporales registrados durante <see cref="RenovarConsentimiento"/>.
    /// </summary>
    private void QuitarHandlersRenovacionConsentimientoTemporal()
    {
        if (ConsentimientoFirmaVM == null)
            return;

        if (_renovacionConsentimientoFirmaHandler != null)
        {
            ConsentimientoFirmaVM.FirmaCompletada -= _renovacionConsentimientoFirmaHandler;
            _renovacionConsentimientoFirmaHandler = null;
        }

        if (_renovacionModalCerradoHandler != null)
        {
            ConsentimientoFirmaVM.ModalSesionFinalizada -= _renovacionModalCerradoHandler;
            _renovacionModalCerradoHandler = null;
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
                    TipoConsentimiento.Imagenes_Menor => TipoConsentimiento.Imagenes,
                    _ => consentimientoAnterior.Tipo
                };
                Log.Information("Cliente ahora es mayor de edad, cambiando tipo de {TipoAnterior} a {TipoNuevo}", 
                    consentimientoAnterior.Tipo, tipoNuevo);
            }

            ConsentimientoFirmaVM ??= new ConsentimientoFirmaViewModel();

            QuitarHandlersRenovacionConsentimientoTemporal();

            var idClienteSel = ClienteSeleccionado.Id;
            var idAnterior = consentimientoAnterior.Id;

            _renovacionConsentimientoFirmaHandler = async (_, cliente) =>
            {
                try
                {
                    QuitarHandlersRenovacionConsentimientoTemporal();

                    if (cliente.Id != idClienteSel)
                        return;

                    await using var cx = new AtaenaDbContext();
                    var anteriorEnBd = await cx.Consentimientos.FindAsync(idAnterior);
                    if (anteriorEnBd != null && !anteriorEnBd.Renovado)
                    {
                        anteriorEnBd.Renovado = true;
                        await cx.SaveChangesAsync();
                    }

                    await CargarClientes();
                    await RefrescarFichaClientePorIdAsync(cliente.Id);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error al marcar consentimiento anterior {Id} como renovado", idAnterior);
                }
            };
            _renovacionModalCerradoHandler = (_, _) =>
            {
                QuitarHandlersRenovacionConsentimientoTemporal();
            };

            ConsentimientoFirmaVM.FirmaCompletada += _renovacionConsentimientoFirmaHandler;
            ConsentimientoFirmaVM.ModalSesionFinalizada += _renovacionModalCerradoHandler;

            // Si es consentimiento de trabajo, necesitamos el trabajo asociado
            var trabajo = consentimientoAnterior.Trabajo;

            await ConsentimientoFirmaVM.AbrirModal(
                ClienteSeleccionado, tipoNuevo, trabajo,
                omitirConsentimientoIdRenovacion: idAnterior);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al renovar consentimiento {ConsentimientoId}", consentimientoAnterior.Id);
            MensajeError = $"Error al renovar consentimiento: {ex.Message}";
        }
    }

    /// <summary>
    /// Elimina un consentimiento firmado y su PDF. El cliente deja de tener ese consentimiento vigente.
    /// </summary>
    [RelayCommand]
    private async Task EliminarConsentimiento(Consentimiento? consentimiento)
    {
        if (consentimiento == null || ClienteSeleccionado == null || !consentimiento.Firmado)
            return;

        var mensaje =
            $"Se eliminará el consentimiento «{consentimiento.NombreTipo}» y su PDF.\n\n" +
            ConsentimientoService.MensajeAvisoTrasEliminar(consentimiento.Tipo, ClienteSeleccionado.NombreCompleto) +
            "\n\nEsta acción no se puede deshacer.";

        var confirmado = await DialogService.ConfirmarAccionAsync(
            titulo: "Eliminar consentimiento",
            mensaje: mensaje,
            botonConfirmar: "Sí, eliminar",
            esPeligroso: true);

        if (!confirmado)
            return;

        try
        {
            var (exito, _, _) = await ConsentimientoService.EliminarConsentimientoAsync(_db, consentimiento.Id);
            if (!exito)
            {
                MensajeError = "No se pudo eliminar el consentimiento.";
                return;
            }

            await CargarClientes();
            await RefrescarFichaClientePorIdAsync(ClienteSeleccionado.Id);

            OverlayNotificationService.Mostrar(
                ConsentimientoService.MensajeAvisoTrasEliminar(consentimiento.Tipo, ClienteSeleccionado.NombreCompleto),
                OverlayNotificationKind.Warning);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al eliminar consentimiento {ConsentimientoId}", consentimiento.Id);
            MensajeError = $"Error al eliminar: {ex.Message}";
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
        LimpiarErroresCampos();
        ValidacionFormularioActiva = false;
        SolicitarConsentimientoImagenes = false;
        PermiteFotosTrabajo = true;
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
        LimpiarErroresCampos();
        ValidacionFormularioActiva = false;
        PermiteFotosTrabajo = cliente.PermiteFotosTrabajo;
    }

    #endregion

    #region Validación por campo (chivatos)

    private void LimpiarErroresCampos()
    {
        ErrorNombre = string.Empty;
        ErrorApellidos = string.Empty;
        ErrorTelefono = string.Empty;
        ErrorEmail = string.Empty;
        ErrorDni = string.Empty;
        ErrorFechaNacimiento = string.Empty;
        ErrorNombreTutor = string.Empty;
        ErrorApellidosTutor = string.Empty;
        ErrorDniTutor = string.Empty;
        ErrorTelefonoTutor = string.Empty;
    }

    private bool ValidarFormularioCompleto()
    {
        ValidarNombre();
        ValidarApellidos();
        ValidarTelefono();
        ValidarEmail();
        ValidarDni();
        ValidarFechaNacimiento();

        if (EsMenorFormulario)
        {
            ValidarNombreTutor();
            ValidarApellidosTutor();
            ValidarDniTutor();
            ValidarTelefonoTutor();
        }
        else
        {
            ErrorNombreTutor = string.Empty;
            ErrorApellidosTutor = string.Empty;
            ErrorDniTutor = string.Empty;
            ErrorTelefonoTutor = string.Empty;
        }

        return !TieneErroresFormulario();
    }

    private bool TieneErroresFormulario() =>
        !string.IsNullOrEmpty(ErrorNombre) ||
        !string.IsNullOrEmpty(ErrorApellidos) ||
        !string.IsNullOrEmpty(ErrorTelefono) ||
        !string.IsNullOrEmpty(ErrorEmail) ||
        !string.IsNullOrEmpty(ErrorDni) ||
        !string.IsNullOrEmpty(ErrorFechaNacimiento) ||
        !string.IsNullOrEmpty(ErrorNombreTutor) ||
        !string.IsNullOrEmpty(ErrorApellidosTutor) ||
        !string.IsNullOrEmpty(ErrorDniTutor) ||
        !string.IsNullOrEmpty(ErrorTelefonoTutor);

    private string ObtenerPrimerMensajeErrorFormulario()
    {
        if (!string.IsNullOrEmpty(ErrorNombre)) return ErrorNombre;
        if (!string.IsNullOrEmpty(ErrorApellidos)) return ErrorApellidos;
        if (!string.IsNullOrEmpty(ErrorDni)) return ErrorDni;
        if (!string.IsNullOrEmpty(ErrorFechaNacimiento)) return ErrorFechaNacimiento;
        if (!string.IsNullOrEmpty(ErrorTelefono)) return ErrorTelefono;
        if (!string.IsNullOrEmpty(ErrorEmail)) return ErrorEmail;
        if (!string.IsNullOrEmpty(ErrorNombreTutor)) return ErrorNombreTutor;
        if (!string.IsNullOrEmpty(ErrorApellidosTutor)) return ErrorApellidosTutor;
        if (!string.IsNullOrEmpty(ErrorDniTutor)) return ErrorDniTutor;
        if (!string.IsNullOrEmpty(ErrorTelefonoTutor)) return ErrorTelefonoTutor;
        return "Revisa los campos marcados en rojo.";
    }

    private void ValidarNombre()
    {
        var t = Nombre?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(t))
        {
            ErrorNombre = ValidacionFormularioActiva ? "El nombre es obligatorio" : string.Empty;
            return;
        }

        ErrorNombre = t.Length < 2 && ValidacionFormularioActiva ? "Al menos 2 caracteres" : string.Empty;
    }

    private void ValidarApellidos()
    {
        var t = Apellidos?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(t))
        {
            ErrorApellidos = ValidacionFormularioActiva ? "Los apellidos son obligatorios" : string.Empty;
            return;
        }

        ErrorApellidos = t.Length < 2 && ValidacionFormularioActiva ? "Al menos 2 caracteres" : string.Empty;
    }

    private void ValidarTelefono()
    {
        var t = Telefono?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(t))
        {
            ErrorTelefono = string.Empty;
            return;
        }

        ErrorTelefono = EsTelefonoValido(t)
            ? string.Empty
            : "Formato: 612345678 o +34612345678";
    }

    private void ValidarEmail()
    {
        var t = Email?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(t))
        {
            ErrorEmail = string.Empty;
            return;
        }

        ErrorEmail = EsEmailValido(t)
            ? string.Empty
            : "Ejemplo: cliente@email.com";
    }

    private void ValidarDni()
    {
        var dni = (Dni ?? string.Empty).Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(dni))
        {
            ErrorDni = ValidacionFormularioActiva
                ? "El documento de identidad es obligatorio"
                : string.Empty;
            return;
        }

        if (EsDniNieValido(dni))
        {
            ErrorDni = string.Empty;
            return;
        }

        if (dni.Length < 5)
        {
            ErrorDni = "Mín. 5 caracteres (12345678A, X1234567L o pasaporte)";
            return;
        }

        ErrorDni = string.Empty;
    }

    private bool TryParseFechaNacimientoTexto(out DateTime fecha)
    {
        fecha = default;
        if (string.IsNullOrWhiteSpace(FechaNacimientoTexto))
            return false;

        var texto = FechaNacimientoTexto.Trim();
        foreach (var formato in FormatosFechaNacimiento)
        {
            if (DateTime.TryParseExact(texto, formato, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out fecha))
                return true;
        }

        return DateTime.TryParse(texto, CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha);
    }

    private void ValidarFechaNacimiento()
    {
        if (FechaNacimiento.HasValue)
        {
            ErrorFechaNacimiento = FechaNacimiento.Value.Date > DateTime.Today
                ? "No puede ser posterior a hoy"
                : string.Empty;
            return;
        }

        if (string.IsNullOrWhiteSpace(FechaNacimientoTexto))
        {
            ErrorFechaNacimiento = ValidacionFormularioActiva
                ? "La fecha de nacimiento es obligatoria"
                : string.Empty;
            return;
        }

        if (!TryParseFechaNacimientoTexto(out var fecha))
        {
            ErrorFechaNacimiento = ValidacionFormularioActiva
                ? "Introduce una fecha válida (DD/MM/AAAA)"
                : string.Empty;
            return;
        }

        if (fecha.Day == 29 && fecha.Month == 2 && !DateTime.IsLeapYear(fecha.Year))
        {
            ErrorFechaNacimiento = $"El año {fecha.Year} no es bisiesto (29/02 no existe)";
            return;
        }

        if (fecha.Date > DateTime.Today)
        {
            ErrorFechaNacimiento = "No puede ser posterior a hoy";
            return;
        }

        ErrorFechaNacimiento = string.Empty;
    }

    private void ValidarNombreTutor()
    {
        if (!EsMenorFormulario)
        {
            ErrorNombreTutor = string.Empty;
            return;
        }

        var t = NombreTutor?.Trim() ?? string.Empty;
        ErrorNombreTutor = string.IsNullOrEmpty(t) && ValidacionFormularioActiva
            ? "Obligatorio para menores"
            : string.Empty;
    }

    private void ValidarApellidosTutor()
    {
        if (!EsMenorFormulario)
        {
            ErrorApellidosTutor = string.Empty;
            return;
        }

        var t = ApellidosTutor?.Trim() ?? string.Empty;
        ErrorApellidosTutor = string.IsNullOrEmpty(t) && ValidacionFormularioActiva
            ? "Obligatorio para menores"
            : string.Empty;
    }

    private void ValidarDniTutor()
    {
        if (!EsMenorFormulario)
        {
            ErrorDniTutor = string.Empty;
            return;
        }

        var dni = (DniTutor ?? string.Empty).Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(dni))
        {
            ErrorDniTutor = ValidacionFormularioActiva ? "DNI del tutor obligatorio" : string.Empty;
            return;
        }

        if (EsDniNieValido(dni))
        {
            ErrorDniTutor = string.Empty;
            return;
        }

        ErrorDniTutor = dni.Length < 5
            ? "Formato DNI/NIE no válido"
            : string.Empty;
    }

    private void ValidarTelefonoTutor()
    {
        if (!EsMenorFormulario)
        {
            ErrorTelefonoTutor = string.Empty;
            return;
        }

        var t = TelefonoTutor?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(t))
        {
            ErrorTelefonoTutor = string.Empty;
            return;
        }

        ErrorTelefonoTutor = EsTelefonoValido(t)
            ? string.Empty
            : "Formato: 612345678 o +34612345678";
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

    #region Ordenación y paginación

    /// <summary>
    /// Ejecuta una acción en el hilo de UI de Avalonia (requerido tras awaits en métodos async).
    /// </summary>
    private static async Task EnUiThreadAsync(Action accion)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            accion();
            return;
        }

        await Dispatcher.UIThread.InvokeAsync(accion);
    }

    /// <summary>
    /// Asigna la lista completa aplicando el orden seleccionado y refresca la paginación.
    /// </summary>
    private void EstablecerListaClientes(IEnumerable<Cliente> clientes)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => EstablecerListaClientes(clientes));
            return;
        }

        var ordenada = OrdenarClientes(clientes).ToList();
        _todosLosClientes = new ObservableCollection<Cliente>(ordenada);
        TotalClientes = ordenada.Count;
        ActualizarPaginacion();
    }

    /// <summary>
    /// Reordena la colección en memoria sin recargar desde la BD.
    /// </summary>
    private void ReordenarClientesActuales()
    {
        var ordenada = OrdenarClientes(_todosLosClientes).ToList();
        _todosLosClientes = new ObservableCollection<Cliente>(ordenada);
        ActualizarPaginacion();
    }

    /// <summary>
    /// Aplica el criterio de ordenación activo.
    /// </summary>
    private IEnumerable<Cliente> OrdenarClientes(IEnumerable<Cliente> clientes)
    {
        var criterio = OrdenSeleccionado?.Valor ?? OrdenClientes.MasReciente;
        return criterio switch
        {
            OrdenClientes.MasReciente => clientes
                .OrderByDescending(c => c.FechaRegistro)
                .ThenByDescending(c => c.Id),
            OrdenClientes.Nombre => clientes
                .OrderBy(c => c.Nombre, StringComparer.OrdinalIgnoreCase)
                .ThenBy(c => c.Apellidos, StringComparer.OrdinalIgnoreCase),
            OrdenClientes.Edad => clientes
                .OrderByDescending(c => c.Edad ?? -1)
                .ThenBy(c => c.Nombre, StringComparer.OrdinalIgnoreCase),
            _ => clientes
                .OrderBy(c => c.FechaRegistro)
                .ThenBy(c => c.Id)
        };
    }

    /// <summary>
    /// Actualiza la lista de clientes mostrados según la página actual.
    /// </summary>
    private void ActualizarPaginacion()
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(ActualizarPaginacion);
            return;
        }

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

/// <summary>
/// Opción visible en el desplegable de ordenación de clientes.
/// </summary>
public sealed class OpcionOrdenCliente(OrdenClientes valor, string etiqueta)
{
    public OrdenClientes Valor { get; } = valor;
    public string Etiqueta { get; } = etiqueta;

    public override string ToString() => Etiqueta;
}
