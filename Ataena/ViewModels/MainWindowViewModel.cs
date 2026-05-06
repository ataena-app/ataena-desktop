using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ataena.Services;

namespace Ataena.ViewModels;

/// <summary>
/// ViewModel principal de la aplicación.
/// Gestiona la navegación entre las diferentes vistas.
/// </summary>
public partial class MainWindowViewModel : ViewModelBase
{
    #region ViewModels de cada sección

    /// <summary>
    /// ViewModel del Dashboard (pantalla principal).
    /// </summary>
    public DashboardViewModel DashboardVM { get; } = new();

    /// <summary>
    /// ViewModel de gestión de clientes.
    /// </summary>
    public ClientesViewModel ClientesVM { get; } = new();

    /// <summary>
    /// ViewModel para visualización de logs.
    /// </summary>
    public LogsViewModel LogsVM { get; } = new();

    /// <summary>
    /// ViewModel de la agenda y citas.
    /// </summary>
    public AgendaViewModel AgendaVM { get; } = new();

    /// <summary>
    /// ViewModel de gestión de trabajos.
    /// </summary>
    public TrabajosViewModel TrabajosVM { get; } = new();

    /// <summary>
    /// ViewModel de vista global de consentimientos.
    /// </summary>
    public ConsentimientosViewModel ConsentimientosVM { get; } = new();

    /// <summary>
    /// ViewModel de configuración de la aplicación.
    /// </summary>
    public ConfiguracionViewModel ConfiguracionVM { get; } = new();

    /// <summary>
    /// ViewModel de backup y restauración.
    /// </summary>
    public BackupViewModel BackupVM { get; } = new();

    /// <summary>
    /// Versión actual de la aplicación (leída del ensamblado), formateada como "v1.0.2".
    /// Se usa como etiqueta visible en la ventana principal.
    /// </summary>
    public string VersionApp
    {
        get
        {
            var v = ActualizacionService.ObtenerVersionActual();
            // El ensamblado siempre tiene 4 segmentos; mostramos solo X.Y.Z para limpieza.
            return $"v{v.Major}.{v.Minor}.{v.Build}";
        }
    }

    /// <summary>
    /// Constructor que inicializa las referencias entre ViewModels.
    /// </summary>
    public MainWindowViewModel()
    {
        // Establecer referencias cruzadas para comunicación entre ViewModels
        AgendaVM.SetTrabajosViewModel(TrabajosVM);
        AgendaVM.SetMainWindowViewModel(this);
        DashboardVM.SetMainWindowViewModel(this);
        TrabajosVM.SetClientesViewModel(ClientesVM);
        ClientesVM.SetTrabajosViewModel(TrabajosVM);
        ClientesVM.SetMainWindowViewModel(this);

        // Registrar handler para diálogos de confirmación
        DialogService.OnConfirmacionRequerida += ProcesarSolicitudConfirmacion;

        // Comprobar actualizaciones en segundo plano (no bloquea el arranque)
        _ = Task.Run(ComprobarActualizacionesAsync);
    }

    #endregion

    #region Actualizaciones

    /// <summary>
    /// Indica si hay una actualización disponible y debe mostrarse el banner.
    /// </summary>
    [ObservableProperty]
    private bool _hayActualizacionDisponible;

    /// <summary>
    /// Texto informativo de la nueva versión (p.ej. "Nueva versión 1.1.0 disponible").
    /// </summary>
    [ObservableProperty]
    private string _textoActualizacion = string.Empty;

    /// <summary>
    /// Indica si se está descargando el instalador.
    /// </summary>
    [ObservableProperty]
    private bool _descargandoActualizacion;

    /// <summary>
    /// Progreso de descarga (0..1) para mostrar barra.
    /// </summary>
    [ObservableProperty]
    private double _progresoActualizacion;

    private ActualizacionService.ResultadoComprobacion? _ultimaComprobacion;

    /// <summary>
    /// Comprueba si hay versión nueva en GitHub Releases y muestra el banner si procede.
    /// </summary>
    private async Task ComprobarActualizacionesAsync()
    {
        try
        {
            var info = await ActualizacionService.ComprobarAsync();
            _ultimaComprobacion = info;
            if (info.HayActualizacion && info.VersionDisponible is not null)
            {
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    TextoActualizacion = $"Nueva versión {info.VersionDisponible} disponible";
                    HayActualizacionDisponible = true;
                });
                Serilog.Log.Information("Actualización disponible: {Version}", info.VersionDisponible);
            }
            else
            {
                Serilog.Log.Information("Aplicación actualizada (versión {Version})", info.VersionActual);
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "Error al comprobar actualizaciones");
        }
    }

    /// <summary>
    /// Descarga el instalador y lo ejecuta. Cierra la aplicación al terminar.
    /// </summary>
    [RelayCommand]
    private async Task InstalarActualizacion()
    {
        if (_ultimaComprobacion is null || !_ultimaComprobacion.HayActualizacion)
            return;

        try
        {
            DescargandoActualizacion = true;
            ProgresoActualizacion = 0;

            var progreso = new Progress<double>(p =>
            {
                ProgresoActualizacion = p;
            });

            var ruta = await ActualizacionService.DescargarAsync(_ultimaComprobacion, progreso);

            // Mensaje claro: el usuario va a ver la app cerrarse y reabrirse sola.
            TextoActualizacion = "Instalando... La app se cerrará y volverá a abrirse en unos segundos.";
            ProgresoActualizacion = 1.0;

            ActualizacionService.EjecutarInstalador(ruta);
            // No cerramos la app aquí: Inno Setup la cerrará vía Restart Manager
            // (/CLOSEAPPLICATIONS) y la relanzará al terminar (/RESTARTAPPLICATIONS).
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Error instalando actualización");
            DescargandoActualizacion = false;
            TextoActualizacion = "Error al descargar. Inténtalo de nuevo.";
        }
    }

    /// <summary>
    /// Oculta el banner de actualización (el usuario la pospone).
    /// </summary>
    [RelayCommand]
    private void PosponerActualizacion()
    {
        HayActualizacionDisponible = false;
    }

    #endregion

    #region Diálogo de Confirmación

    /// <summary>
    /// Indica si el diálogo de confirmación está visible.
    /// </summary>
    [ObservableProperty]
    private bool _mostrarDialogoConfirmacion;

    /// <summary>
    /// Título del diálogo de confirmación.
    /// </summary>
    [ObservableProperty]
    private string _dialogoTitulo = "";

    /// <summary>
    /// Mensaje del diálogo de confirmación.
    /// </summary>
    [ObservableProperty]
    private string _dialogoMensaje = "";

    /// <summary>
    /// Texto del botón de confirmar.
    /// </summary>
    [ObservableProperty]
    private string _dialogoBotonConfirmar = "Confirmar";

    /// <summary>
    /// Indica si la acción es peligrosa (botón rojo).
    /// </summary>
    [ObservableProperty]
    private bool _dialogoEsPeligroso = true;

    // TaskCompletionSource para esperar la respuesta del usuario
    private TaskCompletionSource<bool>? _dialogoTcs;

    /// <summary>
    /// Muestra el diálogo de confirmación y espera la respuesta.
    /// </summary>
    private Task<bool> ProcesarSolicitudConfirmacion(DialogService.ConfirmacionInfo info)
    {
        DialogoTitulo = info.Titulo;
        DialogoMensaje = info.Mensaje;
        DialogoBotonConfirmar = info.BotonConfirmar;
        DialogoEsPeligroso = info.EsPeligroso;
        
        _dialogoTcs = new TaskCompletionSource<bool>();
        MostrarDialogoConfirmacion = true;
        
        return _dialogoTcs.Task;
    }

    /// <summary>
    /// El usuario confirma la acción.
    /// </summary>
    [RelayCommand]
    private void ConfirmarDialogo()
    {
        MostrarDialogoConfirmacion = false;
        _dialogoTcs?.TrySetResult(true);
    }

    /// <summary>
    /// El usuario cancela la acción.
    /// </summary>
    [RelayCommand]
    private void CancelarDialogo()
    {
        MostrarDialogoConfirmacion = false;
        _dialogoTcs?.TrySetResult(false);
    }

    #endregion

    #region Navegación

    /// <summary>
    /// Vista actualmente mostrada.
    /// Valores: "Dashboard", "Clientes", "Agenda", "Trabajos", "Configuracion"
    /// </summary>
    [ObservableProperty]
    private string _vistaActual = "Dashboard";

    /// <summary>
    /// Navega al Dashboard.
    /// </summary>
    [RelayCommand]
    private async Task IrADashboard()
    {
        VistaActual = "Dashboard";
        // Refrescar los datos del Dashboard cada vez que se navega
        await DashboardVM.CargarDatosCommand.ExecuteAsync(null);
    }

    /// <summary>
    /// Navega a la gestión de Clientes.
    /// </summary>
    [RelayCommand]
    private async Task IrAClientes()
    {
        VistaActual = "Clientes";
        // Refrescar lista de clientes al entrar en la vista
        await ClientesVM.CargarClientesCommand.ExecuteAsync(null);
    }

    /// <summary>
    /// Navega a la Agenda.
    /// </summary>
    [RelayCommand]
    private void IrAAgenda()
    {
        VistaActual = "Agenda";
        // TODO: Implementar vista de Agenda
    }

    /// <summary>
    /// Navega a Trabajos.
    /// </summary>
    [RelayCommand]
    private void IrATrabajos()
    {
        VistaActual = "Trabajos";
        // TODO: Implementar vista de Trabajos
    }

    /// <summary>
    /// Navega a Configuración.
    /// </summary>
    [RelayCommand]
    private void IrAConfiguracion()
    {
        VistaActual = "Configuracion";
        // TODO: Implementar vista de Configuración
    }

    /// <summary>
    /// Navega a la vista global de Consentimientos.
    /// </summary>
    [RelayCommand]
    private void IrAConsentimientos()
    {
        VistaActual = "Consentimientos";
    }

    /// <summary>
    /// Navega a la vista de Logs.
    /// </summary>
    [RelayCommand]
    private void IrALogs()
    {
        VistaActual = "Logs";
    }

    /// <summary>
    /// Navega a la vista de Backup y Restauración.
    /// </summary>
    [RelayCommand]
    private async Task IrABackup()
    {
        VistaActual = "Backup";
        // Cargar servicios de nube y backups al entrar
        await BackupVM.CargarServiciosNubeCommand.ExecuteAsync(null);
        await BackupVM.ActualizarListaBackupsCommand.ExecuteAsync(null);
    }

    /// <summary>
    /// Abre la carpeta de logs en el explorador de archivos.
    /// Útil para diagnosticar problemas.
    /// </summary>
    [RelayCommand]
    private void AbrirCarpetaLogs()
    {
        LoggingService.AbrirCarpetaLogs();
    }

    #endregion
}
