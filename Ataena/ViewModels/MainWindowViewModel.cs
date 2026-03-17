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
