using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using InkStudio.Models;
using InkStudio.ViewModels;

namespace InkStudio.Views;

/// <summary>
/// Vista para la gestión de clientes.
/// Muestra lista de clientes y formulario de edición.
/// </summary>
public partial class ClientesView : UserControl
{
    public ClientesView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Carga los clientes cuando la vista se muestra.
    /// </summary>
    private async void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ClientesViewModel vm)
        {
            await vm.CargarClientesCommand.ExecuteAsync(null);
        }
    }

    /// <summary>
    /// Maneja el click en una tarjeta de cliente para seleccionarlo.
    /// </summary>
    private void OnClienteClick(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border border && border.Tag is Cliente cliente)
        {
            if (DataContext is ClientesViewModel vm)
            {
                vm.ClienteSeleccionado = cliente;
                vm.EditarClienteCommand.Execute(null);
            }
        }
    }
}

