using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InkStudio.Data;
using InkStudio.Models;
using InkStudio.Services;
using Serilog;

namespace InkStudio.ViewModels;

/// <summary>
/// ViewModel para gestionar la captura de fotos (antes/después) de un trabajo mediante QR y móvil.
/// </summary>
public partial class FotoTrabajoViewModel : ViewModelBase
{
    private readonly InkStudioDbContext _db;
    private readonly FirmaWebService _firmaWebService = new();

    private string? _tokenActual;
    private Trabajo? _trabajo;
    private bool _esAntes;

    public FotoTrabajoViewModel(InkStudioDbContext dbContext)
    {
        _db = dbContext;
    }

    #region Propiedades

    [ObservableProperty]
    private bool _esVisible;

    [ObservableProperty]
    private string _tituloModal = "📸 Foto del trabajo";

    [ObservableProperty]
    private string _estadoConexion = "⏳ Esperando conexión...";

    [ObservableProperty]
    private bool _estaProcesando;

    [ObservableProperty]
    private string _urlFoto = string.Empty;

    [ObservableProperty]
    private Bitmap? _qrCodeImage;

    [ObservableProperty]
    private Bitmap? _previewFotoAntes;

    [ObservableProperty]
    private Bitmap? _previewFotoDespues;

    [ObservableProperty]
    private string _mensajeError = string.Empty;

    #endregion

    /// <summary>
    /// Abre el modal para capturar/ver fotos de un trabajo.
    /// </summary>
    public async Task AbrirModalAsync(Trabajo trabajo, bool esAntes)
    {
        try
        {
            _trabajo = trabajo;
            _esAntes = esAntes;

            TituloModal = esAntes ? "📸 Foto ANTES del trabajo" : "📸 Foto DESPUÉS del trabajo";
            MensajeError = string.Empty;
            EstadoConexion = "⏳ Preparando captura...";

            // Cargar previews existentes (si hay rutas guardadas)
            await CargarPreviewsAsync();

            // Iniciar servidor y generar QR
            await IniciarCapturaAsync();

            EsVisible = true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al abrir modal de fotos de trabajo");
            MensajeError = $"Error al abrir el modal de fotos: {ex.Message}";
        }
    }

    /// <summary>
    /// Carga en memoria las imágenes existentes para mostrarlas como preview.
    /// </summary>
    private Task CargarPreviewsAsync()
    {
        try
        {
            PreviewFotoAntes = CargarBitmapDesdeRuta(_trabajo?.FotoAntesPath);
            PreviewFotoDespues = CargarBitmapDesdeRuta(_trabajo?.FotoDespuesPath);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error al cargar previews de fotos de trabajo");
        }

        return Task.CompletedTask;
    }

    private static Bitmap? CargarBitmapDesdeRuta(string? ruta)
    {
        if (string.IsNullOrWhiteSpace(ruta) || !File.Exists(ruta))
            return null;

        try
        {
            using var stream = File.OpenRead(ruta);
            return new Bitmap(stream);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error al cargar bitmap desde ruta: {Ruta}", ruta);
            return null;
        }
    }

    /// <summary>
    /// Inicia el servidor HTTP y genera el QR para captura de foto.
    /// </summary>
    private async Task IniciarCapturaAsync()
    {
        if (_trabajo == null)
        {
            MensajeError = "No hay trabajo seleccionado para capturar foto.";
            return;
        }

        try
        {
            EstaProcesando = true;
            EstadoConexion = "🔄 Iniciando servidor...";

            var servidorIniciado = await _firmaWebService.IniciarServidor();
            if (!servidorIniciado)
            {
                EstadoConexion = "❌ Error al iniciar servidor. Verifica permisos.";
                EstaProcesando = false;
                return;
            }

            _tokenActual = FirmaWebService.GenerarTokenUnico();
            _firmaWebService.RegistrarToken(_tokenActual);

            UrlFoto = _firmaWebService.GenerarUrlFoto(_tokenActual);
            QrCodeImage = QRCodeService.GenerarQRCode(UrlFoto, 300);

            // Esperar foto en segundo plano
            _ = Task.Run(async () => await EsperarFotoAsync());

            EstadoConexion = "✅ Servidor activo - Escanea el código QR con tu móvil para hacer la foto";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al iniciar captura de foto");
            EstadoConexion = "❌ Error al iniciar la captura de foto";
        }
        finally
        {
            EstaProcesando = false;
        }
    }

    /// <summary>
    /// Espera la recepción de la foto, la guarda en disco y actualiza el trabajo.
    /// </summary>
    private async Task EsperarFotoAsync()
    {
        if (string.IsNullOrEmpty(_tokenActual) || _trabajo == null)
            return;

        try
        {
            var imagenBase64 = await _firmaWebService.EsperarFirma(_tokenActual, TimeSpan.FromMinutes(5));
            if (string.IsNullOrEmpty(imagenBase64))
            {
                Log.Warning("No se recibió imagen para el token de foto o expiró el tiempo");
                return;
            }

            EstadoConexion = "📥 Recibiendo foto...";

            // Decodificar y guardar JPEG
            var bytes = Convert.FromBase64String(imagenBase64);
            var ruta = _esAntes
                ? ConsentimientoPathService.ObtenerRutaFotoAntes(_trabajo.ClienteId, _trabajo.Id)
                : ConsentimientoPathService.ObtenerRutaFotoDespues(_trabajo.ClienteId, _trabajo.Id);

            Directory.CreateDirectory(Path.GetDirectoryName(ruta)!);
            await File.WriteAllBytesAsync(ruta, bytes);

            // Actualizar trabajo en BD
            var trabajoDb = await _db.Trabajos.FirstOrDefaultAsync(t => t.Id == _trabajo.Id);
            if (trabajoDb != null)
            {
                if (_esAntes)
                {
                    trabajoDb.FotoAntesPath = ruta;
                }
                else
                {
                    trabajoDb.FotoDespuesPath = ruta;
                }

                await _db.SaveChangesAsync();

                // Refrescar instancia local
                if (_esAntes)
                {
                    _trabajo.FotoAntesPath = ruta;
                    PreviewFotoAntes = CargarBitmapDesdeRuta(ruta);
                }
                else
                {
                    _trabajo.FotoDespuesPath = ruta;
                    PreviewFotoDespues = CargarBitmapDesdeRuta(ruta);
                }

                Log.Information("Foto de trabajo guardada en {Ruta}", ruta);
                EstadoConexion = "✅ Foto recibida y guardada correctamente";
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al recibir/guardar foto de trabajo");
            MensajeError = $"Error al guardar la foto: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Cerrar()
    {
        EsVisible = false;
        _firmaWebService.DetenerServidor();
        _tokenActual = null;
    }
}


