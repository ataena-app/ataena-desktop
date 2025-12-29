using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
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
                vm.VerFichaClienteCommand.ExecuteAsync(cliente);
            }
        }
    }

    /// <summary>
    /// Maneja el click en un trabajo para abrir su ficha.
    /// </summary>
    private void OnTrabajoClick(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border border && border.DataContext is Trabajo trabajo)
        {
            if (DataContext is ClientesViewModel vm)
            {
                vm.AbrirTrabajoDesdeFichaCommand.ExecuteAsync(trabajo);
            }
        }
    }

    /// <summary>
    /// Abre el popup del calendario para seleccionar la fecha de nacimiento.
    /// </summary>
    private void OnAbrirCalendarioFechaNacimiento(object? sender, RoutedEventArgs e)
    {
        if (this.FindControl<Popup>("FechaNacimientoPopup") is Popup popup)
        {
            popup.IsOpen = !popup.IsOpen;
        }
    }

    private bool _formateandoFecha = false;

    /// <summary>
    /// Formatea automáticamente la fecha mientras se escribe, añadiendo las barras "/".
    /// </summary>
    private void OnFechaNacimientoTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox textBox || DataContext is not ClientesViewModel vm)
            return;

        // Evitar bucle infinito
        if (_formateandoFecha)
            return;

        var textoActual = textBox.Text ?? string.Empty;

        // Si el texto ya tiene el formato correcto (con barras), no hacer nada
        if (textoActual.Contains("/") && textoActual.Count(c => c == '/') == 2)
        {
            // Verificar que el formato sea correcto (DD/MM/YYYY)
            var partes = textoActual.Split('/');
            if (partes.Length == 3 && partes[0].Length <= 2 && partes[1].Length <= 2 && partes[2].Length <= 4)
            {
                return; // Ya está bien formateado
            }
        }

        // Eliminar todo lo que no sean dígitos
        var soloDigitos = new string(textoActual.Where(char.IsDigit).ToArray());

        // Limitar a 8 dígitos (DDMMYYYY)
        if (soloDigitos.Length > 8)
            soloDigitos = soloDigitos.Substring(0, 8);

        // Formatear con barras automáticas
        string textoFormateado = string.Empty;
        if (soloDigitos.Length > 0)
        {
            textoFormateado = soloDigitos.Substring(0, Math.Min(2, soloDigitos.Length));
            if (soloDigitos.Length > 2)
            {
                textoFormateado += "/" + soloDigitos.Substring(2, Math.Min(2, soloDigitos.Length - 2));
            }
            if (soloDigitos.Length > 4)
            {
                textoFormateado += "/" + soloDigitos.Substring(4, Math.Min(4, soloDigitos.Length - 4));
            }
        }

        // Actualizar el texto formateado solo si es diferente
        if (vm.FechaNacimientoTexto != textoFormateado)
        {
            _formateandoFecha = true;
            var caretIndex = textBox.CaretIndex;
            vm.FechaNacimientoTexto = textoFormateado;
            
            // Mover el cursor a la posición correcta después del formateo
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                // Calcular nueva posición del cursor
                var nuevaPosicion = textoFormateado.Length;
                if (caretIndex < textoFormateado.Length)
                {
                    // Contar cuántas barras hay antes de la posición del cursor
                    var barrasAntes = textoFormateado.Substring(0, Math.Min(caretIndex, textoFormateado.Length)).Count(c => c == '/');
                    nuevaPosicion = Math.Min(caretIndex + barrasAntes, textoFormateado.Length);
                }
                textBox.CaretIndex = nuevaPosicion;
                _formateandoFecha = false;
            }, Avalonia.Threading.DispatcherPriority.Background);
        }
    }
}

