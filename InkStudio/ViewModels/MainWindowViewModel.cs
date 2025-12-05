using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InkStudio.Services;

namespace InkStudio.ViewModels;

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

    // TODO: Añadir estos ViewModels cuando se creen las vistas
    // public TrabajosViewModel TrabajosVM { get; } = new();
    // public ConfiguracionViewModel ConfiguracionVM { get; } = new();

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
    private void IrADashboard()
    {
        VistaActual = "Dashboard";
    }

    /// <summary>
    /// Navega a la gestión de Clientes.
    /// </summary>
    [RelayCommand]
    private void IrAClientes()
    {
        VistaActual = "Clientes";
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
    /// Navega a la vista de Logs.
    /// </summary>
    [RelayCommand]
    private void IrALogs()
    {
        VistaActual = "Logs";
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
