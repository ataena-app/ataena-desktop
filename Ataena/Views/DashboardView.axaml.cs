using Avalonia.Controls;
using Avalonia.Interactivity;
using Ataena.ViewModels;

namespace Ataena.Views;

/// <summary>
/// Code-behind para la vista del Dashboard.
/// </summary>
/// <remarks>
/// El Dashboard muestra:
/// - Citas del día
/// - Estadísticas rápidas
/// - Alertas y pendientes
/// - Acciones rápidas
/// </remarks>
public partial class DashboardView : UserControl
{
    /// <summary>
    /// Inicializa el componente.
    /// </summary>
    public DashboardView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Manejador del evento Loaded.
    /// Carga los datos del Dashboard al mostrar la vista.
    /// </summary>
    /// <param name="sender">Origen del evento.</param>
    /// <param name="e">Argumentos del evento.</param>
    private async void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is DashboardViewModel vm)
        {
            await vm.CargarDatosCommand.ExecuteAsync(null);
        }
    }
}
