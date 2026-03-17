using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ataena.Data;
using Ataena.Models;
using Ataena.Services;
using Serilog;
using Microsoft.EntityFrameworkCore;

namespace Ataena.ViewModels;

/// <summary>
/// ViewModel para gestionar la captura de fotos (antes/después) de un trabajo mediante QR y móvil.
/// </summary>
public partial class FotoTrabajoViewModel : ViewModelBase
{
    private readonly AtaenaDbContext _db;
    private readonly FirmaWebService _firmaWebService = new();

    private string? _tokenActual;
    private Trabajo? _trabajo;
    private bool _esAntes;

    /// <summary>
    /// Evento que se dispara cuando se guarda una foto exitosamente.
    /// </summary>
    public event EventHandler<Trabajo>? FotoGuardada;

    public FotoTrabajoViewModel(AtaenaDbContext dbContext)
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

            TituloModal = esAntes ? "➕ Añadir foto ANTES del trabajo" : "➕ Añadir foto DESPUÉS del trabajo";
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
        {
            Log.Warning("EsperarFotoAsync: Token o trabajo es null. Token: {Token}, Trabajo: {TrabajoId}", 
                _tokenActual, _trabajo?.Id);
            return;
        }

        Log.Information("Esperando foto para token: {Token}, Trabajo: {TrabajoId}, EsAntes: {EsAntes}", 
            _tokenActual, _trabajo.Id, _esAntes);

        try
        {
            var imagenBase64 = await _firmaWebService.EsperarFirma(_tokenActual, TimeSpan.FromMinutes(5));
            if (string.IsNullOrEmpty(imagenBase64))
            {
                Log.Warning("No se recibió imagen para el token de foto {Token} o expiró el tiempo", _tokenActual);
                EstadoConexion = "⏱️ Tiempo de espera agotado. Intenta de nuevo.";
                return;
            }

            Log.Information("Foto recibida para token {Token}, tamaño base64: {Tamaño} caracteres", 
                _tokenActual, imagenBase64.Length);

            EstadoConexion = "📥 Recibiendo foto...";

            // Decodificar y guardar JPEG
            byte[] bytes;
            try
            {
                bytes = Convert.FromBase64String(imagenBase64);
                Log.Information("Imagen decodificada correctamente, tamaño: {Tamaño} bytes", bytes.Length);
            }
            catch (FormatException ex)
            {
                Log.Error(ex, "Error al decodificar base64. Primeros 100 caracteres: {Preview}", 
                    imagenBase64.Length > 100 ? imagenBase64.Substring(0, 100) : imagenBase64);
                MensajeError = "Error: El formato de la imagen no es válido. Intenta con otra foto.";
                EstadoConexion = "❌ Error al procesar la imagen";
                return;
            }

            var ruta = _esAntes
                ? ConsentimientoPathService.ObtenerRutaFotoAntes(_trabajo.ClienteId, _trabajo.Id)
                : ConsentimientoPathService.ObtenerRutaFotoDespues(_trabajo.ClienteId, _trabajo.Id);

            Log.Information("Guardando foto en: {Ruta}", ruta);

            var directorio = Path.GetDirectoryName(ruta);
            if (!string.IsNullOrEmpty(directorio))
            {
                Directory.CreateDirectory(directorio);
            }

            await File.WriteAllBytesAsync(ruta, bytes);
            Log.Information("Foto guardada en disco: {Ruta}, {Tamaño} bytes", ruta, bytes.Length);

            // Actualizar trabajo en BD
            var trabajoDb = await _db.Trabajos.FirstOrDefaultAsync(t => t.Id == _trabajo.Id);
            if (trabajoDb == null)
            {
                Log.Error("No se encontró el trabajo {TrabajoId} en la base de datos", _trabajo.Id);
                MensajeError = "Error: No se encontró el trabajo en la base de datos.";
                EstadoConexion = "❌ Error al actualizar el trabajo";
                return;
            }

            if (_esAntes)
            {
                trabajoDb.FotoAntesPath = ruta;
            }
            else
            {
                trabajoDb.FotoDespuesPath = ruta;
            }

            await _db.SaveChangesAsync();
            Log.Information("Trabajo actualizado en BD con ruta de foto: {Ruta}", ruta);

            // Refrescar instancia local y UI
            if (_esAntes)
            {
                _trabajo.FotoAntesPath = ruta;
                PreviewFotoAntes = CargarBitmapDesdeRuta(ruta);
                OnPropertyChanged(nameof(PreviewFotoAntes));
            }
            else
            {
                _trabajo.FotoDespuesPath = ruta;
                PreviewFotoDespues = CargarBitmapDesdeRuta(ruta);
                OnPropertyChanged(nameof(PreviewFotoDespues));
            }

            Log.Information("✅ Foto de trabajo guardada correctamente en {Ruta}", ruta);
            EstadoConexion = "✅ Foto recibida y guardada correctamente";
            MensajeError = string.Empty; // Limpiar errores previos

            // Disparar evento para notificar que se guardó la foto
            FotoGuardada?.Invoke(this, trabajoDb);

            // Cerrar el modal automáticamente después de un breve delay para que el usuario vea el mensaje de éxito
            await Task.Delay(1500);
            Cerrar();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al recibir/guardar foto de trabajo. Token: {Token}, Trabajo: {TrabajoId}", 
                _tokenActual, _trabajo?.Id);
            MensajeError = $"Error al guardar la foto: {ex.Message}";
            EstadoConexion = "❌ Error al guardar la foto";
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


