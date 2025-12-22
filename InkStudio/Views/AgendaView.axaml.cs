using System.Threading.Tasks;
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
    
    /// <summary>
    /// Maneja el clic en el backdrop del modal para cerrarlo.
    /// </summary>
    private void OnModalBackdropClick(object? sender, PointerPressedEventArgs e)
    {
        // Solo cerrar si se hizo clic directamente en el backdrop (no en el modal)
        if (sender is Border backdrop && e.Source == backdrop)
        {
            if (DataContext is AgendaViewModel vm)
            {
                vm.CancelarEdicionCommand.Execute(null);
            }
        }
    }

    /// <summary>
    /// Maneja el click en un bloque de cita en el calendario semanal.
    /// </summary>
    private void OnCitaCalendarioClick(object? sender, PointerPressedEventArgs e)
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

    /// <summary>
    /// Se ejecuta cuando el Canvas de citas se carga o cambia de tamaño.
    /// Recalcula las posiciones de las citas basándose en el tamaño real del Canvas.
    /// </summary>
    private void OnCanvasCitasLoaded(object? sender, RoutedEventArgs e)
    {
        if (sender is Canvas canvas && DataContext is AgendaViewModel vm)
        {
            // Recalcular posiciones cuando el Canvas cambie de tamaño
            canvas.SizeChanged += (s, args) =>
            {
                if (args.NewSize.Width > 0 && args.NewSize.Height > 0 && vm.CitasSemana.Count > 0)
                {
                    // Calcular ancho por columna basado en el tamaño real
                    var anchoPorColumna = args.NewSize.Width / 7.0;
                    var altoPorFila = 32.0; // Fijo según el diseño
                    
                    // Actualizar posiciones de cada cita (las propiedades notifican cambios automáticamente)
                    foreach (var citaInfo in vm.CitasSemana)
                    {
                        citaInfo.Left = citaInfo.Columna * anchoPorColumna + 2;
                        citaInfo.Top = citaInfo.Fila * altoPorFila + 1;
                        citaInfo.Width = anchoPorColumna - 6;
                        citaInfo.Height = citaInfo.RowSpan * altoPorFila - 2;
                    }
                }
            };
            
            // También recalcular cuando se carga inicialmente (usando un pequeño delay para asegurar que el Canvas tenga tamaño)
            _ = Task.Delay(100).ContinueWith(_ =>
            {
                if (canvas.Bounds.Width > 0 && canvas.Bounds.Height > 0 && vm.CitasSemana.Count > 0)
                {
                    var anchoPorColumna = canvas.Bounds.Width / 7.0;
                    var altoPorFila = 32.0;
                    
                    foreach (var citaInfo in vm.CitasSemana)
                    {
                        citaInfo.Left = citaInfo.Columna * anchoPorColumna + 2;
                        citaInfo.Top = citaInfo.Fila * altoPorFila + 1;
                        citaInfo.Width = anchoPorColumna - 6;
                        citaInfo.Height = citaInfo.RowSpan * altoPorFila - 2;
                    }
                }
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }
    }
}
