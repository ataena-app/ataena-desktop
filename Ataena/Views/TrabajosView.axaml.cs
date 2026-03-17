using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Ataena.Models;
using Ataena.ViewModels;

namespace Ataena.Views;

/// <summary>
/// Vista para la gestión de trabajos (tatuajes y piercings).
/// </summary>
public partial class TrabajosView : UserControl
{
    public TrabajosView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Se ejecuta cuando se carga el control.
    /// </summary>
    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is TrabajosViewModel vm)
        {
            _ = vm.CargarTrabajosCommand.ExecuteAsync(null);
        }
    }

    /// <summary>
    /// Maneja el clic en una tarjeta de trabajo.
    /// </summary>
    private void OnTrabajoClick(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (sender is Border border && border.Tag is Trabajo trabajo)
        {
            if (DataContext is TrabajosViewModel vm)
            {
                vm.TrabajoSeleccionado = trabajo;
                vm.EditarTrabajoCommand.Execute(null);
            }
        }
    }

    /// <summary>
    /// Maneja el clic en el botón de firmar consentimiento desde el DataTemplate.
    /// </summary>
    private void OnFirmarConsentimientoClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is Trabajo trabajo)
        {
            if (DataContext is TrabajosViewModel vm)
            {
                vm.FirmarConsentimientoTrabajoCommand.Execute(trabajo);
            }
        }
    }
}

