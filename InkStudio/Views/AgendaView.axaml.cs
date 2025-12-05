using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using InkStudio.Models;
using InkStudio.ViewModels;

namespace InkStudio.Views;

/// <summary>
/// Vista para la gestión de la agenda y citas.
/// </summary>
/// <remarks>
/// Permite ver citas en calendario (día/semana/mes) y gestionar su estado.
/// </remarks>
public partial class AgendaView : UserControl
{
    /// <summary>
    /// Inicializa el componente.
    /// </summary>
    public AgendaView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Carga las citas cuando la vista se muestra.
    /// </summary>
    private async void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is AgendaViewModel vm)
        {
            await vm.CargarCitasCommand.ExecuteAsync(null);
        }
    }

    /// <summary>
    /// Maneja el click en una tarjeta de cita para seleccionarla.
    /// </summary>
    private void OnCitaClick(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border border && border.Tag is Cita cita)
        {
            if (DataContext is AgendaViewModel vm)
            {
                vm.CitaSeleccionada = cita;
                vm.EditarCitaCommand.Execute(null);
            }
        }
    }
}
