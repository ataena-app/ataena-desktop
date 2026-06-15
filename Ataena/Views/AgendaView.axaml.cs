using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Ataena.Models;
using Ataena.ViewModels;
using Ataena.Converters;
using Serilog;

namespace Ataena.Views;

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
    /// Cierra el popup del calendario al elegir un día.
    /// </summary>
    private void OnFechaCitaCalendarSelectedDatesChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is AgendaViewModel vm && e.AddedItems.Count > 0)
            vm.MostrarCalendarioCitaExpandido = false;

        if (sender is Calendar calendar)
            AplicarEstilosCalendarioCita(calendar);
    }

    /// <summary>
    /// Aplica estilos visuales (fines de semana) al calendario del formulario de cita.
    /// </summary>
    private void OnFechaCitaCalendarLoaded(object? sender, RoutedEventArgs e)
    {
        if (sender is not Calendar calendar)
            return;

        calendar.PropertyChanged += (_, args) =>
        {
            if (args.Property.Name == nameof(Calendar.DisplayDate))
                AplicarEstilosCalendarioCita(calendar);
        };

        AplicarEstilosCalendarioCita(calendar);
    }

    private static void AplicarEstilosCalendarioCita(Calendar calendar)
    {
        try
        {
            var dayButtons = new List<CalendarDayButton>();
            BuscarCalendarDayButtons(calendar, dayButtons);

            foreach (var button in dayButtons)
            {
                button.Classes.Remove("weekend");
                button.Classes.Remove("other-month");

                if (button.DataContext is DateTime date)
                {
                    if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                        button.Classes.Add("weekend");

                    if (calendar.DisplayDate.Month != date.Month)
                        button.Classes.Add("other-month");
                }
            }
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "No se pudieron aplicar estilos al calendario de cita");
        }
    }

    private static void BuscarCalendarDayButtons(Control? parent, List<CalendarDayButton> dayButtons)
    {
        if (parent == null)
            return;

        if (parent is CalendarDayButton dayButton)
        {
            dayButtons.Add(dayButton);
            return;
        }

        if (parent is Panel panel)
        {
            foreach (var child in panel.Children)
            {
                if (child is Control childControl)
                    BuscarCalendarDayButtons(childControl, dayButtons);
            }
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
    /// Estado del arrastre de citas.
    /// </summary>
    private Border? _citaArrastrandose;
    private Models.Cita? _citaOriginal;
    private Avalonia.Point _offsetArrastre;
    private Avalonia.Point _posicionInicialArrastre;
    private bool _estaArrastrando;
    private Canvas? _canvasCitas;
    private AgendaViewModel? _viewModel;
    
    /// <summary>
    /// Estado del redimensionado de citas.
    /// </summary>
    private Border? _citaRedimensionandose;
    private Models.Cita? _citaRedimensionOriginal;
    private double _alturaInicialCita;
    private double _topInicialCita;
    private int _duracionInicialMinutos;
    private bool _estaRedimensionando;
    
    /// <summary>
    /// Indicador visual (fantasma) que muestra dónde se colocará la cita al arrastrarla.
    /// </summary>
    private Border? _indicadorFantasma;
    
    /// <summary>
    /// Distancia mínima en píxeles que debe moverse el mouse para considerar que es un arrastre (no un click).
    /// </summary>
    private const double DistanciaMinimaArrastre = 5.0;
    
    /// <summary>
    /// Altura en píxeles del área del borde inferior que permite redimensionar (zona sensible).
    /// </summary>
    private const double AlturaZonaRedimensionado = 10.0;

    // Estado para creación de nuevas citas arrastrando sobre el calendario
    private bool _estaCreandoNuevaCita = false;
    private Avalonia.Point _posicionInicioCreacion;
    private Border? _indicadorCreacionNueva;

    /// <summary>
    /// Se ejecuta cuando el Canvas de citas se carga o cambia de tamaño.
    /// Crea y posiciona los bloques de citas manualmente en el Canvas.
    /// </summary>
    private void OnCanvasCitasLoaded(object? sender, RoutedEventArgs e)
    {
        if (sender is Canvas canvas && DataContext is AgendaViewModel vm)
        {
            // Guardar referencias para el arrastre
            _canvasCitas = canvas;
            _viewModel = vm;
            
            // Suscribirse a eventos del Canvas para creación, arrastre y redimensionado
            canvas.PointerPressed += OnCanvasPointerPressed;
            canvas.PointerMoved += OnCanvasPointerMoved;
            canvas.PointerReleased += OnCanvasPointerReleased;
            void ActualizarCitas()
            {
                // Si el canvas aún no tiene tamaño válido, no podemos calcular posiciones.
                // Pero aunque no haya citas en CitasSemana, debemos permitir limpiar las antiguas.
                if (canvas.Bounds.Width <= 0 || canvas.Bounds.Height <= 0)
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
                    
                    // Si hay citas superpuestas, ajustar ancho y posición para apilarlas horizontalmente
                    var width = anchoPorColumna - 6;
                    if (citaInfo.TieneSuperposiciones && citaInfo.MaxSuperposicionesSimultaneas > 1)
                    {
                        // Dividir el ancho entre el número máximo de citas superpuestas simultáneamente
                        var maxSuperposiciones = citaInfo.MaxSuperposicionesSimultaneas;
                        var anchoDisponible = anchoPorColumna - 6;
                        var anchoPorCita = anchoDisponible / maxSuperposiciones;
                        var espacioEntreCitas = 2.0; // Espacio entre citas superpuestas
                        
                        width = anchoPorCita - espacioEntreCitas;
                        
                        // Ajustar left para posicionar horizontalmente (lado a lado)
                        // El índice 0 va a la izquierda, índice 1 a la derecha, etc.
                        left = citaInfo.Columna * anchoPorColumna + 2 + (citaInfo.IndiceEnGrupo * (anchoPorCita));
                        
                        Serilog.Log.Debug("📐 Cita {CitaId}: TieneSuperposiciones={Tiene}, Índice={Indice}, MaxSuperposiciones={Max}, Width={Width}, Left={Left}, Columna={Col}", 
                            citaInfo.Cita.Id, citaInfo.TieneSuperposiciones, citaInfo.IndiceEnGrupo, maxSuperposiciones, width, left, citaInfo.Columna);
                    }
                    else
                    {
                        Serilog.Log.Debug("📐 Cita {CitaId}: Sin superposiciones o MaxSuperposiciones={Max}, Width={Width}, Left={Left}", 
                            citaInfo.Cita.Id, citaInfo.MaxSuperposicionesSimultaneas, width, left);
                    }
                    
                    var height = citaInfo.RowSpan * altoPorFila - 1;

                    // Actualizar propiedades de CitaSemanaInfo
                    citaInfo.Left = left;
                    citaInfo.Top = top;
                    citaInfo.Width = width;
                    citaInfo.Height = height;

                    // Determinar si es una cita pequeña (media hora o menos)
                    var esCitaPequena = height < 40; // Menos de ~40px de altura

                    // Obtener o crear el Border para esta cita
                    if (!_citaBorders.TryGetValue(citaInfo.Cita.Id, out var border))
                    {
                        // Crear nuevo Border
                        border = new Border
                        {
                            Background = GetColorFromCita(citaInfo.Cita),
                            CornerRadius = new Avalonia.CornerRadius(6),
                            Padding = esCitaPequena ? new Avalonia.Thickness(4, 2) : new Avalonia.Thickness(6, 4),
                            Margin = new Avalonia.Thickness(2, 0),
                            Opacity = 0.85,
                            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
                            Tag = citaInfo.Cita,
                            ClipToBounds = true // Asegurar que el contenido no se salga del Border
                        };

                        // Handler para iniciar arrastre o redimensionado
                        border.PointerPressed += OnCitaPointerPressed;
                        
                        // Handler para detectar cuando el mouse está sobre el borde inferior (redimensionado)
                        border.PointerMoved += OnCitaPointerMovedParaRedimensionado;

                        var stackPanel = new StackPanel 
                        { 
                            Spacing = esCitaPequena ? 1 : 2
                        };
                        
                        // Hora
                        stackPanel.Children.Add(new TextBlock
                        {
                            Text = citaInfo.Cita.HoraInicioFormateada,
                            FontSize = esCitaPequena ? 9 : 10,
                            FontWeight = Avalonia.Media.FontWeight.Bold,
                            Foreground = Avalonia.Media.Brushes.White,
                            TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis
                        });
                        
                        // Nombre del cliente (solo si hay espacio suficiente)
                        if (!esCitaPequena)
                        {
                            stackPanel.Children.Add(new TextBlock
                            {
                                Text = citaInfo.Cita.Cliente?.NombreCompleto ?? "Sin cliente",
                                FontSize = 11,
                                FontWeight = Avalonia.Media.FontWeight.SemiBold,
                                Foreground = Avalonia.Media.Brushes.White,
                                TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis,
                                TextWrapping = Avalonia.Media.TextWrapping.NoWrap
                            });
                        }
                        else
                        {
                            // Para citas pequeñas, mostrar solo el nombre corto o iniciales
                            var nombreCorto = citaInfo.Cita.Cliente?.NombreCompleto?.Split(' ').FirstOrDefault() ?? "Sin cliente";
                            stackPanel.Children.Add(new TextBlock
                            {
                                Text = nombreCorto,
                                FontSize = 9,
                                FontWeight = Avalonia.Media.FontWeight.SemiBold,
                                Foreground = Avalonia.Media.Brushes.White,
                                TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis,
                                TextWrapping = Avalonia.Media.TextWrapping.NoWrap
                            });
                        }
                        
                        // Icono (más pequeño para citas pequeñas)
                        stackPanel.Children.Add(new TextBlock
                        {
                            Text = citaInfo.Cita.IconoTipo,
                            FontSize = esCitaPequena ? 10 : 12,
                            Opacity = 0.9,
                            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
                            TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis
                        });

                        // Crear un contenedor Grid para el contenido y el indicador de redimensionado
                        var gridContenedor = new Grid
                        {
                            RowDefinitions = new RowDefinitions("*,Auto")
                        };
                        
                        // Contenido principal
                        Grid.SetRow(stackPanel, 0);
                        gridContenedor.Children.Add(stackPanel);

                        // Indicador de warning si el cliente no tiene RGPD firmado
                        var clienteSinRGPD = citaInfo.Cita.Cliente != null && !citaInfo.Cita.Cliente.TieneConsentimientoRGPD;
                        if (clienteSinRGPD)
                        {
                            var warningIcon = new TextBlock
                            {
                                Text = "⚠️",
                                FontSize = esCitaPequena ? 10 : 14,
                                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
                                Margin = new Avalonia.Thickness(0, -2, 2, 0),
                                Tag = "WarningRGPD"
                            };
                            ToolTip.SetTip(warningIcon, "⚠️ Cliente sin RGPD firmado");
                            Grid.SetRow(warningIcon, 0);
                            gridContenedor.Children.Add(warningIcon);
                        }

                        // Indicador de menor de edad
                        var esMenorDeEdad = citaInfo.Cita.Cliente != null && citaInfo.Cita.Cliente.EsMenorDeEdad;
                        if (esMenorDeEdad)
                        {
                            var menorIcon = new TextBlock
                            {
                                Text = "🔞",
                                FontSize = esCitaPequena ? 10 : 12,
                                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
                                Margin = new Avalonia.Thickness(0, -2, clienteSinRGPD ? 18 : 2, 0),
                                Tag = "MenorDeEdad"
                            };
                            ToolTip.SetTip(menorIcon, "🔞 Cliente menor de edad");
                            Grid.SetRow(menorIcon, 0);
                            gridContenedor.Children.Add(menorIcon);
                        }

                        // La barra de consentimiento (si falta) se añade/actualiza siempre más abajo,
                        // también para Borders ya existentes.
                        
                        // Eliminado el badge visual de superposiciones (círculo + número) por no aportar utilidad práctica.
                        
                        // Indicador visual del borde inferior (zona de redimensionado)
                        var indicadorBorde = new Border
                        {
                            Height = 4,
                            Background = Avalonia.Media.Brushes.White,
                            Opacity = 0.3,
                            CornerRadius = new Avalonia.CornerRadius(0, 0, 6, 6),
                            Name = "IndicadorBordeInferior"
                        };
                        Grid.SetRow(indicadorBorde, 1);
                        gridContenedor.Children.Add(indicadorBorde);
                        
                        border.Child = gridContenedor;
                        canvas.Children.Add(border);
                        _citaBorders[citaInfo.Cita.Id] = border;
                    }
                    else
                    {
                        // Actualizar el contenido si la cita ya existe pero cambió de tamaño
                        // También actualizar el badge de superposiciones si es necesario
                        if (border.Child is Grid gridContenedorExistente)
                        {
                            // Actualizar padding del border
                            border.Padding = esCitaPequena ? new Avalonia.Thickness(4, 2) : new Avalonia.Thickness(6, 4);
                            
                            // Buscar el StackPanel dentro del Grid
                            if (gridContenedorExistente.Children[0] is StackPanel existingPanel)
                            {
                                // Actualizar spacing del panel
                                existingPanel.Spacing = esCitaPequena ? 1 : 2;
                                
                                // Actualizar tamaños de fuente si es necesario
                                if (existingPanel.Children.Count >= 3)
                                {
                                    if (existingPanel.Children[0] is TextBlock horaBlock)
                                    {
                                        horaBlock.Text = citaInfo.Cita.HoraInicioFormateada;
                                        horaBlock.FontSize = esCitaPequena ? 9 : 10;
                                    }
                                    if (existingPanel.Children[1] is TextBlock nombreBlock)
                                    {
                                        nombreBlock.FontSize = esCitaPequena ? 9 : 11;
                                        if (esCitaPequena)
                                        {
                                            var nombreCorto = citaInfo.Cita.Cliente?.NombreCompleto?.Split(' ').FirstOrDefault() ?? "Sin cliente";
                                            nombreBlock.Text = nombreCorto;
                                        }
                                        else
                                        {
                                            nombreBlock.Text = citaInfo.Cita.Cliente?.NombreCompleto ?? "Sin cliente";
                                        }
                                    }
                                    if (existingPanel.Children[2] is TextBlock iconoBlock)
                                    {
                                        iconoBlock.FontSize = esCitaPequena ? 10 : 12;
                                    }
                                }
                            }
                            
                        // Eliminado el badge de superposiciones (círculo + número) por no aportar utilidad práctica.
                        }
                    }

                    // Actualizar posiciones y tamaño SIEMPRE, incluso si el Border ya existe
                    // Esto asegura que las posiciones se actualicen cuando se mueven citas
                    Canvas.SetLeft(border, left);
                    Canvas.SetTop(border, top);
                    border.Width = width;
                    border.Height = height;
                    
                    // Actualizar el color de fondo según el tipo de cita/trabajo
                    border.Background = GetColorFromCita(citaInfo.Cita);

                    // Actualizar la barra de falta de consentimiento de trabajo
                    // (se muestra solo si hay trabajo asociado y NO tiene consentimiento firmado)
                    if (border.Child is Grid gridContenedorParaConsentimiento)
                    {
                        // Buscar una barra existente marcada con Tag específico
                        var barraExistente = gridContenedorParaConsentimiento.Children
                            .OfType<Border>()
                            .FirstOrDefault(b => Equals(b.Tag, "BarraConsentimientoTrabajo"));

                        var trabajo = citaInfo.Cita.Trabajo;
                        var debeMostrarBarra = trabajo != null && !trabajo.TieneConsentimiento;

                        if (debeMostrarBarra && barraExistente == null)
                        {
                            // Crear y añadir la barra si no existía
                            var nuevaBarra = new Border
                            {
                                Background = Avalonia.Media.Brushes.Orange,
                                Height = 3,
                                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
                                CornerRadius = new Avalonia.CornerRadius(6, 6, 0, 0),
                                Tag = "BarraConsentimientoTrabajo"
                            };
                            Grid.SetRow(nuevaBarra, 0);
                            gridContenedorParaConsentimiento.Children.Add(nuevaBarra);
                        }
                        else if (!debeMostrarBarra && barraExistente != null)
                        {
                            // Quitar la barra si ya no hace falta (porque ahora tiene consentimiento o se ha quitado el trabajo)
                            gridContenedorParaConsentimiento.Children.Remove(barraExistente);
                        }

                        // Actualizar el indicador de warning si el cliente no tiene RGPD firmado
                        var warningExistente = gridContenedorParaConsentimiento.Children
                            .OfType<TextBlock>()
                            .FirstOrDefault(t => Equals(t.Tag, "WarningRGPD"));

                        var clienteSinRGPD = citaInfo.Cita.Cliente != null && !citaInfo.Cita.Cliente.TieneConsentimientoRGPD;

                        if (clienteSinRGPD && warningExistente == null)
                        {
                            // Añadir warning si no existía
                            var warningIcon = new TextBlock
                            {
                                Text = "⚠️",
                                FontSize = esCitaPequena ? 10 : 14,
                                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
                                Margin = new Avalonia.Thickness(0, -2, 2, 0),
                                Tag = "WarningRGPD"
                            };
                            ToolTip.SetTip(warningIcon, "⚠️ Cliente sin RGPD firmado");
                            Grid.SetRow(warningIcon, 0);
                            gridContenedorParaConsentimiento.Children.Add(warningIcon);
                        }
                        else if (!clienteSinRGPD && warningExistente != null)
                        {
                            // Quitar warning si el cliente ya tiene RGPD firmado
                            gridContenedorParaConsentimiento.Children.Remove(warningExistente);
                        }

                        // Actualizar indicador de menor de edad
                        var menorExistente = gridContenedorParaConsentimiento.Children.OfType<TextBlock>()
                            .FirstOrDefault(tb => tb.Tag as string == "MenorDeEdad");
                        var esMenorDeEdad = citaInfo.Cita.Cliente != null && citaInfo.Cita.Cliente.EsMenorDeEdad;
                        
                        if (esMenorDeEdad && menorExistente == null)
                        {
                            var menorIcon = new TextBlock
                            {
                                Text = "🔞",
                                FontSize = esCitaPequena ? 10 : 12,
                                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
                                Margin = new Avalonia.Thickness(0, -2, clienteSinRGPD ? 18 : 2, 0),
                                Tag = "MenorDeEdad"
                            };
                            ToolTip.SetTip(menorIcon, "🔞 Cliente menor de edad");
                            Grid.SetRow(menorIcon, 0);
                            gridContenedorParaConsentimiento.Children.Add(menorIcon);
                        }
                        else if (!esMenorDeEdad && menorExistente != null)
                        {
                            gridContenedorParaConsentimiento.Children.Remove(menorExistente);
                        }
                    }

                    Serilog.Log.Debug("🔍 Cita {CitaId} actualizada: Left={Left}, Top={Top}, Width={Width}, Height={Height}, Fila={Fila}, Hora={Hora}, Columna={Columna}, Índice={Indice}, MaxSuperposiciones={Max}", 
                        citaInfo.Cita.Id, left, top, width, height, citaInfo.Fila, citaInfo.Cita.HoraInicio, citaInfo.Columna, citaInfo.IndiceEnGrupo, citaInfo.MaxSuperposicionesSimultaneas);
                }
            }

            // Suscribirse a cambios en CitasSemana (cuando se agregan/eliminan elementos)
            vm.CitasSemana.CollectionChanged += (s, args) =>
            {
                Serilog.Log.Debug("📋 CitasSemana.CollectionChanged: Action={Action}, NewItems={NewCount}, OldItems={OldCount}", 
                    args.Action, args.NewItems?.Count ?? 0, args.OldItems?.Count ?? 0);
                
                // Suscribirse a cambios en propiedades de las nuevas citas
                if (args.NewItems != null)
                {
                    foreach (var item in args.NewItems)
                    {
                        if (item is AgendaViewModel.CitaSemanaInfo citaInfo)
                        {
                            citaInfo.PropertyChanged += (sender, propArgs) =>
                            {
                                if (propArgs.PropertyName == nameof(AgendaViewModel.CitaSemanaInfo.Left) ||
                                    propArgs.PropertyName == nameof(AgendaViewModel.CitaSemanaInfo.Top) ||
                                    propArgs.PropertyName == nameof(AgendaViewModel.CitaSemanaInfo.Width) ||
                                    propArgs.PropertyName == nameof(AgendaViewModel.CitaSemanaInfo.Height) ||
                                    propArgs.PropertyName == nameof(AgendaViewModel.CitaSemanaInfo.IndiceEnGrupo) ||
                                    propArgs.PropertyName == nameof(AgendaViewModel.CitaSemanaInfo.MaxSuperposicionesSimultaneas) ||
                                    propArgs.PropertyName == nameof(AgendaViewModel.CitaSemanaInfo.CitasSuperpuestas))
                                {
                                    Serilog.Log.Debug("🔄 Propiedad {Prop} cambió en cita {CitaId}, recalculando posiciones", 
                                        propArgs.PropertyName, citaInfo.Cita.Id);
                                    // Usar Dispatcher para ejecutar después de que todas las propiedades estén actualizadas
                                    Avalonia.Threading.Dispatcher.UIThread.Post(() => ActualizarCitas(), Avalonia.Threading.DispatcherPriority.Background);
                                }
                            };
                        }
                    }
                }
                
                // Retrasar la actualización para asegurar que todas las propiedades estén actualizadas
                // Esto es especialmente importante cuando se mueve una cita
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    ActualizarCitas();
                }, Avalonia.Threading.DispatcherPriority.Background);
            };
            
            // Suscribirse a cambios en las propiedades de CitaSemanaInfo existentes
            foreach (var citaInfo in vm.CitasSemana)
            {
                citaInfo.PropertyChanged += (s, args) =>
                {
                    if (args.PropertyName == nameof(AgendaViewModel.CitaSemanaInfo.Left) ||
                        args.PropertyName == nameof(AgendaViewModel.CitaSemanaInfo.Top) ||
                        args.PropertyName == nameof(AgendaViewModel.CitaSemanaInfo.Width) ||
                        args.PropertyName == nameof(AgendaViewModel.CitaSemanaInfo.Height) ||
                        args.PropertyName == nameof(AgendaViewModel.CitaSemanaInfo.IndiceEnGrupo) ||
                        args.PropertyName == nameof(AgendaViewModel.CitaSemanaInfo.MaxSuperposicionesSimultaneas) ||
                        args.PropertyName == nameof(AgendaViewModel.CitaSemanaInfo.CitasSuperpuestas))
                    {
                        Serilog.Log.Debug("🔄 Propiedad {Prop} cambió en cita {CitaId}, recalculando posiciones", 
                            args.PropertyName, citaInfo.Cita.Id);
                        // Usar Dispatcher para ejecutar después de que todas las propiedades estén actualizadas
                        Avalonia.Threading.Dispatcher.UIThread.Post(() => ActualizarCitas(), Avalonia.Threading.DispatcherPriority.Background);
                    }
                };
            }

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
    /// Obtiene el color de fondo de una cita en la vista semanal.
    /// - Verde para tatuajes
    /// - Azul para piercings
    /// - Otros tipos (consulta, retoque) en morado suave
    /// Si no hay trabajo asociado, se usa el TipoCita como referencia.
    /// </summary>
    private Avalonia.Media.IBrush GetColorFromCita(Models.Cita cita)
    {
        // Si hay trabajo asociado, usamos el tipo de trabajo
        if (cita.Trabajo != null)
        {
            return cita.Trabajo.Tipo switch
            {
                Models.TipoTrabajo.Tatuaje => new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#10b981")), // Verde
                Models.TipoTrabajo.Piercing => new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#3b82f6")), // Azul
                _ => new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#6366f1")) // Morado por defecto
            };
        }

        // Si no hay trabajo, usamos el tipo de cita como aproximación
        return cita.TipoCita switch
        {
            Models.TipoCita.Tatuaje => new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#10b981")), // Verde
            Models.TipoCita.Piercing => new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#3b82f6")), // Azul
            _ => new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#6366f1")) // Morado por defecto
        };
    }

    /// <summary>
    /// Se ejecuta cuando se mueve el mouse sobre una cita. Detecta si está sobre el borde inferior para redimensionado.
    /// </summary>
    private void OnCitaPointerMovedParaRedimensionado(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        if (sender is not Border border || _estaArrastrando || _estaRedimensionando)
        {
            return;
        }

        // Obtener posición del mouse relativa al Border
        var posicionEnBorder = e.GetPosition(border);
        
        // Obtener altura actual del Border (usar Height si está disponible, sino Bounds.Height)
        var alturaBorder = border.Height > 0 ? border.Height : border.Bounds.Height;
        
        // Si la altura no está disponible, usar un valor por defecto
        if (alturaBorder <= 0)
        {
            alturaBorder = 60; // Valor por defecto
        }
        
        // Verificar si el mouse está cerca del borde inferior (zona de redimensionado)
        // La posición Y aumenta hacia abajo, así que el borde inferior está en alturaBorder
        var distanciaAlBordeInferior = alturaBorder - posicionEnBorder.Y;
        
        Serilog.Log.Debug("📏 Mouse sobre cita: Y={Y}, Altura={Altura}, DistanciaAlBorde={Distancia}", 
            posicionEnBorder.Y, alturaBorder, distanciaAlBordeInferior);
        
        if (distanciaAlBordeInferior <= AlturaZonaRedimensionado && distanciaAlBordeInferior >= 0)
        {
            // Cambiar cursor a SizeNS (redimensionado vertical)
            border.Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.SizeNorthSouth);
            Serilog.Log.Debug("📏 Cursor cambiado a SizeNS");
        }
        else
        {
            // Restaurar cursor normal
            border.Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand);
        }
    }

    /// <summary>
    /// Se ejecuta cuando se presiona el mouse sobre una cita. Inicia el arrastre o redimensionado.
    /// Si hay citas superpuestas y es click derecho, muestra un menú para seleccionar.
    /// </summary>
    private void OnCitaPointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (sender is not Border border || border.Tag is not Models.Cita cita || _canvasCitas == null || _viewModel == null)
        {
            return;
        }

        // Buscar la información de la cita en CitasSemana para verificar superposiciones
        var citaInfo = _viewModel.CitasSemana.FirstOrDefault(c => c.Cita.Id == cita.Id);
        
        // Si hay citas superpuestas y es click derecho, mostrar menú de selección
        // Si es click izquierdo, permitir arrastre normal
        if (citaInfo != null && citaInfo.TieneSuperposiciones && 
            e.GetCurrentPoint(border).Properties.IsRightButtonPressed)
        {
            MostrarMenuCitasSuperpuestas(citaInfo, e);
            return;
        }

        // Obtener posición del mouse relativa al Border
        var posicionEnBorder = e.GetPosition(border);
        var alturaBorder = border.Height > 0 ? border.Height : border.Bounds.Height;
        if (alturaBorder <= 0) alturaBorder = 60; // Valor por defecto
        
        var distanciaAlBordeInferior = alturaBorder - posicionEnBorder.Y;
        
        Serilog.Log.Debug("🖱️ Presionado sobre cita {CitaId}: Y={Y}, Altura={Altura}, DistanciaAlBorde={Distancia}", 
            cita.Id, posicionEnBorder.Y, alturaBorder, distanciaAlBordeInferior);
        
        // Verificar si se presionó sobre el borde inferior (zona de redimensionado)
        // Usar una zona más amplia (10px) para facilitar el click
        if (distanciaAlBordeInferior <= AlturaZonaRedimensionado && distanciaAlBordeInferior >= 0)
        {
            // Iniciar redimensionado
            e.Pointer.Capture(border);
            _citaRedimensionandose = border;
            _citaRedimensionOriginal = cita;
            _estaRedimensionando = true;
            _alturaInicialCita = border.Height;
            _topInicialCita = Canvas.GetTop(border);
            _duracionInicialMinutos = cita.DuracionMinutos;
            
            // Cambiar cursor inmediatamente
            border.Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.SizeNorthSouth);
            
            Serilog.Log.Debug("📏 Iniciando redimensionado de cita {CitaId}", cita.Id);
            return;
        }

        // Si no es redimensionado, iniciar arrastre normal
        // Asegurar que no estamos en modo redimensionado
        if (_estaRedimensionando)
        {
            Serilog.Log.Warning("⚠️ Intento de arrastre mientras se está redimensionando, ignorando");
            return;
        }
        
        // Capturar el puntero para recibir eventos incluso fuera del Border
        e.Pointer.Capture(border);
        
        // Guardar estado del arrastre
        _citaArrastrandose = border;
        _citaOriginal = cita;
        _estaArrastrando = false;
        
        // Calcular offset desde el CENTRO del Border hasta el punto donde se presionó
        var posicionPresion = e.GetPosition(_canvasCitas);
        _posicionInicialArrastre = posicionPresion;
        var leftActual = Canvas.GetLeft(border);
        var topActual = Canvas.GetTop(border);
        var anchoCita = border.Width;
        var altoCita = border.Height;
        
        // Offset desde el centro de la cita (no desde la esquina)
        _offsetArrastre = new Avalonia.Point(
            posicionPresion.X - (leftActual + anchoCita / 2),
            posicionPresion.Y - (topActual + altoCita / 2)
        );
        
        // Crear indicador fantasma (sombra) que muestra dónde se colocará la cita
        _indicadorFantasma = new Border
        {
            Background = GetColorFromCita(cita),
            CornerRadius = new Avalonia.CornerRadius(6),
            Opacity = 0.3,
            BorderBrush = Avalonia.Media.Brushes.White,
            BorderThickness = new Avalonia.Thickness(2),
            Width = anchoCita,
            Height = altoCita,
            IsHitTestVisible = false // No interceptar eventos del mouse
        };
        Canvas.SetLeft(_indicadorFantasma, leftActual);
        Canvas.SetTop(_indicadorFantasma, topActual);
        _indicadorFantasma.ZIndex = 5; // Debajo de la cita que se arrastra pero visible
        _canvasCitas.Children.Add(_indicadorFantasma);
        
        Serilog.Log.Debug("🖱️ Presionado sobre cita {CitaId} - Iniciando arrastre (offset desde centro)", cita.Id);
    }

    /// <summary>
    /// Se ejecuta cuando se presiona el mouse sobre el Canvas.
    /// Permite iniciar la creación de una nueva cita arrastrando sobre un hueco vacío.
    /// </summary>
    private void OnCanvasPointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (_canvasCitas == null || _viewModel == null)
            return;

        // Si se ha hecho click sobre una cita existente (cualquier parte del Border),
        // NO iniciar creación de nueva cita: dejamos que actúe la lógica de esa cita.
        var posicionEnCanvas = e.GetPosition(_canvasCitas);
        foreach (var kvp in _citaBorders)
        {
            var border = kvp.Value;
            if (border.Tag is Models.Cita)
            {
                var leftCita = Canvas.GetLeft(border);
                var topCita = Canvas.GetTop(border);
                var rectCita = new Avalonia.Rect(leftCita, topCita, border.Bounds.Width, border.Bounds.Height);
                if (rectCita.Contains(posicionEnCanvas))
                {
                    // Click dentro de una cita → no creamos nueva
                    return;
                }
            }
        }

        // Solo botón izquierdo
        var point = e.GetCurrentPoint(_canvasCitas);
        if (!point.Properties.IsLeftButtonPressed)
            return;

        // Iniciar modo creación
        _estaCreandoNuevaCita = true;
        _posicionInicioCreacion = e.GetPosition(_canvasCitas);

        // Capturar el puntero en el Canvas para seguir el arrastre
        e.Pointer.Capture(_canvasCitas);

        // Crear indicador visual de creación (borde punteado translúcido)
        _indicadorCreacionNueva = new Border
        {
            BorderBrush = Avalonia.Media.Brushes.White,
            BorderThickness = new Avalonia.Thickness(1.5),
            CornerRadius = new Avalonia.CornerRadius(6),
            Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromArgb(40, 59, 130, 246)), // Azul muy translúcido
            IsHitTestVisible = false
        };

        // Posición inicial mínima (un slot)
        var anchoPorColumna = _canvasCitas.Bounds.Width / 7.0;
        var altoPorFila = _canvasCitas.Bounds.Height / 28.0;

        var columna = (int)Math.Floor(Math.Max(0, Math.Min(6, _posicionInicioCreacion.X / anchoPorColumna)));
        var fila = (int)Math.Floor(Math.Max(0, Math.Min(27, _posicionInicioCreacion.Y / altoPorFila)));

        var left = columna * anchoPorColumna + 2;
        var top = fila * altoPorFila;

        Canvas.SetLeft(_indicadorCreacionNueva, left);
        Canvas.SetTop(_indicadorCreacionNueva, top);
        _indicadorCreacionNueva.Width = anchoPorColumna - 6;
        _indicadorCreacionNueva.Height = altoPorFila - 1;

        _canvasCitas.Children.Add(_indicadorCreacionNueva);
    }

    /// <summary>
    /// Se ejecuta cuando se mueve el mouse durante el arrastre o redimensionado. Actualiza la posición visual.
    /// Este handler se suscribe al Canvas para capturar el movimiento incluso fuera del Border.
    /// </summary>
    private void OnCanvasPointerMoved(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        // Manejar creación de nueva cita arrastrando sobre Canvas vacío
        if (_estaCreandoNuevaCita && _canvasCitas != null && _viewModel != null && _indicadorCreacionNueva != null)
        {
            var posicionActual = e.GetPosition(_canvasCitas);

            var anchoPorColumna = _canvasCitas.Bounds.Width / 7.0;
            var altoPorFila = _canvasCitas.Bounds.Height / 28.0;

            // Columna determinada por la posición inicial (no cambiamos de día durante el arrastre)
            var columna = (int)Math.Floor(Math.Max(0, Math.Min(6, _posicionInicioCreacion.X / anchoPorColumna)));

            // Fila de inicio y fin según Y (permitir arrastrar hacia arriba o hacia abajo)
            var filaInicio = (int)Math.Floor(Math.Max(0, Math.Min(27, _posicionInicioCreacion.Y / altoPorFila)));
            var filaActual = (int)Math.Floor(Math.Max(0, Math.Min(27, posicionActual.Y / altoPorFila)));

            var filaMin = Math.Min(filaInicio, filaActual);
            var filaMax = Math.Max(filaInicio, filaActual);

            // Asegurar al menos 1 fila (30 minutos)
            if (filaMax == filaMin)
                filaMax = Math.Min(27, filaMin + 1);

            var rowSpan = filaMax - filaMin + 1;

            var left = columna * anchoPorColumna + 2;
            var top = filaMin * altoPorFila;
            var width = anchoPorColumna - 6;
            var height = rowSpan * altoPorFila - 1;

            Canvas.SetLeft(_indicadorCreacionNueva, left);
            Canvas.SetTop(_indicadorCreacionNueva, top);
            _indicadorCreacionNueva.Width = width;
            _indicadorCreacionNueva.Height = height;

            return;
        }

        // Manejar redimensionado
        if (_estaRedimensionando && _citaRedimensionandose != null && _citaRedimensionOriginal != null && _canvasCitas != null && _viewModel != null)
        {
            if (e.Pointer.Captured == _citaRedimensionandose)
            {
                // Obtener posición actual del mouse relativa al Canvas
                var posicionMouseRedimension = e.GetPosition(_canvasCitas);
                
                // Calcular nueva altura basándose en la posición Y del mouse
                var altoPorFila = _canvasCitas.Bounds.Height / 28.0;
                var nuevaAltura = posicionMouseRedimension.Y - _topInicialCita;
                
                // Asegurar altura mínima (30 minutos = 1 fila)
                nuevaAltura = Math.Max(altoPorFila - 1, nuevaAltura);
                
                // Calcular cuántas filas ocupa la nueva altura
                var filasOcupadas = (int)Math.Round(nuevaAltura / altoPorFila);
                filasOcupadas = Math.Max(1, filasOcupadas); // Mínimo 1 fila (30 minutos)
                
                // Calcular nueva duración en minutos (cada fila = 30 minutos)
                var nuevaDuracionMinutos = filasOcupadas * 30;
                
                // Actualizar altura visual
                _citaRedimensionandose.Height = nuevaAltura;
                
                Serilog.Log.Debug("📏 Redimensionando cita {CitaId}: {DuracionInicial}min -> {NuevaDuracion}min", 
                    _citaRedimensionOriginal.Id, _duracionInicialMinutos, nuevaDuracionMinutos);
                
                return;
            }
        }
        
        // Manejar arrastre normal
        if (_citaArrastrandose == null || _citaOriginal == null || _canvasCitas == null || _viewModel == null)
        {
            return;
        }
        
        // Solo procesar si el puntero está capturado (estamos arrastrando)
        if (e.Pointer.Captured != _citaArrastrandose)
        {
            return;
        }

        // Obtener posición actual del mouse relativa al Canvas
        var posicionMouse = e.GetPosition(_canvasCitas);
        
        // Calcular distancia desde la posición inicial
        var distancia = Math.Sqrt(
            Math.Pow(posicionMouse.X - _posicionInicialArrastre.X, 2) +
            Math.Pow(posicionMouse.Y - _posicionInicialArrastre.Y, 2)
        );
        
        // Si la distancia supera el umbral, iniciar el arrastre visual
        if (!_estaArrastrando && distancia > DistanciaMinimaArrastre)
        {
            _estaArrastrando = true;
            
            // Cambiar apariencia visual para indicar que se está arrastrando
            _citaArrastrandose.Opacity = 0.6;
            _citaArrastrandose.BorderBrush = Avalonia.Media.Brushes.White;
            _citaArrastrandose.BorderThickness = new Avalonia.Thickness(2);
            _citaArrastrandose.Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.SizeAll);
            
            Serilog.Log.Debug("🖱️ Iniciando arrastre de cita {CitaId}", _citaOriginal.Id);
        }
        
        // Solo actualizar posición si realmente estamos arrastrando
        if (_estaArrastrando)
        {
            // Calcular nueva posición del Border (manteniendo el offset desde el centro)
            var nuevoLeft = posicionMouse.X - _offsetArrastre.X - (_citaArrastrandose.Width / 2);
            var nuevoTop = posicionMouse.Y - _offsetArrastre.Y - (_citaArrastrandose.Height / 2);
            
            // Asegurar que no se salga del Canvas
            nuevoLeft = Math.Max(0, Math.Min(nuevoLeft, _canvasCitas.Bounds.Width - _citaArrastrandose.Width));
            nuevoTop = Math.Max(0, Math.Min(nuevoTop, _canvasCitas.Bounds.Height - _citaArrastrandose.Height));
            
            // Actualizar posición visual de la cita
            Canvas.SetLeft(_citaArrastrandose, nuevoLeft);
            Canvas.SetTop(_citaArrastrandose, nuevoTop);
            
            // Actualizar posición del indicador fantasma (sombra) para mostrar dónde se colocará
            if (_indicadorFantasma != null && _viewModel != null)
            {
                // Calcular posición final basándose en el grid (snap a filas/columnas)
                var anchoPorColumna = _canvasCitas.Bounds.Width / 7.0;
                var altoPorFila = _canvasCitas.Bounds.Height / 28.0;
                
                var columna = (int)Math.Floor(Math.Max(0, Math.Min(6, nuevoLeft / anchoPorColumna)));
                var fila = (int)Math.Floor(Math.Max(0, Math.Min(27, nuevoTop / altoPorFila)));
                
                // Calcular posición exacta en el grid (snap)
                var leftSnap = columna * anchoPorColumna + 2;
                var topSnap = fila * altoPorFila;
                
                // Calcular ancho y altura del indicador fantasma
                var widthSnap = anchoPorColumna - 6;
                var heightSnap = _citaArrastrandose.Height;
                
                // Verificar si hay superposiciones en esta posición
                // Buscar citas que se solapen con esta posición temporal
                var citasEnMismaColumna = _viewModel.CitasSemana
                    .Where(c => c.Columna == columna && c.Cita.Id != _citaOriginal.Id)
                    .ToList();
                
                var citasSuperpuestas = new List<AgendaViewModel.CitaSemanaInfo>();
                var filaFinCita = fila + (int)Math.Ceiling(heightSnap / altoPorFila);
                
                foreach (var otraCita in citasEnMismaColumna)
                {
                    var filaFinOtra = otraCita.Fila + otraCita.RowSpan;
                    // Verificar si se solapan
                    if (fila < filaFinOtra && otraCita.Fila < filaFinCita)
                    {
                        citasSuperpuestas.Add(otraCita);
                    }
                }
                
                // Si hay superposiciones, calcular el índice y ajustar el ancho
                if (citasSuperpuestas.Count > 0)
                {
                    // Agregar la cita actual al grupo de superposiciones
                    var todasLasCitas = citasSuperpuestas.ToList();
                    todasLasCitas.Add(new AgendaViewModel.CitaSemanaInfo
                    {
                        Cita = _citaOriginal,
                        Columna = columna,
                        Fila = fila,
                        RowSpan = (int)Math.Ceiling(heightSnap / altoPorFila)
                    });
                    
                    // Ordenar por hora de inicio para asignar índices
                    todasLasCitas = todasLasCitas
                        .OrderBy(c => c.Fila)
                        .ThenBy(c => c.Cita.HoraInicio)
                        .ToList();
                    
                    // Calcular el número máximo de citas superpuestas simultáneamente
                    var maxSuperposiciones = CalcularMaxSuperposicionesSimultaneas(todasLasCitas);
                    
                    // Encontrar el índice de la cita actual en el grupo
                    var indiceEnGrupo = todasLasCitas.FindIndex(c => c.Cita.Id == _citaOriginal.Id);
                    if (indiceEnGrupo < 0) indiceEnGrupo = 0;
                    
                    // Ajustar ancho y posición según el índice
                    var anchoDisponible = anchoPorColumna - 6;
                    var anchoPorCita = anchoDisponible / maxSuperposiciones;
                    var espacioEntreCitas = 2.0;
                    
                    widthSnap = anchoPorCita - espacioEntreCitas;
                    leftSnap = columna * anchoPorColumna + 2 + (indiceEnGrupo * anchoPorCita);
                    
                    Serilog.Log.Debug("👻 Preview con superposiciones: Columna={Col}, Fila={Fila}, Índice={Indice}, MaxSuperposiciones={Max}, Width={Width}, Left={Left}", 
                        columna, fila, indiceEnGrupo, maxSuperposiciones, widthSnap, leftSnap);
                }
                
                // Actualizar el indicador fantasma
                Canvas.SetLeft(_indicadorFantasma, leftSnap);
                Canvas.SetTop(_indicadorFantasma, topSnap);
                _indicadorFantasma.Width = widthSnap;
                _indicadorFantasma.Height = heightSnap;
            }
        }
    }
    
    /// <summary>
    /// Calcula el número máximo de citas que se solapan simultáneamente en cualquier punto del tiempo.
    /// Método auxiliar para calcular la preview durante el arrastre.
    /// </summary>
    private int CalcularMaxSuperposicionesSimultaneas(List<AgendaViewModel.CitaSemanaInfo> grupo)
    {
        if (grupo.Count <= 1) return grupo.Count;
        
        // Crear eventos de inicio y fin
        var eventos = new List<(int fila, bool esInicio)>();
        
        foreach (var cita in grupo)
        {
            eventos.Add((cita.Fila, true));
            eventos.Add((cita.Fila + cita.RowSpan, false));
        }
        
        // Ordenar eventos por fila, y si hay empate, los finales antes que los iniciales
        eventos.Sort((a, b) => 
        {
            var comparacion = a.fila.CompareTo(b.fila);
            if (comparacion != 0) return comparacion;
            return a.esInicio == b.esInicio ? 0 : (a.esInicio ? 1 : -1);
        });
        
        // Sweep line: contar citas activas en cada punto
        int maxSuperposiciones = 0;
        int citasActivas = 0;
        
        foreach (var evento in eventos)
        {
            if (evento.esInicio)
            {
                citasActivas++;
                maxSuperposiciones = Math.Max(maxSuperposiciones, citasActivas);
            }
            else
            {
                citasActivas--;
            }
        }
        
        return maxSuperposiciones;
    }

    /// <summary>
    /// Handler del Border (no se usa, pero se mantiene para compatibilidad).
    /// </summary>
    private void OnCitaPointerMoved(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        // Este handler se mantiene pero no hace nada, ya que usamos OnCanvasPointerMoved
    }

    /// <summary>
    /// Se ejecuta cuando se suelta el mouse en el Canvas. Finaliza el arrastre/redimensionado y guarda los cambios.
    /// </summary>
    private async void OnCanvasPointerReleased(object? sender, Avalonia.Input.PointerReleasedEventArgs e)
    {
        // Manejar finalización de creación de nueva cita
        if (_estaCreandoNuevaCita && _canvasCitas != null && _viewModel != null)
        {
            // Liberar captura (si la tenía)
            e.Pointer.Capture(null);

                // Calcular rango final basándose en _posicionInicioCreacion y posición de suelta
                var posicionFinal = e.GetPosition(_canvasCitas);

                var anchoColumnaCreacion = _canvasCitas.Bounds.Width / 7.0;
                var altoFilaCreacion = _canvasCitas.Bounds.Height / 28.0;

                var columna = (int)Math.Floor(Math.Max(0, Math.Min(6, _posicionInicioCreacion.X / anchoColumnaCreacion)));

                var filaInicio = (int)Math.Floor(Math.Max(0, Math.Min(27, _posicionInicioCreacion.Y / altoFilaCreacion)));
                var filaFinal = (int)Math.Floor(Math.Max(0, Math.Min(27, posicionFinal.Y / altoFilaCreacion)));

                var filaMin = Math.Min(filaInicio, filaFinal);
                var filaMax = Math.Max(filaInicio, filaFinal);

                // rowSpan = número de slots seleccionados (mínimo 1 = 30 minutos)
                var rowSpan = filaMax - filaMin + 1;

                // Convertir a hora de inicio y duración (snap natural a slots de 30 minutos)
                var minutosDesdeInicioDia = filaMin * 30; // cada fila = 30 minutos
                var horaInicio = TimeSpan.FromMinutes(8 * 60 + minutosDesdeInicioDia); // día empieza a las 08:00
                var duracionMinutos = rowSpan * 30;

                // Obtener fecha del día correspondiente según la columna actual (vista semanal)
                DateTime fechaCita = DateTime.Today;
                try
                {
                    if (_viewModel.DiasSemana != null && _viewModel.DiasSemana.Count == 7)
                    {
                        fechaCita = _viewModel.DiasSemana[columna].Fecha;
                    }
                    else if (_viewModel.FechaSeleccionada.HasValue)
                    {
                        fechaCita = _viewModel.FechaSeleccionada.Value.Date;
                    }
                }
                catch
                {
                    // Fallback a hoy si algo falla
                    fechaCita = DateTime.Today;
                }

                // Eliminar indicador visual
                if (_indicadorCreacionNueva != null)
                {
                    _canvasCitas.Children.Remove(_indicadorCreacionNueva);
                    _indicadorCreacionNueva = null;
                }

                _estaCreandoNuevaCita = false;

                // Abrir modal de nueva cita con hora y duración precargadas
                await _viewModel.CrearCitaDesdeCalendario(fechaCita, horaInicio, duracionMinutos);

                Serilog.Log.Information("🆕 Creación de cita desde calendario: Fecha={Fecha}, HoraInicio={Hora}, Duracion={Duracion}min",
                    fechaCita, horaInicio, duracionMinutos);

                // No continuar con lógica de arrastre/redimensionado
                return;
        }

        // Manejar finalización de redimensionado PRIMERO
        if (_estaRedimensionando && _citaRedimensionandose != null && _citaRedimensionOriginal != null && _canvasCitas != null && _viewModel != null)
        {
            if (e.Pointer.Captured == _citaRedimensionandose)
            {
                // Liberar el puntero
                e.Pointer.Capture(null);
                
                // Calcular nueva duración basándose en la altura final
                var altoPorFilaRedimension = _canvasCitas.Bounds.Height / 28.0;
                var alturaFinal = _citaRedimensionandose.Height;
                var filasOcupadas = (int)Math.Round(alturaFinal / altoPorFilaRedimension);
                filasOcupadas = Math.Max(1, filasOcupadas); // Mínimo 1 fila (30 minutos)
                
                var nuevaDuracionMinutos = filasOcupadas * 30;
                
                // Validar duración mínima y máxima
                nuevaDuracionMinutos = Math.Max(30, nuevaDuracionMinutos); // Mínimo 30 minutos
                nuevaDuracionMinutos = Math.Min(480, nuevaDuracionMinutos); // Máximo 8 horas
                
                Serilog.Log.Information("📏 Finalizando redimensionado de cita {CitaId}: {DuracionInicial}min -> {NuevaDuracion}min", 
                    _citaRedimensionOriginal.Id, _duracionInicialMinutos, nuevaDuracionMinutos);
                
                // Restaurar cursor
                _citaRedimensionandose.Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand);
                
                // Cambiar la duración en el ViewModel (esto actualizará la BD y recargará las citas)
                await _viewModel.CambiarDuracionCitaAsync(_citaRedimensionOriginal.Id, nuevaDuracionMinutos);
                
                // Limpiar estado COMPLETAMENTE
                var citaIdRedimension = _citaRedimensionOriginal.Id;
                _citaRedimensionandose = null;
                _citaRedimensionOriginal = null;
                _estaRedimensionando = false;
                
                Serilog.Log.Debug("📏 Estado de redimensionado limpiado para cita {CitaId}", citaIdRedimension);
                return;
            }
        }
        
        // Manejar finalización de arrastre normal
        if (_citaArrastrandose == null || _citaOriginal == null || _canvasCitas == null || _viewModel == null)
        {
            return;
        }
        
        // Solo procesar si el puntero estaba capturado por nuestra cita
        if (e.Pointer.Captured != _citaArrastrandose)
        {
            return;
        }

        // Liberar el puntero
        e.Pointer.Capture(null);
        
        // Si no se inició el arrastre (fue solo un click), abrir el modal de edición
        if (!_estaArrastrando)
        {
            // Eliminar el indicador fantasma si existe
            if (_indicadorFantasma != null && _canvasCitas != null)
            {
                _canvasCitas.Children.Remove(_indicadorFantasma);
                _indicadorFantasma = null;
            }
            
            if (_citaOriginal != null && _viewModel != null)
            {
                _viewModel.CitaSeleccionada = _citaOriginal;
                _viewModel.EditarCitaCommand.Execute(null);
            }
            
            // Limpiar estado
            _citaArrastrandose = null;
            _citaOriginal = null;
            _estaArrastrando = false;
            return;
        }
        
        // Calcular nueva posición (día y hora) basándose en la posición final de la cita
        // Usar la posición actual de la cita (no del mouse) para mayor precisión
        var nuevoLeft = Canvas.GetLeft(_citaArrastrandose);
        var nuevoTop = Canvas.GetTop(_citaArrastrandose);
        
        // Eliminar el indicador fantasma
        if (_indicadorFantasma != null && _canvasCitas != null)
        {
            _canvasCitas.Children.Remove(_indicadorFantasma);
            _indicadorFantasma = null;
        }
        
        // Calcular dimensiones del Canvas (verificar que no sea null)
        if (_canvasCitas == null) return;
        
        var anchoPorColumna = _canvasCitas.Bounds.Width / 7.0;
        var altoPorFila = _canvasCitas.Bounds.Height / 28.0;
        
        // Calcular nueva columna (día) y fila (hora)
        var nuevaColumna = (int)Math.Floor(Math.Max(0, Math.Min(6, nuevoLeft / anchoPorColumna)));
        var nuevaFila = (int)Math.Floor(Math.Max(0, Math.Min(27, nuevoTop / altoPorFila)));
        
        // Calcular nueva fecha y hora
        if (nuevaColumna >= _viewModel.DiasSemana.Count || nuevaFila >= _viewModel.HorasSemana.Count)
        {
            // Eliminar el indicador fantasma
            if (_indicadorFantasma != null && _canvasCitas != null)
            {
                _canvasCitas.Children.Remove(_indicadorFantasma);
                _indicadorFantasma = null;
            }
            
            // Restaurar apariencia y cancelar
            _citaArrastrandose.Opacity = 0.85;
            _citaArrastrandose.BorderBrush = null;
            _citaArrastrandose.BorderThickness = new Avalonia.Thickness(0);
            _citaArrastrandose.Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand);
            _citaArrastrandose = null;
            _citaOriginal = null;
            _estaArrastrando = false;
            return;
        }
        
        var diaSemana = _viewModel.DiasSemana[nuevaColumna];
        var horaSlot = _viewModel.HorasSemana[nuevaFila];
        
        // Parsear la hora del slot (formato "HH:mm")
        var partesHora = horaSlot.Etiqueta.Split(':');
        if (partesHora.Length != 2 || !int.TryParse(partesHora[0], out var horas) || !int.TryParse(partesHora[1], out var minutos))
        {
            // Si no se puede parsear, usar la hora original
            horas = _citaOriginal.HoraInicio.Hours;
            minutos = _citaOriginal.HoraInicio.Minutes;
        }
        
        var nuevaFecha = diaSemana.Fecha;
        var nuevaHora = new TimeSpan(horas, minutos, 0);
        
        Serilog.Log.Information("🖱️ Finalizando arrastre de cita {CitaId}: Nueva fecha={Fecha}, Nueva hora={Hora}", 
            _citaOriginal.Id, nuevaFecha, nuevaHora);
        
        // Restaurar apariencia visual
        _citaArrastrandose.Opacity = 0.85;
        _citaArrastrandose.BorderBrush = null;
        _citaArrastrandose.BorderThickness = new Avalonia.Thickness(0);
        _citaArrastrandose.Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand);
        
        // Guardar referencia antes de limpiar
        var citaId = _citaOriginal.Id;
        
        // Limpiar estado ANTES de mover (para evitar conflictos)
        _citaArrastrandose = null;
        _citaOriginal = null;
        _estaArrastrando = false;
        
        // Mover la cita en el ViewModel (esto actualizará la BD y recargará las citas)
        await _viewModel.MoverCitaAsync(citaId, nuevaFecha, nuevaHora);
        
        Serilog.Log.Debug("🖱️ Estado de arrastre limpiado para cita {CitaId}", citaId);
    }

    /// <summary>
    /// Muestra un menú contextual para seleccionar una cita cuando hay superposiciones.
    /// </summary>
    private void MostrarMenuCitasSuperpuestas(AgendaViewModel.CitaSemanaInfo citaInfo, Avalonia.Input.PointerEventArgs e)
    {
        if (_viewModel == null || citaInfo.CitasSuperpuestas.Count <= 1) return;
        
        // Crear un menú contextual con las citas superpuestas
        var menu = new Avalonia.Controls.ContextMenu();
        
        foreach (var citaSuperpuesta in citaInfo.CitasSuperpuestas)
        {
            var menuItem = new Avalonia.Controls.MenuItem
            {
                Header = $"{citaSuperpuesta.Cita.HoraInicioFormateada} - {citaSuperpuesta.Cita.Cliente?.NombreCompleto ?? "Sin cliente"}",
                Tag = citaSuperpuesta.Cita
            };
            
            menuItem.Click += (s, args) =>
            {
                if (s is Avalonia.Controls.MenuItem item && item.Tag is Models.Cita citaSeleccionada)
                {
                    _viewModel.CitaSeleccionada = citaSeleccionada;
                    _viewModel.EditarCitaCommand.Execute(null);
                }
            };
            
            menu.Items.Add(menuItem);
        }
        
        // Mostrar el menú en la posición del click
        menu.PlacementTarget = _canvasCitas;
        menu.Placement = Avalonia.Controls.PlacementMode.Pointer;
        menu.Open();
        
        Serilog.Log.Debug("📋 Mostrando menú de {Count} citas superpuestas", citaInfo.CitasSuperpuestas.Count);
    }

    /// <summary>
    /// Selecciona un día en la vista mensual.
    /// </summary>
    private async void OnDiaMesPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
            return;

        if (sender is not Border { DataContext: AgendaViewModel.DiaMesInfo dia })
            return;

        if (DataContext is not AgendaViewModel vm)
            return;

        await vm.SeleccionarDiaMesCommand.ExecuteAsync(dia.Fecha);
        e.Handled = true;
    }
}
