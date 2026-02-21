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
using Microsoft.EntityFrameworkCore;

namespace InkStudio.ViewModels;

/// <summary>
/// ViewModel para gestionar la captura de fotos de DNI (cliente o tutor) mediante QR y móvil.
/// </summary>
public partial class FotoDniViewModel : ViewModelBase
{
    private readonly FirmaWebService _firmaWebService = new();

    private string? _tokenActual;
    private Cliente? _cliente;
    private bool _esDniTutor;

    /// <summary>
    /// Evento que se dispara cuando se guarda una foto exitosamente.
    /// </summary>
    public event EventHandler<Cliente>? FotoGuardada;

    #region Propiedades

    [ObservableProperty]
    private bool _esVisible;

    [ObservableProperty]
    private string _tituloModal = "📷 Foto del DNI";

    [ObservableProperty]
    private string _estadoConexion = "⏳ Esperando conexión...";

    [ObservableProperty]
    private bool _estaProcesando;

    [ObservableProperty]
    private string _urlFoto = string.Empty;

    [ObservableProperty]
    private Bitmap? _qrCodeImage;

    [ObservableProperty]
    private Bitmap? _previewFotoDni;

    [ObservableProperty]
    private string _mensajeError = string.Empty;

    [ObservableProperty]
    private string _instrucciones = string.Empty;

    #endregion

    /// <summary>
    /// Abre el modal para capturar foto de DNI.
    /// </summary>
    /// <param name="cliente">Cliente al que pertenece el DNI.</param>
    /// <param name="esDniTutor">True si es el DNI del tutor, false si es del cliente.</param>
    public async Task AbrirModalAsync(Cliente cliente, bool esDniTutor)
    {
        try
        {
            _cliente = cliente;
            _esDniTutor = esDniTutor;

            TituloModal = esDniTutor 
                ? "📷 Foto del DNI del Tutor" 
                : "📷 Foto del DNI del Cliente";

            Instrucciones = esDniTutor
                ? $"Sube una foto del DNI del tutor: {cliente.NombreCompletoTutor}"
                : $"Sube una foto del DNI de: {cliente.NombreCompleto}";

            MensajeError = string.Empty;
            EstadoConexion = "⏳ Preparando captura...";

            // Cargar preview existente
            CargarPreview();

            // Iniciar servidor y generar QR
            await IniciarCapturaAsync();

            EsVisible = true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al abrir modal de foto de DNI");
            MensajeError = $"Error al abrir el modal: {ex.Message}";
        }
    }

    /// <summary>
    /// Carga la imagen existente para mostrarla como preview.
    /// </summary>
    private void CargarPreview()
    {
        try
        {
            var ruta = _esDniTutor ? _cliente?.FotoDniTutorPath : _cliente?.FotoDniPath;
            PreviewFotoDni = CargarBitmapDesdeRuta(ruta);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error al cargar preview de foto de DNI");
        }
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
        if (_cliente == null)
        {
            MensajeError = "No hay cliente seleccionado.";
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

            EstadoConexion = "✅ Escanea el código QR con tu móvil y sube la foto del DNI";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al iniciar captura de foto de DNI");
            EstadoConexion = "❌ Error al iniciar la captura";
        }
        finally
        {
            EstaProcesando = false;
        }
    }

    /// <summary>
    /// Espera la recepción de la foto, la guarda en disco y actualiza el cliente.
    /// </summary>
    private async Task EsperarFotoAsync()
    {
        if (string.IsNullOrEmpty(_tokenActual) || _cliente == null)
        {
            Log.Warning("EsperarFotoAsync: Token o cliente es null");
            return;
        }

        Log.Information("Esperando foto DNI para token: {Token}, Cliente: {ClienteId}, EsTutor: {EsTutor}", 
            _tokenActual, _cliente.Id, _esDniTutor);

        try
        {
            var imagenBase64 = await _firmaWebService.EsperarFirma(_tokenActual, TimeSpan.FromMinutes(5));
            if (string.IsNullOrEmpty(imagenBase64))
            {
                Log.Warning("No se recibió imagen de DNI para el token {Token}", _tokenActual);
                EstadoConexion = "⏱️ Tiempo de espera agotado. Intenta de nuevo.";
                return;
            }

            Log.Information("Foto DNI recibida, tamaño base64: {Tamaño} caracteres", imagenBase64.Length);
            EstadoConexion = "📥 Recibiendo foto...";

            // Decodificar imagen
            byte[] bytes;
            try
            {
                bytes = Convert.FromBase64String(imagenBase64);
                Log.Information("Imagen decodificada, tamaño: {Tamaño} bytes", bytes.Length);
            }
            catch (FormatException ex)
            {
                Log.Error(ex, "Error al decodificar base64 de foto DNI");
                MensajeError = "Error: El formato de la imagen no es válido.";
                EstadoConexion = "❌ Error al procesar la imagen";
                return;
            }

            // Determinar ruta según tipo
            var ruta = _esDniTutor
                ? ConsentimientoPathService.ObtenerRutaFotoDniTutor(_cliente.Id)
                : ConsentimientoPathService.ObtenerRutaFotoDni(_cliente.Id);

            Log.Information("Guardando foto DNI en: {Ruta}", ruta);

            var directorio = Path.GetDirectoryName(ruta);
            if (!string.IsNullOrEmpty(directorio))
            {
                Directory.CreateDirectory(directorio);
            }

            await File.WriteAllBytesAsync(ruta, bytes);
            Log.Information("Foto DNI guardada en disco: {Ruta}", ruta);

            // Actualizar cliente en BD
            using var db = new InkStudioDbContext();
            var clienteDb = await db.Clientes.FirstOrDefaultAsync(c => c.Id == _cliente.Id);
            if (clienteDb == null)
            {
                Log.Error("No se encontró el cliente {ClienteId} en la base de datos", _cliente.Id);
                MensajeError = "Error: No se encontró el cliente en la base de datos.";
                EstadoConexion = "❌ Error al actualizar";
                return;
            }

            if (_esDniTutor)
            {
                clienteDb.FotoDniTutorPath = ruta;
                _cliente.FotoDniTutorPath = ruta;
            }
            else
            {
                clienteDb.FotoDniPath = ruta;
                _cliente.FotoDniPath = ruta;
            }

            await db.SaveChangesAsync();
            Log.Information("Cliente actualizado con ruta de foto DNI: {Ruta}", ruta);

            // Actualizar preview
            PreviewFotoDni = CargarBitmapDesdeRuta(ruta);
            OnPropertyChanged(nameof(PreviewFotoDni));

            Log.Information("✅ Foto de DNI guardada correctamente");
            EstadoConexion = "✅ Foto del DNI recibida y guardada correctamente";
            MensajeError = string.Empty;

            // Disparar evento
            FotoGuardada?.Invoke(this, clienteDb);

            // Cerrar automáticamente
            await Task.Delay(1500);
            Cerrar();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al recibir/guardar foto de DNI");
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
        PreviewFotoDni = null;
    }

    [RelayCommand]
    private async Task RefrescarQrAsync()
    {
        await IniciarCapturaAsync();
    }

    [RelayCommand]
    private async Task SubirDesdeOrdenadorAsync()
    {
        if (_cliente == null)
        {
            MensajeError = "No hay cliente seleccionado.";
            return;
        }

        try
        {
            var topLevel = Avalonia.Application.Current?.ApplicationLifetime is 
                Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop 
                ? desktop.MainWindow 
                : null;

            if (topLevel == null)
            {
                MensajeError = "No se pudo abrir el selector de archivos.";
                return;
            }

            var archivos = await topLevel.StorageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions
            {
                Title = "Seleccionar foto del DNI",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new Avalonia.Platform.Storage.FilePickerFileType("Imágenes")
                    {
                        Patterns = new[] { "*.jpg", "*.jpeg", "*.png", "*.bmp", "*.gif" }
                    }
                }
            });

            if (archivos == null || archivos.Count == 0)
                return;

            var archivo = archivos[0];
            EstadoConexion = "📥 Procesando imagen...";

            // Leer el archivo
            await using var stream = await archivo.OpenReadAsync();
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            var bytes = memoryStream.ToArray();

            // Determinar ruta de destino
            var ruta = _esDniTutor
                ? ConsentimientoPathService.ObtenerRutaFotoDniTutor(_cliente.Id)
                : ConsentimientoPathService.ObtenerRutaFotoDni(_cliente.Id);

            var directorio = Path.GetDirectoryName(ruta);
            if (!string.IsNullOrEmpty(directorio))
            {
                Directory.CreateDirectory(directorio);
            }

            await File.WriteAllBytesAsync(ruta, bytes);
            Log.Information("Foto DNI subida desde ordenador: {Ruta}", ruta);

            // Actualizar cliente en BD
            using var db = new InkStudioDbContext();
            var clienteDb = await db.Clientes.FirstOrDefaultAsync(c => c.Id == _cliente.Id);
            if (clienteDb == null)
            {
                MensajeError = "Error: No se encontró el cliente.";
                return;
            }

            if (_esDniTutor)
            {
                clienteDb.FotoDniTutorPath = ruta;
                _cliente.FotoDniTutorPath = ruta;
            }
            else
            {
                clienteDb.FotoDniPath = ruta;
                _cliente.FotoDniPath = ruta;
            }

            await db.SaveChangesAsync();

            // Actualizar preview
            PreviewFotoDni = CargarBitmapDesdeRuta(ruta);
            OnPropertyChanged(nameof(PreviewFotoDni));

            EstadoConexion = "✅ Foto del DNI guardada correctamente";
            MensajeError = string.Empty;

            FotoGuardada?.Invoke(this, clienteDb);

            await Task.Delay(1500);
            Cerrar();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al subir foto DNI desde ordenador");
            MensajeError = $"Error al subir la foto: {ex.Message}";
            EstadoConexion = "❌ Error al subir la foto";
        }
    }
}
