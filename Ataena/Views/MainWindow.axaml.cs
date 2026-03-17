using Avalonia.Controls;

namespace Ataena.Views;

/// <summary>
/// Ventana principal de la aplicación Ataena CRM.
/// </summary>
/// <remarks>
/// Contiene:
/// - Barra de navegación lateral
/// - Área de contenido principal (cambia según la sección)
/// 
/// Las vistas disponibles son:
/// - Dashboard (inicio)
/// - Clientes (gestión de clientes)
/// - Agenda (calendario de citas)
/// - Trabajos (galería de trabajos)
/// - Configuración (ajustes)
/// </remarks>
public partial class MainWindow : Window
{
    /// <summary>
    /// Inicializa la ventana principal.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
        
        // Asegurar que la ventana se abra maximizada después de que se muestre
        Opened += (sender, e) =>
        {
            WindowState = WindowState.Maximized;
        };
    }
}
