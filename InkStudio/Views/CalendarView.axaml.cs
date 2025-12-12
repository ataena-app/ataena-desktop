using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using InkStudio.ViewModels;

namespace InkStudio.Views;

/// <summary>
/// Vista de calendario visual para mostrar el mes con citas usando CalendarControl.Avalonia de NuGet.
/// </summary>
public partial class CalendarView : UserControl
{
    /// <summary>
    /// Inicializa el componente.
    /// </summary>
    public CalendarView()
    {
        InitializeComponent();
    }
}

