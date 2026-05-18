using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Ataena.Data;
using Ataena.Models;
using Ataena.Services;
using Serilog;

namespace Ataena.ViewModels;

/// <summary>
/// ViewModel para el modal de firma de consentimientos.
/// </summary>
public partial class ConsentimientoFirmaViewModel : ViewModelBase
{
    private readonly FirmaWebService _firmaWebService = FirmaWebService.InstanciaCompartida;
    private string? _tokenActual;
    private Consentimiento? _consentimiento;

    /// <summary>
    /// Evento que se dispara cuando se completa la firma y se guarda el PDF.
    /// </summary>
    public event EventHandler<Cliente>? FirmaCompletada;

    /// <summary>
    /// El usuario canceló o cerró el modal de firma (sin implicar que <see cref="FirmaCompletada"/> haya tenido lugar).
    /// </summary>
    public event EventHandler? ModalSesionFinalizada;

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
    /// Título legible del modal según el tipo de consentimiento.
    /// </summary>
    public string TituloModal => TipoConsentimiento switch
    {
        TipoConsentimiento.RGPD => "📝 Consentimiento RGPD",
        TipoConsentimiento.RGPD_Menor => "📝 Consentimiento RGPD (Menor)",
        TipoConsentimiento.Imagenes => "📸 Consentimiento de uso de imágenes",
        TipoConsentimiento.Imagenes_Menor => "📸 Consentimiento de uso de imágenes (Menor)",
        TipoConsentimiento.Trabajo => "📝 Consentimiento de trabajo",
        TipoConsentimiento.Trabajo_Menor => "📝 Consentimiento de trabajo (Menor)",
        _ => "📝 Consentimiento"
    };

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
    /// Indica si el PDF ya se generó (para mostrar opción Imprimir).
    /// </summary>
    [ObservableProperty]
    private bool _pdfGenerado;

    /// <summary>
    /// Ruta del PDF generado (para imprimir).
    /// </summary>
    private string? _rutaPdfGenerado;

    /// <summary>
    /// Indica si la opción de imprimir está habilitada.
    /// </summary>
    [ObservableProperty]
    private bool _usarImpresora;

    /// <summary>
    /// Indica si mostrar el botón Imprimir (PDF generado y impresora habilitada).
    /// </summary>
    public bool MostrarImprimir => PdfGenerado && UsarImpresora;

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

    #region Propiedades para menores (doble firma)

    /// <summary>
    /// Indica si es un consentimiento para menor de edad (requiere doble firma).
    /// </summary>
    [ObservableProperty]
    private bool _esConsentimientoMenor = false;

    /// <summary>
    /// Data URL de la firma del representante para previsualizar en el modal (consentimiento menor o única firma).
    /// </summary>
    [ObservableProperty]
    private string? _vistaPreviaRepresentanteDataUrl;

    /// <summary>
    /// Data URL de la firma del menor (solo consentimiento de menor con doble firma en el móvil).
    /// </summary>
    [ObservableProperty]
    private string? _vistaPreviaMenorDataUrl;

    /// <summary>
    /// Firma del menor en Base64 (solo fila en BD / PDF).
    /// </summary>
    [ObservableProperty]
    private string? _firmaMenorBase64;

    /// <summary>
    /// Fase de firma en móvil: un solo QR con dos cajas si es menor.
    /// </summary>
    public string TituloFaseFirma =>
        EsConsentimientoMenor
            ? "📱 Un solo enlace: REPRESENTANTE LEGAL arriba y MENOR abajo (dos cajas)."
            : "📱 Escanea el QR o abre la URL en el móvil para firmar.";

    /// <summary>
    /// Indica si el cliente es menor y no tiene datos del tutor.
    /// </summary>
    public bool FaltanDatosTutor => EsConsentimientoMenor && 
                                    Cliente != null && 
                                    !Cliente.TieneDatosTutor;

    #endregion

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
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                EstaProcesando = true;
                EstadoConexion = "🔄 Iniciando servidor...";
            });

            var servidorIniciado = await _firmaWebService.IniciarServidor().ConfigureAwait(false);
            if (!servidorIniciado)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    EstadoConexion =
                        "❌ No se pudo iniciar el servidor local. ¿Puerto ocupado? Prueba ejecutar como admin si el equipo bloquea el enlace.";
                });
                return;
            }

            _tokenActual = FirmaWebService.GenerarTokenUnico();

            var tituloMovil = TituloModal;

            _firmaWebService.RegistrarToken(_tokenActual!, TextoConsentimiento, tituloMovil,
                firmaMenorDosCajas: EsConsentimientoMenor);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                UrlFirma = _firmaWebService.GenerarUrlFirma(_tokenActual);
                QrCodeImage = QRCodeService.GenerarQRCode(UrlFirma, 300);

                _firmaWebService.FirmaRecibida -= OnFirmaRecibida;
                _firmaWebService.FirmaRecibida += OnFirmaRecibida;

                EstadoConexion = EsConsentimientoMenor
                    ? "✅ Servidor activo — Un solo QR: en el móvil, primero REPRESENTANTE LEGAL y debajo MENOR."
                    : "✅ Servidor activo - Escanea el código QR con tu móvil";
            });

            _ = Task.Run(async () => await EsperarFirma().ConfigureAwait(false));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al iniciar proceso de firma");
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                EstadoConexion = "❌ Error al iniciar el proceso de firma";
            });
        }
        finally
        {
            await Dispatcher.UIThread.InvokeAsync(() => EstaProcesando = false);
        }
    }

    /// <summary>
    /// Cancela el proceso de firma y cierra el modal.
    /// </summary>
    [RelayCommand]
    private void CancelarFirma()
    {
        CerrarModalYServidor();
    }

    /// <summary>
    /// Confirma la firma y genera el PDF.
    /// Para menores, maneja el flujo de doble firma.
    /// </summary>
    [RelayCommand]
    private async Task ConfirmarFirma()
    {
        if (EsConsentimientoMenor)
        {
            if (!FirmaRecibida ||
                string.IsNullOrWhiteSpace(FirmaMenorBase64) ||
                string.IsNullOrWhiteSpace(ImagenFirmaBase64))
            {
                Log.Warning("Intento de confirmar consentimiento menor sin ambas firmas");
                EstadoConexion = "⚠️ Falta alguna firma (representante o menor). Completa desde el móvil.";
                return;
            }
        }
        else if (!FirmaRecibida || string.IsNullOrEmpty(ImagenFirmaBase64))
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

            // Consentimiento de menor: representante legal (captura en mismo envío desde el móvil) + menor
            if (EsConsentimientoMenor)
            {
                _consentimiento.FirmaMenorBase64 = FirmaMenorBase64;
                _consentimiento.FirmaTutorBase64 = ImagenFirmaBase64;
                _consentimiento.NombreTutorFirmante = Cliente.NombreCompletoTutor;
                _consentimiento.DniTutorFirmante = Cliente.DniTutor;
            }

            // Generar ruta del PDF
            var rutaPdf = ConsentimientoPathService.ObtenerRutaCompletaPdf(
                Cliente.Id,
                TipoConsentimiento,
                Trabajo?.Id);

            // Generar PDF (para menores incluirá ambas firmas)
            var firmaParaPdf = EsConsentimientoMenor ? FirmaMenorBase64 : ImagenFirmaBase64;
            var firmaTutorParaPdf = EsConsentimientoMenor ? ImagenFirmaBase64 : null;
            
            var pdfGenerado = await ConsentimientoService.GenerarPdfConsentimiento(
                _consentimiento,
                firmaParaPdf ?? ImagenFirmaBase64,
                rutaPdf,
                firmaTutorParaPdf);

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
            _rutaPdfGenerado = rutaPdf;
            PdfGenerado = true;
            OnPropertyChanged(nameof(MostrarImprimir));

            OverlayNotificationService.Mostrar(
                "El archivo PDF del consentimiento se ha guardado. Puedes imprimirlo desde el mismo modal o cerrar cuando termines.",
                OverlayNotificationKind.Success,
                "¡PDF guardado!");

            // Disparar evento de firma completada
            FirmaCompletada?.Invoke(this, Cliente);

            // No cerrar automáticamente: el usuario puede imprimir o cerrar
            EstaProcesando = false;
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
    /// <param name="omitirConsentimientoIdRenovacion">
    /// Renovación: Id del consentimiento vigente que no debe reutilizarse (se creará una fila nueva).
    /// </param>
    public async Task AbrirModal(Cliente cliente, TipoConsentimiento tipo, Trabajo? trabajo = null,
        int? omitirConsentimientoIdRenovacion = null)
    {
        Cliente = cliente;
        TipoConsentimiento = tipo;
        Trabajo = trabajo;

        // Detectar si es consentimiento para menor de edad
        EsConsentimientoMenor = cliente.EsMenorDeEdad &&
                               (tipo == TipoConsentimiento.RGPD_Menor ||
                                tipo == TipoConsentimiento.Trabajo_Menor ||
                                tipo == TipoConsentimiento.Imagenes_Menor);

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
            using var context = new AtaenaDbContext();
            var configuracion = await context.Configuracion.FindAsync(1);
            if (configuracion == null)
            {
                configuracion = new Configuracion { NombreEstudio = "Ataena" };
            }

            TextoConsentimiento = ConsentimientoService.ReemplazarPlaceholders(
                plantilla,
                cliente,
                trabajo,
                configuracion,
                DateTime.Now);
        }

        // Cargar UsarImpresora
        using (var dbConfig = new AtaenaDbContext())
        {
            var cfg = await dbConfig.Configuracion.FirstOrDefaultAsync(c => c.Id == 1);
            UsarImpresora = cfg?.UsarImpresora ?? false;
        }

        // Crear o cargar consentimiento vigente (!Renovado). En renovaciones se puede excluir el Id sustituido.
        using var db = new AtaenaDbContext();
        var query = db.Consentimientos
            .Where(c => c.ClienteId == cliente.Id &&
                        c.Tipo == tipo &&
                        !c.Renovado &&
                        (trabajo == null || c.TrabajoId == trabajo.Id));
        if (omitirConsentimientoIdRenovacion.HasValue)
            query = query.Where(c => c.Id != omitirConsentimientoIdRenovacion.Value);

        _consentimiento = await query.OrderByDescending(c => c.Id).FirstOrDefaultAsync();

        if (_consentimiento == null)
        {
            _consentimiento = new Consentimiento
            {
                ClienteId = cliente.Id,
                Tipo = tipo,
                TrabajoId = trabajo?.Id,
                FechaFirma = DateTime.Now,
                Firmado = false,
                EsConsentimientoMenor = EsConsentimientoMenor,
                EdadClienteAlFirmar = cliente.Edad
            };
        }

        // Tras awaits de BD la continuación puede ser de un pool de hilos (sin sync context UI):
        // abrir modal, QR y bitmap deben asignarse en el hilo de la aplicación Avalonia.
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            LimpiarEstado();
            OnPropertyChanged(nameof(TituloFaseFirma));
            OnPropertyChanged(nameof(FaltanDatosTutor));
            EsVisible = true;
        });

        await IniciarFirmaCommand.ExecuteAsync(null);
    }

    /// <summary>
    /// Cierra el modal y limpia el estado.
    /// </summary>
    public void CerrarModal()
    {
        CerrarModalYServidor();
    }

    private void CerrarModalYServidor()
    {
        _firmaWebService.FirmaRecibida -= OnFirmaRecibida;
        _firmaWebService.DetenerServidor();
        EsVisible = false;
        LimpiarEstado();
        ModalSesionFinalizada?.Invoke(this, EventArgs.Empty);
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
            var firma = await _firmaWebService.EsperarFirma(_tokenActual, TimeSpan.FromMinutes(5))
                .ConfigureAwait(false);
            if (!string.IsNullOrEmpty(firma))
            {
                Log.Information("Firma recibida para token: {Token}", _tokenActual);
            }
            else
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    EstadoConexion = "⏱️ Tiempo de espera agotado. Intenta de nuevo.";
                });
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al esperar firma");
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                EstadoConexion = "❌ Error al recibir la firma";
            });
        }
    }

    /// <summary>
    /// Maneja el evento cuando se recibe una firma.
    /// </summary>
    private void OnFirmaRecibida(object? sender, FirmaRecibidaEventArgs e)
    {
        if (e.Token != _tokenActual)
            return;

        Dispatcher.UIThread.Post(() =>
        {
            if (e.Token != _tokenActual)
                return;

            if (EsConsentimientoMenor && e.EsFirmaConsentimientoMenorDual)
            {
                ImagenFirmaBase64 = e.ImagenBase64;
                FirmaMenorBase64 = e.ImagenMenorConsentimientoMenorDual!;
                FirmaRecibida = true;
                VistaPreviaRepresentanteDataUrl = ABase64SinPrefijoADataUri(e.ImagenBase64);
                VistaPreviaMenorDataUrl = ABase64SinPrefijoADataUri(e.ImagenMenorConsentimientoMenorDual);
                EstadoConexion =
                    "✅ Representante legal y menor firmaron en la misma página — revisa y pulsa «Generar PDF».";
                OverlayNotificationService.Mostrar(
                    "Ambas firmas recibidas. Comprueba la vista previa en el equipo y guarda el PDF.",
                    OverlayNotificationKind.Success,
                    "Firmas recibidas");
                return;
            }

            if (EsConsentimientoMenor)
            {
                FirmaRecibida = false;
                EstadoConexion =
                    "⚠️ Falta modo de firma dual en el móvil. Actualiza la app, recarga esta vista y rescanea el QR.";
                Log.Warning(
                    "Recibida firma no dual para consentimiento de menor — token puede ser página antigua.");
                return;
            }

            ImagenFirmaBase64 = e.ImagenBase64;
            FirmaRecibida = true;
            VistaPreviaRepresentanteDataUrl = ABase64SinPrefijoADataUri(e.ImagenBase64);
            VistaPreviaMenorDataUrl = null;
            EstadoConexion = "✅ Firma recibida - Revisa y confirma";
            OverlayNotificationService.Mostrar(
                "Revisa la firma en el modal y pulsa «Generar PDF» para guardarlo.",
                OverlayNotificationKind.Success,
                "¡Consentimiento firmado!");
        });
    }

    private static string? ABase64SinPrefijoADataUri(string? imagenSinPrefijo) =>
        string.IsNullOrWhiteSpace(imagenSinPrefijo)
            ? null
            : "data:image/png;base64," + imagenSinPrefijo.Trim();

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
        
        FirmaMenorBase64 = null;
        VistaPreviaRepresentanteDataUrl = null;
        VistaPreviaMenorDataUrl = null;

        // Limpiar estado post-generación
        PdfGenerado = false;
        _rutaPdfGenerado = null;
    }

    [RelayCommand]
    private async Task ImprimirPdf()
    {
        if (string.IsNullOrEmpty(_rutaPdfGenerado) || !System.IO.File.Exists(_rutaPdfGenerado))
        {
            EstadoConexion = "❌ No hay PDF para imprimir";
            return;
        }

        try
        {
            EstaProcesando = true;
            EstadoConexion = "🖨️ Enviando a imprimir...";

            var (exito, mensaje) = await ImpresorService.ImprimirPdfAsync(_rutaPdfGenerado);

            if (exito)
            {
                EstadoConexion = "✅ " + mensaje;
            }
            else
            {
                EstadoConexion = "❌ " + mensaje;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al imprimir PDF");
            EstadoConexion = $"❌ Error al imprimir: {ex.Message}";
        }
        finally
        {
            EstaProcesando = false;
        }
    }

    #endregion
}

