using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Markup.Xaml;
using Ataena.ViewModels;
using Ataena.Views;

namespace Ataena;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();

            // Mostrar splash mientras carga
            var splash = new SplashView();
            splash.Show();

            // Cargar configuración y mostrar logo
            await splash.CargarConfiguracionAsync();

            // Crear ventana principal
            var mainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };
            
            // Asegurar que la ventana se abra maximizada
            mainWindow.WindowState = WindowState.Maximized;
            
            desktop.MainWindow = mainWindow;
            mainWindow.Show();

            // Cerrar splash
            splash.Close();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}