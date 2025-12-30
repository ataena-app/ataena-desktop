using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InkStudio.Data;
using InkStudio.Models;
using InkStudio.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace InkStudio.ViewModels;

/// <summary>
/// ViewModel para la gestión de backups y restauración.
/// </summary>
public partial class BackupViewModel : ViewModelBase
{
    private readonly InkStudioDbContext _db = new();
    private readonly BackupService _backupService;
    private readonly RestauracionService _restauracionService;

    public BackupViewModel()
    {
        _backupService = new BackupService(_db);
        _restauracionService = new RestauracionService();

        // Cargar datos iniciales en el hilo de UI para evitar errores de "Call from invalid thread"
        // al ejecutar comandos (AsyncRelayCommand) que notifican CanExecuteChanged.
        Dispatcher.UIThread.Post(async () =>
        {
            try
            {
                await CargarConfiguracionCommand.ExecuteAsync(null);
                await CargarServiciosNubeCommand.ExecuteAsync(null);
                await ActualizarListaBackupsCommand.ExecuteAsync(null);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error al cargar datos iniciales en BackupViewModel");
            }
        }, DispatcherPriority.Background);
    }

    #region Propiedades - Servicios de Nube

    /// <summary>
    /// Lista de servicios de nube detectados.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<InfoServicioNube> _serviciosNube = new();

    /// <summary>
    /// Servicio de nube seleccionado actualmente.
    /// </summary>
    [ObservableProperty]
    private InfoServicioNube? _servicioNubeSeleccionado;

    /// <summary>
    /// Indica si se debe copiar automáticamente a la nube.
    /// </summary>
    [ObservableProperty]
    private bool _copiarAutomaticamenteNube = true;

    #endregion

    #region Propiedades - Backups

    /// <summary>
    /// Lista de backups disponibles (local y nube).
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<InfoBackup> _backups = new();

    /// <summary>
    /// Backup seleccionado actualmente.
    /// </summary>
    [ObservableProperty]
    private InfoBackup? _backupSeleccionado;

    /// <summary>
    /// Último backup creado (para mostrar información).
    /// </summary>
    [ObservableProperty]
    private InfoBackup? _ultimoBackup;

    #endregion

    #region Propiedades - Configuración de Backup Automático

    /// <summary>
    /// Indica si el backup automático está activado.
    /// </summary>
    [ObservableProperty]
    private bool _backupAutomaticoActivo = false;

    /// <summary>
    /// Frecuencia del backup automático (0=Diario, 1=Semanal, 2=Mensual).
    /// </summary>
    [ObservableProperty]
    private int _backupFrecuencia = 0;

    /// <summary>
    /// Hora del backup automático (en minutos desde medianoche).
    /// </summary>
    [ObservableProperty]
    private int _backupHora = 840; // 14:00

    /// <summary>
    /// Número de backups a mantener.
    /// </summary>
    [ObservableProperty]
    private int _backupMantenerUltimos = 10;

    #endregion

    #region Propiedades - Estado

    /// <summary>
    /// Indica si hay una operación en curso.
    /// </summary>
    [ObservableProperty]
    private bool _cargando = false;

    /// <summary>
    /// Flag para prevenir ejecuciones múltiples simultáneas del comando de backup.
    /// </summary>
    private bool _creandoBackup = false;

    /// <summary>
    /// Lock object para sincronización thread-safe.
    /// </summary>
    private readonly object _lockBackup = new object();

    /// <summary>
    /// Determina si se puede crear un backup.
    /// </summary>
    private bool CanCrearBackup() => !_creandoBackup && !Cargando;

    /// <summary>
    /// Mensaje de estado para mostrar al usuario.
    /// </summary>
    [ObservableProperty]
    private string _mensajeEstado = string.Empty;

    /// <summary>
    /// Mensaje de error para mostrar al usuario.
    /// </summary>
    [ObservableProperty]
    private string _mensajeError = string.Empty;

    /// <summary>
    /// Progreso de la operación actual (0-100).
    /// </summary>
    [ObservableProperty]
    private int _progreso = 0;

    #endregion

    #region Comandos - Carga de Datos

    /// <summary>
    /// Carga la configuración de backup desde la base de datos.
    /// </summary>
    [RelayCommand]
    private async Task CargarConfiguracion()
    {
        try
        {
            var config = await _db.Configuracion.FirstOrDefaultAsync();
            if (config != null)
            {
                BackupAutomaticoActivo = config.BackupAutomaticoActivo;
                BackupFrecuencia = config.BackupFrecuencia;
                BackupHora = config.BackupHora;
                BackupMantenerUltimos = config.BackupMantenerUltimos;
                CopiarAutomaticamenteNube = config.BackupCopiarAutomaticamenteNube;

                // Cargar servicio de nube seleccionado
                if (config.BackupServicioNube.HasValue && !string.IsNullOrEmpty(config.BackupRutaNube))
                {
                    var servicio = ServiciosNube.FirstOrDefault(s => s.Tipo == config.BackupServicioNube.Value);
                    if (servicio != null)
                    {
                        ServicioNubeSeleccionado = servicio;
                    }
                    else
                    {
                        // Crear servicio "Otro" si no está en la lista detectada
                        ServicioNubeSeleccionado = new InfoServicioNube
                        {
                            Tipo = ServicioNube.Otro,
                            Nombre = "Carpeta personalizada",
                            RutaCarpeta = config.BackupRutaNube,
                            Detectado = true,
                            Sincronizado = Directory.Exists(config.BackupRutaNube)
                        };
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al cargar configuración de backup");
            MensajeError = $"Error al cargar configuración: {ex.Message}";
        }
    }

    /// <summary>
    /// Detecta y carga los servicios de nube disponibles.
    /// </summary>
    [RelayCommand]
    private async Task CargarServiciosNube()
    {
        try
        {
            await Task.Run(() =>
            {
                var servicios = BackupService.DetectarServiciosNube();
                ServiciosNube.Clear();
                foreach (var servicio in servicios)
                {
                    ServiciosNube.Add(servicio);
                }
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al detectar servicios de nube");
            MensajeError = $"Error al detectar servicios de nube: {ex.Message}";
        }
    }

    /// <summary>
    /// Actualiza la lista de backups disponibles.
    /// </summary>
    private bool _actualizandoLista = false;

    [RelayCommand]
    private async Task ActualizarListaBackups()
    {
        // Prevenir ejecuciones múltiples simultáneas
        if (_actualizandoLista)
        {
            Log.Warning("⚠️ ActualizarListaBackups ya está en ejecución, ignorando...");
            return;
        }

        try
        {
            _actualizandoLista = true;
            Cargando = true;
            MensajeError = string.Empty;

            // Capturar el servicio seleccionado antes de entrar en Task.Run para evitar problemas de threading
            var servicioSeleccionado = ServicioNubeSeleccionado;
            
            await Task.Run(() =>
            {
                // Usar un HashSet con una clave única para detectar duplicados reales
                // La clave será: nombre_archivo + fecha_creacion + tamaño
                var backupsUnicos = new Dictionary<string, InfoBackup>();

                // Solo cargar backups del servicio de nube seleccionado (OneDrive, etc.)
                // NO cargar backups locales
                if (servicioSeleccionado != null && Directory.Exists(servicioSeleccionado.RutaCarpeta))
                {
                    var backupsNube = BackupService.ListarBackups(servicioSeleccionado.RutaCarpeta);
                    Log.Information("☁️ Backups encontrados en {Servicio}: {Count}", servicioSeleccionado.Nombre, backupsNube.Count);

                    // Agregar backups de nube
                    foreach (var backupNube in backupsNube)
                    {
                        // Crear clave única para el backup
                        var claveUnica = $"{backupNube.NombreArchivo}|{backupNube.FechaCreacion:yyyy-MM-dd HH:mm:ss}|{backupNube.TamañoBytes}";
                        
                        if (!backupsUnicos.ContainsKey(claveUnica))
                        {
                            var metadata = BackupService.ObtenerMetadataBackup(backupNube.RutaCompleta);
                            if (metadata != null)
                            {
                                backupNube.Metadata = metadata;
                            }
                            backupNube.Sincronizado = true; // Todos los backups de nube están sincronizados
                            backupsUnicos[claveUnica] = backupNube;
                            Log.Information("☁️ Backup agregado desde {Servicio}: {Nombre}", servicioSeleccionado.Nombre, backupNube.NombreArchivo);
                        }
                    }
                }
                else
                {
                    Log.Warning("⚠️ No hay servicio de nube seleccionado o la carpeta no existe. No se mostrarán backups.");
                }

                // Actualizar la colección en el hilo de UI
                var backupsOrdenados = backupsUnicos.Values.OrderByDescending(b => b.FechaCreacion).ToList();
                Dispatcher.UIThread.Post(() =>
                {
                    Backups.Clear();
                    foreach (var backup in backupsOrdenados)
                    {
                        // Verificar que no esté ya en la lista (por si acaso)
                        if (!Backups.Any(b => 
                            b.NombreArchivo == backup.NombreArchivo &&
                            b.FechaCreacion == backup.FechaCreacion &&
                            b.TamañoBytes == backup.TamañoBytes))
                        {
                            Backups.Add(backup);
                        }
                    }
                    UltimoBackup = Backups.FirstOrDefault();
                    Log.Information("📊 Total backups únicos en la lista: {Count}", Backups.Count);
                }, DispatcherPriority.Normal);
            });

            MensajeEstado = $"✅ {Backups.Count} backups encontrados";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al actualizar lista de backups");
            MensajeError = $"Error al actualizar lista: {ex.Message}";
        }
        finally
        {
            Cargando = false;
            _actualizandoLista = false;
        }
    }

    #endregion

    #region Comandos - Crear Backup

    /// <summary>
    /// Crea un nuevo backup.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanCrearBackup))]
    private async Task CrearBackup()
    {
        // Prevenir ejecuciones múltiples simultáneas usando lock
        lock (_lockBackup)
        {
            if (_creandoBackup || Cargando)
            {
                Log.Warning("⚠️ Intento de crear backup mientras ya hay uno en curso. Ignorando...");
                return;
            }
            // Establecer flags de forma atómica
            _creandoBackup = true;
        }

        // Actualizar propiedades en el hilo de UI
        Dispatcher.UIThread.Post(() =>
        {
            Cargando = true;
            CrearBackupCommand.NotifyCanExecuteChanged();
        }, DispatcherPriority.Normal);
        
        Log.Information("🔄 Iniciando creación de backup (ID: {ThreadId})", System.Threading.Thread.CurrentThread.ManagedThreadId);

        try
        {
            // Actualizar propiedades en el hilo de UI
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                MensajeError = string.Empty;
                MensajeEstado = "🔄 Creando backup...";
                Progreso = 0;
            });

            string? rutaBackup = null;

            // Crear backup local
            await Dispatcher.UIThread.InvokeAsync(() => Progreso = 25);
            Log.Information("📦 Llamando a CrearBackupAsync...");
            rutaBackup = await _backupService.CrearBackupAsync();
            Log.Information("✅ Backup creado: {Ruta}", rutaBackup);

            await Dispatcher.UIThread.InvokeAsync(() => Progreso = 50);

            // Copiar a nube si está configurado
            if (CopiarAutomaticamenteNube && ServicioNubeSeleccionado != null)
            {
                await Dispatcher.UIThread.InvokeAsync(() => Progreso = 75);
                await _backupService.CopiarBackupANubeAsync(rutaBackup, ServicioNubeSeleccionado);
            }

            await Dispatcher.UIThread.InvokeAsync(() => Progreso = 100);

            // Rotar backups si es necesario
            if (BackupMantenerUltimos > 0)
            {
                var carpetaLocal = BackupService.ObtenerRutaCarpetaBackups();
                BackupService.RotarBackups(carpetaLocal, BackupMantenerUltimos);

                if (ServicioNubeSeleccionado != null && Directory.Exists(ServicioNubeSeleccionado.RutaCarpeta))
                {
                    BackupService.RotarBackups(ServicioNubeSeleccionado.RutaCarpeta, BackupMantenerUltimos);
                }
            }

            // Actualizar lista
            await ActualizarListaBackupsCommand.ExecuteAsync(null);

            // Actualizar propiedades en el hilo de UI
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                MensajeEstado = "✅ Backup creado exitosamente";
                Progreso = 0;
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al crear backup");
            // Actualizar propiedades en el hilo de UI
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                MensajeError = $"Error al crear backup: {ex.Message}";
                MensajeEstado = "❌ Error al crear backup";
                Progreso = 0;
            });
        }
        finally
        {
            // Actualizar propiedades en el hilo de UI
            Dispatcher.UIThread.Post(() =>
            {
                lock (_lockBackup)
                {
                    Cargando = false;
                    _creandoBackup = false;
                }
                CrearBackupCommand.NotifyCanExecuteChanged();
            }, DispatcherPriority.Normal);
            Log.Information("✅ Proceso de backup finalizado. Flags reseteados.");
        }
    }

    #endregion

    #region Comandos - Restaurar Backup

    /// <summary>
    /// Restaura un backup seleccionado.
    /// Muestra un diálogo de confirmación antes de proceder.
    /// </summary>
    [RelayCommand]
    private async Task RestaurarBackup(InfoBackup? backup)
    {
        if (backup == null)
        {
            MensajeError = "Por favor, selecciona un backup para restaurar";
            return;
        }

        try
        {
            // Validar backup
            if (!BackupService.ValidarBackup(backup.RutaCompleta))
            {
                MensajeError = "El backup seleccionado no es válido o está corrupto";
                return;
            }

            // Obtener resumen del backup
            var resumen = _restauracionService.ObtenerResumenBackup(backup.RutaCompleta);

            // Mostrar diálogo de confirmación
            var confirmado = await DialogService.ConfirmarRestaurarBackupAsync(
                nombreBackup: backup.NombreArchivo,
                resumen: resumen
            );

            if (!confirmado)
            {
                Log.Debug("Restauración de backup cancelada por el usuario: {Backup}", backup.NombreArchivo);
                return;
            }

            Cargando = true;
            MensajeError = string.Empty;
            MensajeEstado = "🔄 Restaurando backup...";
            Progreso = 0;

            await Task.Run(async () =>
            {
                Progreso = 50;
                var metadata = await _restauracionService.RestaurarBackupAsync(backup.RutaCompleta, crearBackupActual: true);
                Progreso = 100;
            });

            MensajeEstado = "✅ Backup restaurado exitosamente.\n\n⚠️ Por favor, cierra y vuelve a abrir la aplicación para que los cambios surtan efecto.";
            Progreso = 0;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al restaurar backup");
            MensajeError = $"Error al restaurar backup: {ex.Message}";
            MensajeEstado = "❌ Error al restaurar backup";
            Progreso = 0;
        }
        finally
        {
            Cargando = false;
        }
    }

    /// <summary>
    /// Restaura un backup desde un archivo seleccionado.
    /// </summary>
    [RelayCommand]
    private async Task RestaurarDesdeArchivo()
    {
        // TODO: Implementar selector de archivo
        // Por ahora, usar el backup seleccionado si existe
        if (BackupSeleccionado != null)
        {
            await RestaurarBackupCommand.ExecuteAsync(BackupSeleccionado);
        }
        else
        {
            MensajeError = "Por favor, selecciona un backup o proporciona la ruta al archivo";
        }
    }

    #endregion

    #region Comandos - Configuración

    /// <summary>
    /// Guarda la configuración de backup.
    /// </summary>
    [RelayCommand]
    private async Task GuardarConfiguracion()
    {
        try
        {
            Cargando = true;
            MensajeError = string.Empty;

            var config = await _db.Configuracion.FirstOrDefaultAsync();
            if (config == null)
            {
                config = new Configuracion { Id = 1 };
                _db.Configuracion.Add(config);
            }

            config.BackupAutomaticoActivo = BackupAutomaticoActivo;
            config.BackupFrecuencia = BackupFrecuencia;
            config.BackupHora = BackupHora;
            config.BackupMantenerUltimos = BackupMantenerUltimos;
            config.BackupCopiarAutomaticamenteNube = CopiarAutomaticamenteNube;
            config.BackupServicioNube = ServicioNubeSeleccionado?.Tipo;
            config.BackupRutaNube = ServicioNubeSeleccionado?.RutaCarpeta;

            await _db.SaveChangesAsync();

            MensajeEstado = "✅ Configuración guardada";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al guardar configuración de backup");
            MensajeError = $"Error al guardar configuración: {ex.Message}";
        }
        finally
        {
            Cargando = false;
        }
    }

    /// <summary>
    /// Selecciona una carpeta personalizada para backups en la nube.
    /// </summary>
    [RelayCommand]
    private void SeleccionarCarpetaNube()
    {
        // TODO: Implementar selector de carpeta usando Avalonia
        // Por ahora, usar la carpeta del servicio seleccionado o crear uno nuevo
        MensajeEstado = "Funcionalidad de selección de carpeta pendiente de implementar";
    }

    #endregion

    #region Comandos - Gestión de Backups

    /// <summary>
    /// Elimina un backup.
    /// Muestra un diálogo de confirmación antes de proceder.
    /// </summary>
    [RelayCommand]
    private async Task EliminarBackup(InfoBackup? backup)
    {
        if (backup == null)
        {
            return;
        }

        // Mostrar diálogo de confirmación
        var confirmado = await DialogService.ConfirmarEliminarAsync(
            tipoElemento: "el backup",
            nombreElemento: $"{backup.NombreArchivo}\nFecha: {backup.FechaCreacion:dd/MM/yyyy HH:mm}\nTamaño: {backup.TamañoFormateado}",
            advertenciaAdicional: "El archivo de backup se eliminará permanentemente."
        );

        if (!confirmado)
        {
            Log.Debug("Eliminación de backup cancelada por el usuario: {Backup}", backup.NombreArchivo);
            return;
        }

        try
        {
            BackupService.EliminarBackup(backup.RutaCompleta);
            await ActualizarListaBackupsCommand.ExecuteAsync(null);
            MensajeEstado = "✅ Backup eliminado";
            Log.Information("Backup eliminado: {Backup}", backup.NombreArchivo);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al eliminar backup");
            MensajeError = $"Error al eliminar backup: {ex.Message}";
        }
    }

    /// <summary>
    /// Muestra información detallada de un backup.
    /// </summary>
    [RelayCommand]
    private void VerInfoBackup(InfoBackup? backup)
    {
        if (backup == null)
        {
            return;
        }

        var metadata = BackupService.ObtenerMetadataBackup(backup.RutaCompleta);
        if (metadata != null)
        {
            MensajeEstado = $"📦 {backup.NombreArchivo}\n" +
                           $"Fecha: {metadata.FechaCreacion:dd/MM/yyyy HH:mm}\n" +
                           $"Tamaño: {backup.TamañoFormateado}\n" +
                           $"Clientes: {metadata.NumeroClientes}\n" +
                           $"Citas: {metadata.NumeroCitas}\n" +
                           $"Trabajos: {metadata.NumeroTrabajos}\n" +
                           $"Consentimientos: {metadata.NumeroConsentimientos}\n" +
                           $"Versión: {metadata.VersionApp}";
        }
        else
        {
            MensajeEstado = $"📦 {backup.NombreArchivo}\n" +
                           $"Fecha: {backup.FechaCreacion:dd/MM/yyyy HH:mm}\n" +
                           $"Tamaño: {backup.TamañoFormateado}";
        }
    }

    #endregion
}

