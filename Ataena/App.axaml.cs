using System;
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
using Serilog;

namespace Ataena;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            try
            {
                Services.LoggingService.EscribirDiagnostico("App: OnFrameworkInitializationCompleted inicio");

                DisableAvaloniaDataAnnotationValidation();

                // IMPORTANTE: Avalonia muestra MainWindow cuando termina este método.
                // Si usamos await, el método "termina" antes de asignar MainWindow.
                // Solución: asignar MainWindow ANTES de cualquier await, aunque esté oculto.
                var setupCompletado = Services.SetupInicialService.SetupCompletado;
                Services.LoggingService.EscribirDiagnostico($"App: SetupCompletado={setupCompletado}");

                if (setupCompletado)
                {
                    Services.LoggingService.EscribirDiagnostico("App: Abriendo MainWindow directamente");
                    var mainWindow = new MainWindow { DataContext = new MainWindowViewModel() };
                    mainWindow.WindowState = WindowState.Maximized;
                    desktop.MainWindow = mainWindow;
                    mainWindow.Show();
                }
                else
                {
                    Services.LoggingService.EscribirDiagnostico("App: Abriendo SetupInicialView como MainWindow");

                    // Durante el setup ponemos el modo "cierre explícito" para que al cerrar el
                    // SetupView (que inicialmente ES la MainWindow) Avalonia NO entienda que se
                    // cerró la última ventana y apague la app antes de abrir la MainWindow real.
                    // Lo revertimos a OnLastWindowClose tras la transición.
                    desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

                    var setupView = new SetupInicialView();
                    setupView.SetupCompletado += (_, _) =>
                    {
                        try
                        {
                            Services.LoggingService.EscribirDiagnostico("App: SetupCompletado recibido → creando MainWindow");

                            var mw = new MainWindow { DataContext = new MainWindowViewModel() };
                            mw.WindowState = WindowState.Maximized;

                            // 1º asignar y mostrar la nueva MainWindow
                            desktop.MainWindow = mw;
                            mw.Show();
                            mw.Activate();

                            // 2º cerrar la ventana de setup en el siguiente tick del dispatcher
                            //    (así Avalonia ya tiene registrada la nueva MainWindow)
                            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                            {
                                try
                                {
                                    setupView.Close();
                                }
                                catch (Exception exClose)
                                {
                                    Services.LoggingService.EscribirDiagnostico(
                                        $"App: aviso al cerrar setupView - {exClose.GetType().Name}: {exClose.Message}");
                                }
                                finally
                                {
                                    // Restaurar el modo por defecto: cerrar MainWindow apaga la app.
                                    desktop.ShutdownMode = ShutdownMode.OnLastWindowClose;
                                    Services.LoggingService.EscribirDiagnostico("App: transición setup → MainWindow OK");
                                }
                            });
                        }
                        catch (Exception exTrans)
                        {
                            Services.LoggingService.EscribirDiagnostico(
                                $"App: ERROR transición - {exTrans.GetType().Name}: {exTrans.Message}");
                            Serilog.Log.Error(exTrans, "Error al transitar de Setup a MainWindow");
                            // Si falla, restauramos el modo para no dejar la app zombi.
                            desktop.ShutdownMode = ShutdownMode.OnLastWindowClose;
                        }
                    };
                    desktop.MainWindow = setupView;
                    setupView.Show();
                }

                // Splash opcional: mostrarlo brevemente mientras carga (no bloquea)
                var splash = new SplashView();
                splash.Show();
                _ = splash.CargarConfiguracionAsync().ContinueWith(_ =>
                {
                    Avalonia.Threading.Dispatcher.UIThread.Post(() => splash.Close());
                });

                Services.LoggingService.EscribirDiagnostico("App: OnFrameworkInitializationCompleted OK");
            }
            catch (Exception ex)
            {
                Services.LoggingService.EscribirDiagnostico($"App: ERROR - {ex.GetType().Name}: {ex.Message}");
                Serilog.Log.Fatal(ex, "Error fatal en arranque de la aplicación");
                throw;
            }
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