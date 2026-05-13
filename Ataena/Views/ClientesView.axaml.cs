using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Ataena.Models;
using Ataena.ViewModels;

namespace Ataena.Views;

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
            
            // Aplicar estilos de fin de semana cuando se abre el popup
            if (popup.IsOpen)
            {
                // Usar un pequeño delay para asegurar que el calendario esté renderizado
                Dispatcher.UIThread.Post(() =>
                {
                    ActualizarHeaderMes();
                    AplicarEstilosFinDeSemana(popup);
                    
                    // Suscribirse a cambios en el calendario para actualizar el header
                    // cuando el usuario navega por los meses
                    var calendar = BuscarCalendarEnPopup(popup);
                    if (calendar != null)
                    {
                        calendar.PropertyChanged += (s, args) =>
                        {
                            if (args.Property.Name == "DisplayDate")
                            {
                                ActualizarHeaderMes();
                                Dispatcher.UIThread.Post(() =>
                                {
                                    AplicarEstilosFinDeSemana(popup);
                                }, DispatcherPriority.Loaded);
                            }
                        };
                    }
                }, DispatcherPriority.Loaded);
            }
        }
    }

    /// <summary>
    /// Actualiza el header cuando cambia la fecha seleccionada.
    /// </summary>
    private void OnFechaNacimientoSelectedDateChanged(object? sender, Avalonia.Controls.DatePickerSelectedValueChangedEventArgs e)
    {
        ActualizarHeaderMes();
        
        // Reaplicar estilos cuando cambia la fecha
        if (this.FindControl<Popup>("FechaNacimientoPopup") is Popup popup && popup.IsOpen)
        {
            Dispatcher.UIThread.Post(() =>
            {
                AplicarEstilosFinDeSemana(popup);
            }, DispatcherPriority.Loaded);
        }
    }

    /// <summary>
    /// Busca el Calendar dentro del popup.
    /// </summary>
    private Calendar? BuscarCalendarEnPopup(Popup popup)
    {
        Calendar? calendar = null;
        
        void FindCalendar(Control? parent)
        {
            if (parent == null) return;
            if (parent is Calendar cal && calendar == null)
            {
                calendar = cal;
                return;
            }
            if (parent is Panel panel)
            {
                foreach (var child in panel.Children)
                {
                    if (child is Control childControl)
                    {
                        FindCalendar(childControl);
                    }
                }
            }
        }
        
        if (popup.Child is Control child)
        {
            FindCalendar(child);
        }
        
        return calendar;
    }

    /// <summary>
    /// Actualiza el texto del header con el mes y año actual del calendario.
    /// </summary>
    private void ActualizarHeaderMes()
    {
        if (this.FindControl<TextBlock>("MesAnioHeader") is TextBlock header)
        {
            // Buscar el Calendar dentro del popup para obtener el mes mostrado
            if (this.FindControl<Popup>("FechaNacimientoPopup") is Popup popup)
            {
                Calendar? calendar = null;
                
                void FindCalendar(Control? parent)
                {
                    if (parent == null) return;
                    if (parent is Calendar cal && calendar == null)
                    {
                        calendar = cal;
                        return;
                    }
                    if (parent is Panel panel)
                    {
                        foreach (var child in panel.Children)
                        {
                            if (child is Control childControl)
                            {
                                FindCalendar(childControl);
                            }
                        }
                    }
                }
                
                if (popup.Child is Control child)
                {
                    FindCalendar(child);
                }
                
                if (calendar != null)
                {
                    header.Text = calendar.DisplayDate.ToString("MMMM yyyy", System.Globalization.CultureInfo.GetCultureInfo("es-ES"));
                }
                else
                {
                    // Fallback: usar SelectedDate del DatePicker o fecha actual
                    if (this.FindControl<DatePicker>("FechaNacimientoDatePicker") is DatePicker datePicker)
                    {
                        var fechaMostrada = datePicker.SelectedDate?.DateTime ?? DateTime.Now;
                        header.Text = fechaMostrada.ToString("MMMM yyyy", System.Globalization.CultureInfo.GetCultureInfo("es-ES"));
                    }
                }
            }
        }
    }

    /// <summary>
    /// Aplica estilos a los días de fin de semana en el calendario.
    /// </summary>
    private void AplicarEstilosFinDeSemana(Popup popup)
    {
        try
        {
            // Buscar el DatePicker dentro del popup usando el contenido del popup
            DatePicker? datePicker = null;
            Calendar? calendar = null;
            
            // Buscar recursivamente en el árbol visual
            void FindControls(Control? parent)
            {
                if (parent == null) return;
                
                if (parent is DatePicker dp && datePicker == null)
                {
                    datePicker = dp;
                }
                
                if (parent is Calendar cal && calendar == null)
                {
                    calendar = cal;
                }
                
                // Buscar en los hijos
                if (parent is Panel panel)
                {
                    foreach (var child in panel.Children)
                    {
                        if (child is Control childControl)
                        {
                            FindControls(childControl);
                        }
                    }
                }
            }
            
            // Buscar en el contenido del popup
            if (popup.Child is Control child)
            {
                FindControls(child);
            }
            
            if (calendar == null && datePicker != null)
            {
                // Si no encontramos el Calendar directamente, buscar en el DatePicker
                FindControls(datePicker);
            }
            
            if (calendar == null) return;

            // Buscar todos los CalendarDayButton recursivamente
            var dayButtons = new List<CalendarDayButton>();
            void FindDayButtons(Control? parent)
            {
                if (parent == null) return;
                
                if (parent is CalendarDayButton button)
                {
                    dayButtons.Add(button);
                }
                
                if (parent is Panel panel)
                {
                    foreach (var child in panel.Children)
                    {
                        if (child is Control childControl)
                        {
                            FindDayButtons(childControl);
                        }
                    }
                }
            }
            
            FindDayButtons(calendar);
            
            foreach (var button in dayButtons)
            {
                // Verificar si el día es sábado o domingo
                // En Avalonia, el DataContext del botón puede contener la fecha
                if (button.DataContext is DateTime date)
                {
                    var dayOfWeek = date.DayOfWeek;
                    if (dayOfWeek == DayOfWeek.Saturday || dayOfWeek == DayOfWeek.Sunday)
                    {
                        button.Classes.Add("weekend");
                        // Aplicar estilo directamente con color más visible
                        button.Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#2d3748"));
                        button.Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#cbd5e1"));
                    }
                }
                
                // También marcar días de otros meses
                if (button.DataContext is DateTime dateCheck)
                {
                    // Verificar si el día pertenece al mes actual del calendario
                    if (calendar.DisplayDate.Month != dateCheck.Month)
                    {
                        button.Classes.Add("other-month");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // Si falla, no es crítico, simplemente no se aplicarán los estilos
            System.Diagnostics.Debug.WriteLine($"Error al aplicar estilos de fin de semana: {ex.Message}");
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

