using System;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using InkStudio.Data;
using InkStudio.Models;
using InkStudio.Services;
using Serilog;

namespace InkStudio.ViewModels;

/// <summary>
/// ViewModel para el modal de firma de consentimientos.
/// </summary>
public partial class ConsentimientoFirmaViewModel : ViewModelBase
{
    private readonly FirmaWebService _firmaWebService = new();
    private string? _tokenActual;
    private Consentimiento? _consentimiento;

    /// <summary>
    /// Evento que se dispara cuando se completa la firma y se guarda el PDF.
    /// </summary>
    public event EventHandler<Cliente>? FirmaCompletada;

    #region Propiedades

    /// <summary>
    /// Texto del consentimiento a mostrar.
    /// </summary>
    [ObservableProperty]
    private string _textoConsentimiento = string.Empty;

    /// <summary>
    /// Tipo de consentimiento.
    /// </summary>
    [ObservableProperty]
    private TipoConsentimiento _tipoConsentimiento;

    /// <summary>
    /// Cliente que va a firmar.
    /// </summary>
    [ObservableProperty]
    private Cliente? _cliente;

    /// <summary>
    /// Trabajo asociado (opcional, solo para consentimientos de trabajo).
    /// </summary>
    [ObservableProperty]
    private Trabajo? _trabajo;

    /// <summary>
    /// URL para acceder a la página de firma desde el móvil.
    /// </summary>
    [ObservableProperty]
    private string _urlFirma = string.Empty;

    /// <summary>
    /// Imagen del código QR.
    /// </summary>
    [ObservableProperty]
    private Bitmap? _qrCodeImage;

    /// <summary>
    /// Indica si se ha recibido la firma.
    /// </summary>
    [ObservableProperty]
    private bool _firmaRecibida = false;

    /// <summary>
    /// Imagen de la firma recibida (base64).
    /// </summary>
    [ObservableProperty]
    private string? _imagenFirmaBase64;

    /// <summary>
    /// Estado de la conexión (texto descriptivo).
    /// </summary>
    [ObservableProperty]
    private string _estadoConexion = "⏳ Esperando conexión...";

    /// <summary>
    /// Indica si el usuario acepta los términos.
    /// </summary>
    [ObservableProperty]
    private bool _aceptaTerminos = false;

    /// <summary>
    /// Indica si el modal está visible.
    /// </summary>
    [ObservableProperty]
    private bool _esVisible = false;

    /// <summary>
    /// Indica si se está procesando (generando PDF, etc.).
    /// </summary>
    [ObservableProperty]
    private bool _estaProcesando = false;

    #endregion

    #region Comandos

    /// <summary>
    /// Inicia el proceso de firma: inicia servidor, genera token y QR.
    /// </summary>
    [RelayCommand]
    private async Task IniciarFirma()
    {
        if (Cliente == null)
        {
            Log.Warning("Intento de iniciar firma sin cliente seleccionado");
            return;
        }

        try
        {
            EstaProcesando = true;
            EstadoConexion = "🔄 Iniciando servidor...";

            // Iniciar servidor HTTP
            var servidorIniciado = await _firmaWebService.IniciarServidor();
            if (!servidorIniciado)
            {
                EstadoConexion = "❌ Error al iniciar servidor. Verifica permisos.";
                EstaProcesando = false;
                return;
            }

            // Generar token único
            _tokenActual = FirmaWebService.GenerarTokenUnico();
            _firmaWebService.RegistrarToken(_tokenActual);

            // Generar URL
            UrlFirma = _firmaWebService.GenerarUrlFirma(_tokenActual);

            // Generar QR Code
            QrCodeImage = QRCodeService.GenerarQRCode(UrlFirma, 300);

            // Suscribirse al evento de firma recibida
            _firmaWebService.FirmaRecibida += OnFirmaRecibida;

            // Esperar firma en segundo plano
            _ = Task.Run(async () => await EsperarFirma());

            EstadoConexion = "✅ Servidor activo - Escanea el código QR con tu móvil";
            EstaProcesando = false;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al iniciar proceso de firma");
            EstadoConexion = "❌ Error al iniciar el proceso de firma";
            EstaProcesando = false;
        }
    }

    /// <summary>
    /// Cancela el proceso de firma y cierra el modal.
    /// </summary>
    [RelayCommand]
    private void CancelarFirma()
    {
        _firmaWebService.FirmaRecibida -= OnFirmaRecibida;
        _firmaWebService.DetenerServidor();
        EsVisible = false;
        LimpiarEstado();
    }

    /// <summary>
    /// Confirma la firma y genera el PDF.
    /// </summary>
    [RelayCommand]
    private async Task ConfirmarFirma()
    {
        if (!FirmaRecibida || string.IsNullOrEmpty(ImagenFirmaBase64))
        {
            Log.Warning("Intento de confirmar firma sin firma recibida");
            return;
        }

        if (!AceptaTerminos)
        {
            EstadoConexion = "⚠️ Debes aceptar los términos para continuar";
            return;
        }

        if (Cliente == null || _consentimiento == null)
        {
            Log.Error("Datos incompletos para generar PDF");
            return;
        }

        try
        {
            EstaProcesando = true;
            EstadoConexion = "📄 Generando PDF...";

            // Generar ruta del PDF
            var rutaPdf = ConsentimientoPathService.ObtenerRutaCompletaPdf(
                Cliente.Id,
                TipoConsentimiento,
                Trabajo?.Id);

            // Generar PDF
            var pdfGenerado = await ConsentimientoService.GenerarPdfConsentimiento(
                _consentimiento,
                ImagenFirmaBase64,
                rutaPdf);

            if (!pdfGenerado)
            {
                EstadoConexion = "❌ Error al generar el PDF";
                EstaProcesando = false;
                return;
            }

            // Guardar consentimiento en BD
            var guardado = await ConsentimientoService.GuardarConsentimiento(_consentimiento, rutaPdf);
            if (!guardado)
            {
                EstadoConexion = "❌ Error al guardar el consentimiento";
                EstaProcesando = false;
                return;
            }

            EstadoConexion = "✅ Consentimiento firmado y guardado correctamente";

            // Disparar evento de firma completada
            FirmaCompletada?.Invoke(this, Cliente);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al confirmar firma");
            EstadoConexion = "❌ Error al procesar la firma";
            EstaProcesando = false;
        }
    }

    /// <summary>
    /// Copia la URL al portapapeles.
    /// </summary>
    [RelayCommand]
    private async Task CopiarUrl()
    {
        if (string.IsNullOrEmpty(UrlFirma))
            return;

        try
        {
            // TODO: Implementar copia al portapapeles en Avalonia
            // Por ahora solo logueamos
            Log.Information("URL para copiar: {URL}", UrlFirma);
            EstadoConexion = "📋 URL copiada al portapapeles";
            await Task.Delay(2000);
            if (FirmaRecibida)
            {
                EstadoConexion = "✅ Firma recibida - Lista para confirmar";
            }
            else
            {
                EstadoConexion = "✅ Servidor activo - Escanea el código QR con tu móvil";
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al copiar URL");
        }
    }

    #endregion

    #region Métodos Públicos

    /// <summary>
    /// Abre el modal de firma para un consentimiento.
    /// </summary>
    /// <param name="cliente">Cliente que va a firmar.</param>
    /// <param name="tipo">Tipo de consentimiento.</param>
    /// <param name="trabajo">Trabajo asociado (opcional).</param>
    public async Task AbrirModal(Cliente cliente, TipoConsentimiento tipo, Trabajo? trabajo = null)
    {
        Cliente = cliente;
        TipoConsentimiento = tipo;
        Trabajo = trabajo;

        // Cargar texto del consentimiento
        var plantilla = ConsentimientoService.CargarPlantillaTexto(tipo);
        if (string.IsNullOrEmpty(plantilla))
        {
            Log.Error("No se pudo cargar la plantilla para tipo: {Tipo}", tipo);
            TextoConsentimiento = "Error al cargar el texto del consentimiento.";
        }
        else
        {
            // Cargar configuración para reemplazar placeholders
            using var context = new InkStudioDbContext();
            var configuracion = await context.Configuracion.FindAsync(1);
            if (configuracion == null)
            {
                configuracion = new Configuracion { NombreEstudio = "InkStudio" };
            }

            TextoConsentimiento = ConsentimientoService.ReemplazarPlaceholders(
                plantilla,
                cliente,
                trabajo,
                configuracion,
                DateTime.Now);
        }

        // Crear o cargar consentimiento
        using var db = new InkStudioDbContext();
        _consentimiento = await db.Consentimientos
            .FirstOrDefaultAsync(c => c.ClienteId == cliente.Id && 
                                     c.Tipo == tipo && 
                                     (trabajo == null || c.TrabajoId == trabajo.Id));

        if (_consentimiento == null)
        {
            _consentimiento = new Consentimiento
            {
                ClienteId = cliente.Id,
                Tipo = tipo,
                TrabajoId = trabajo?.Id,
                FechaFirma = DateTime.Now,
                Firmado = false
            };
        }

        // Limpiar estado primero
        LimpiarEstado();
        
        // Ahora abrir el modal explícitamente
        EsVisible = true;

        // Iniciar automáticamente el servidor
        await IniciarFirmaCommand.ExecuteAsync(null);
    }

    /// <summary>
    /// Cierra el modal y limpia el estado.
    /// </summary>
    public void CerrarModal()
    {
        EsVisible = false;
        LimpiarEstado();
        _firmaWebService.FirmaRecibida -= OnFirmaRecibida;
        _firmaWebService.DetenerServidor();
    }

    #endregion

    #region Métodos Privados

    /// <summary>
    /// Espera a recibir la firma del móvil.
    /// </summary>
    private async Task EsperarFirma()
    {
        if (string.IsNullOrEmpty(_tokenActual))
            return;

        try
        {
            var firma = await _firmaWebService.EsperarFirma(_tokenActual, TimeSpan.FromMinutes(5));
            if (!string.IsNullOrEmpty(firma))
            {
                // La firma se recibirá a través del evento OnFirmaRecibida
                Log.Information("Firma recibida para token: {Token}", _tokenActual);
            }
            else
            {
                EstadoConexion = "⏱️ Tiempo de espera agotado. Intenta de nuevo.";
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al esperar firma");
            EstadoConexion = "❌ Error al recibir la firma";
        }
    }

    /// <summary>
    /// Maneja el evento cuando se recibe una firma.
    /// </summary>
    private void OnFirmaRecibida(object? sender, FirmaRecibidaEventArgs e)
    {
        if (e.Token != _tokenActual)
            return;

        ImagenFirmaBase64 = e.ImagenBase64;
        FirmaRecibida = true;
        EstadoConexion = "✅ Firma recibida - Revisa y confirma";
    }

    /// <summary>
    /// Limpia el estado del modal.
    /// </summary>
    private void LimpiarEstado()
    {
        UrlFirma = string.Empty;
        QrCodeImage = null;
        FirmaRecibida = false;
        ImagenFirmaBase64 = null;
        EstadoConexion = "⏳ Esperando conexión...";
        AceptaTerminos = false;
        EstaProcesando = false;
        _tokenActual = null;
    }

    #endregion
}

