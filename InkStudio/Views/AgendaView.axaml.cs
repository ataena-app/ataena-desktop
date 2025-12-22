using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Interactivity;
using InkStudio.Models;
using InkStudio.ViewModels;
using InkStudio.Converters;
using Serilog;

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
    /// Diccionario para mantener referencias a los Border de cada cita (usando ID de cita como clave).
    /// </summary>
    private readonly Dictionary<int, Border> _citaBorders = new();

    /// <summary>
    /// Se ejecuta cuando el Canvas de citas se carga o cambia de tamaño.
    /// Crea y posiciona los bloques de citas manualmente en el Canvas.
    /// </summary>
    private void OnCanvasCitasLoaded(object? sender, RoutedEventArgs e)
    {
        if (sender is Canvas canvas && DataContext is AgendaViewModel vm)
        {
            void ActualizarCitas()
            {
                if (canvas.Bounds.Width <= 0 || canvas.Bounds.Height <= 0 || vm.CitasSemana.Count == 0)
                {
                    return;
                }

                // Calcular dimensiones basadas en el tamaño real
                var anchoPorColumna = canvas.Bounds.Width / 7.0;
                var altoPorFila = canvas.Bounds.Height / 28.0;

                Serilog.Log.Information("🔍 Actualizando citas en Canvas: Canvas={Width}x{Height}, AltoPorFila={Alto}, Citas={Count}", 
                    canvas.Bounds.Width, canvas.Bounds.Height, altoPorFila, vm.CitasSemana.Count);

                // Limpiar citas antiguas que ya no existen
                var idsActuales = vm.CitasSemana.Select(c => c.Cita.Id).ToHashSet();
                var idsAEliminar = _citaBorders.Keys.Where(k => !idsActuales.Contains(k)).ToList();
                foreach (var id in idsAEliminar)
                {
                    if (_citaBorders.TryGetValue(id, out var border))
                    {
                        canvas.Children.Remove(border);
                        _citaBorders.Remove(id);
                    }
                }

                // Crear o actualizar bloques de citas
                foreach (var citaInfo in vm.CitasSemana)
                {
                    // Calcular posiciones
                    var left = citaInfo.Columna * anchoPorColumna + 2;
                    var top = citaInfo.Fila * altoPorFila;
                    var width = anchoPorColumna - 6;
                    var height = citaInfo.RowSpan * altoPorFila - 1;

                    // Actualizar propiedades de CitaSemanaInfo
                    citaInfo.Left = left;
                    citaInfo.Top = top;
                    citaInfo.Width = width;
                    citaInfo.Height = height;

                    // Obtener o crear el Border para esta cita
                    if (!_citaBorders.TryGetValue(citaInfo.Cita.Id, out var border))
                    {
                        // Crear nuevo Border
                        border = new Border
                        {
                            Background = GetColorFromEstado(citaInfo.Cita.Estado),
                            CornerRadius = new Avalonia.CornerRadius(6),
                            Padding = new Avalonia.Thickness(6, 4),
                            Margin = new Avalonia.Thickness(2, 0),
                            Opacity = 0.85,
                            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
                            Tag = citaInfo.Cita
                        };

                        border.PointerPressed += (s, args) =>
                        {
                            if (s is Border b && b.Tag is Models.Cita cita)
                            {
                                if (DataContext is AgendaViewModel vm2)
                                {
                                    vm2.CitaSeleccionada = cita;
                                    vm2.EditarCitaCommand.Execute(null);
                                }
                            }
                        };

                        var stackPanel = new StackPanel 
                        { 
                            Spacing = 2
                        };
                        stackPanel.Children.Add(new TextBlock
                        {
                            Text = citaInfo.Cita.HoraInicioFormateada,
                            FontSize = 10,
                            FontWeight = Avalonia.Media.FontWeight.Bold,
                            Foreground = Avalonia.Media.Brushes.White,
                            TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis
                        });
                        stackPanel.Children.Add(new TextBlock
                        {
                            Text = citaInfo.Cita.Cliente?.NombreCompleto ?? "Sin cliente",
                            FontSize = 11,
                            FontWeight = Avalonia.Media.FontWeight.SemiBold,
                            Foreground = Avalonia.Media.Brushes.White,
                            TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis,
                            TextWrapping = Avalonia.Media.TextWrapping.NoWrap
                        });
                        stackPanel.Children.Add(new TextBlock
                        {
                            Text = citaInfo.Cita.IconoTipo,
                            FontSize = 12,
                            Opacity = 0.9,
                            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
                            TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis
                        });

                        border.Child = stackPanel;
                        canvas.Children.Add(border);
                        _citaBorders[citaInfo.Cita.Id] = border;
                    }

                    // Actualizar posiciones y tamaño
                    Canvas.SetLeft(border, left);
                    Canvas.SetTop(border, top);
                    border.Width = width;
                    border.Height = height;

                    Serilog.Log.Debug("🔍 Cita {CitaId} actualizada: Left={Left}, Top={Top}, Width={Width}, Height={Height}, Fila={Fila}, Hora={Hora}, Columna={Columna}", 
                        citaInfo.Cita.Id, left, top, width, height, citaInfo.Fila, citaInfo.Cita.HoraInicio, citaInfo.Columna);
                }
            }

            // Suscribirse a cambios en CitasSemana (cuando se agregan/eliminan elementos)
            vm.CitasSemana.CollectionChanged += (s, args) =>
            {
                ActualizarCitas();
            };

            // Recalcular cuando cambie de tamaño
            canvas.SizeChanged += (s, args) => ActualizarCitas();

            // Actualizar inicialmente
            _ = Task.Delay(100).ContinueWith(_ =>
            {
                ActualizarCitas();
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }
    }

    /// <summary>
    /// Obtiene el color de fondo según el estado de la cita usando el converter.
    /// </summary>
    private Avalonia.Media.IBrush GetColorFromEstado(Models.EstadoCita estado)
    {
        var converter = new EstadoCitaToColorConverter();
        return converter.Convert(estado, typeof(Avalonia.Media.IBrush), null, null) as Avalonia.Media.IBrush 
               ?? Avalonia.Media.Brushes.Gray;
    }
}
