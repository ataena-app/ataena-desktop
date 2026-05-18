using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using System.Text.Json;
using Serilog;

namespace Ataena.Services;

/// <summary>
/// Servicio para gestionar el servidor HTTP local que recibe firmas desde dispositivos móviles.
/// </summary>
/// <remarks>
/// El servidor se inicia en un puerto local (por defecto 8080) y permite que dispositivos
/// móviles en la misma red WiFi se conecten para firmar consentimientos.
/// Una sola instancia por proceso evita conflicto (<see cref="HttpListenerException"/> 32).
/// </remarks>
public class FirmaWebService : IDisposable
{
    /// <summary>Una única instancia por proceso para no intentar enlazar dos veces el mismo puerto (error 32).</summary>
    public static FirmaWebService InstanciaCompartida { get; } = new();

    private HttpListener? _listener;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _serverTask;
    private bool _disposed = false;

    // Almacenar firmas recibidas por token
    private readonly ConcurrentDictionary<string, FirmaRecibida> _firmasRecibidas = new();
    
    // Almacenar tokens activos con su expiración
    private readonly ConcurrentDictionary<string, DateTime> _tokensActivos = new();

    // Texto del consentimiento asociado a cada token (para mostrarlo en el móvil)
    private readonly ConcurrentDictionary<string, ContextoFirma> _contextoPorToken = new();

    private const int PuertoPorDefecto = 8080;
    /// <summary>Si el puerto preferido está ocupado (WIN32 32 / 183), se prueban sucesivamente más puertos TCP.</summary>
    private const int MaxPuertosAlternativos = 32;
    private const int TimeoutTokenMinutos = 10; // Los tokens expiran después de 10 minutos
    private static readonly HashSet<int> PuertosFirewallConfigurados = new();

    /// <summary>
    /// Evento que se dispara cuando se recibe una firma.
    /// </summary>
    public event EventHandler<FirmaRecibidaEventArgs>? FirmaRecibida;

    /// <summary>
    /// Indica si el servidor está actualmente ejecutándose.
    /// </summary>
    public bool EstaActivo => _listener?.IsListening ?? false;

    /// <summary>
    /// IP local del servidor (se establece al iniciar).
    /// </summary>
    public string? IpLocal { get; private set; }

    /// <summary>
    /// Puerto en el que está escuchando el servidor.
    /// </summary>
    public int Puerto { get; private set; }

    /// <summary>
    /// URL base del servidor (http://{IP}:{Puerto}).
    /// </summary>
    public string? UrlBase { get; private set; }

    /// <summary>
    /// Obtiene la IP local del PC automáticamente.
    /// </summary>
    /// <returns>IP local como string, o null si no se puede obtener.</returns>
    public static string? DetectarIPLocal()
    {
        try
        {
            // Obtener todas las direcciones IP
            var host = Dns.GetHostEntry(Dns.GetHostName());
            
            foreach (var ip in host.AddressList)
            {
                // Filtrar solo IPv4 y que no sea localhost
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
                    !IPAddress.IsLoopback(ip))
                {
                    var ipString = ip.ToString();
                    Log.Information("IP local detectada: {IP}", ipString);
                    return ipString;
                }
            }

            // Si no se encuentra ninguna, usar localhost
            Log.Warning("No se encontró IP local, usando localhost");
            return "127.0.0.1";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al detectar IP local");
            return "127.0.0.1"; // Fallback a localhost
        }
    }

    /// <summary>
    /// Genera un token único para una sesión de firma.
    /// </summary>
    /// <returns>Token único (GUID sin guiones).</returns>
    public static string GenerarTokenUnico()
    {
        var token = Guid.NewGuid().ToString("N"); // Sin guiones
        Log.Debug("Token único generado: {Token}", token);
        return token;
    }

    /// <summary>
    /// Genera la URL completa para una sesión de firma.
    /// </summary>
    /// <param name="token">Token único de la sesión.</param>
    /// <returns>URL completa (http://{IP}:{Puerto}/firma/{token}).</returns>
    public string GenerarUrlFirma(string token)
    {
        if (string.IsNullOrEmpty(UrlBase))
            throw new InvalidOperationException("El servidor no está iniciado. Llama a IniciarServidor() primero.");

        var url = $"{UrlBase}/firma/{token}";
        Log.Debug("URL de firma generada: {URL}", url);
        return url;
    }

    /// <summary>
    /// Genera la URL completa para una sesión de captura de foto.
    /// </summary>
    /// <param name="token">Token único de la sesión.</param>
    /// <returns>URL completa (http://{IP}:{Puerto}/foto/{token}).</returns>
    public string GenerarUrlFoto(string token)
    {
        if (string.IsNullOrEmpty(UrlBase))
            throw new InvalidOperationException("El servidor no está iniciado. Llama a IniciarServidor() primero.");

        var url = $"{UrlBase}/foto/{token}";
        Log.Debug("URL de foto generada: {URL}", url);
        return url;
    }

    /// <summary>
    /// Configura automáticamente el firewall de Windows para permitir conexiones en el puerto especificado.
    /// </summary>
    /// <param name="puerto">Puerto a abrir en el firewall.</param>
    private static void ConfigurarFirewall(int puerto)
    {
        try
        {
            // Verificar si la regla ya existe
            var nombreRegla = $"Ataena_HTTP_Puerto_{puerto}";
            var procesoVerificar = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = $"advfirewall firewall show rule name=\"{nombreRegla}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            procesoVerificar.Start();
            var salida = procesoVerificar.StandardOutput.ReadToEnd();
            procesoVerificar.WaitForExit();

            // Si la regla ya existe, no hacer nada
            if (salida.Contains(nombreRegla) && salida.Contains("Enabled:                              Yes"))
            {
                Log.Information("Regla de firewall ya existe y está habilitada para el puerto {Puerto}", puerto);
                return;
            }

            // Crear la regla del firewall
            Log.Information("Configurando firewall de Windows para permitir conexiones en el puerto {Puerto}...", puerto);
            
            var proceso = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = $"advfirewall firewall add rule name=\"{nombreRegla}\" dir=in action=allow protocol=TCP localport={puerto}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    Verb = "runas" // Ejecutar como administrador si es posible
                }
            };

            try
            {
                proceso.Start();
                var output = proceso.StandardOutput.ReadToEnd();
                var error = proceso.StandardError.ReadToEnd();
                proceso.WaitForExit();

                if (proceso.ExitCode == 0)
                {
                    Log.Information("✅ Regla de firewall creada exitosamente para el puerto {Puerto}", puerto);
                }
                else
                {
                    // Si falla por permisos, intentar sin runas (puede que ya tenga permisos)
                    Log.Warning("No se pudo crear la regla de firewall con permisos elevados. Intentando sin elevación...");
                    
                    proceso.StartInfo.Verb = null;
                    proceso.Start();
                    output = proceso.StandardOutput.ReadToEnd();
                    error = proceso.StandardError.ReadToEnd();
                    proceso.WaitForExit();

                    if (proceso.ExitCode == 0)
                    {
                        Log.Information("✅ Regla de firewall creada exitosamente para el puerto {Puerto}", puerto);
                    }
                    else
                    {
                        Log.Warning("⚠️ No se pudo crear la regla de firewall automáticamente. Código: {Codigo}, Error: {Error}. " +
                                   "El usuario puede necesitar ejecutar la aplicación como administrador o configurar el firewall manualmente.",
                                   proceso.ExitCode, error);
                    }
                }
            }
            catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
            {
                // Usuario canceló el UAC
                Log.Warning("⚠️ Se canceló la elevación de permisos. El firewall puede necesitar configuración manual.");
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "⚠️ Error al configurar el firewall automáticamente. " +
                          "El usuario puede necesitar configurar el firewall manualmente para permitir conexiones en el puerto {Puerto}.", puerto);
        }
    }

    /// <summary>
    /// Inicia el servidor HTTP local.
    /// </summary>
    /// <param name="puerto">Puerto en el que escuchar (por defecto 8080).</param>
    /// <returns>True si se inició correctamente, False en caso contrario.</returns>
    public Task<bool> IniciarServidor(int puerto = PuertoPorDefecto)
    {
        if (_listener?.IsListening ?? false)
        {
            Log.Warning("El servidor ya está activo en el puerto {Puerto}", Puerto);
            return Task.FromResult(true);
        }

        return Task.Run(() =>
        {
            try
            {
                LiberarListenerHuerfano();

                // Evitar CTS/task colgados si un arranque anterior falló a medias
                try
                {
                    _cancellationTokenSource?.Cancel();
                }
                catch
                {
                    // ok
                }

                try
                {
                    _cancellationTokenSource?.Dispose();
                }
                catch
                {
                    // ok
                }

                _cancellationTokenSource = null;
                _serverTask = null;

                IpLocal = DetectarIPLocal();
                if (string.IsNullOrEmpty(IpLocal))
                {
                    Log.Error("No se pudo detectar la IP local");
                    return false;
                }

                // WIN32 32 = recurso compartido (puerto/prefijo http.sys ocupado); 183 = ya existe (mismo efecto práctico).
                const int errorPuertoEnUso = 32;
                const int errorYaExiste = 183;

                for (var intento = 0; intento < MaxPuertosAlternativos; intento++)
                {
                    Puerto = puerto + intento;
                    var urlQr = $"http://{IpLocal}:{Puerto}";

                    if (PuertosFirewallConfigurados.Add(Puerto))
                        ConfigurarFirewall(Puerto);

                    HttpListener? nuevo = null;
                    try
                    {
                        nuevo = new HttpListener();
                        try
                        {
                            nuevo.Prefixes.Add($"http://*:{Puerto}/");
                            if (intento == 0)
                                Log.Information("Intentando enlazar en el puerto {Puerto} (todas las interfaces)", Puerto);
                            else
                                Log.Information(
                                    "Probando otro puerto libre: {Puerto} (intento {Intento}/{Max})",
                                    Puerto,
                                    intento + 1,
                                    MaxPuertosAlternativos);
                        }
                        catch (HttpListenerException exPref)
                        {
                            Log.Warning(
                                exPref,
                                "Sin prefijo *:{Puerto} (permisos/URLACL). Usando localhost; el QR con la LAN puede fallar hasta resolver permisos.",
                                Puerto);
                            nuevo.Prefixes.Add($"http://localhost:{Puerto}/");
                            urlQr = $"http://localhost:{Puerto}";
                        }

                        nuevo.Start();
                        _listener = nuevo;
                        nuevo = null;
                        UrlBase = urlQr;

                        Log.Information("═══════════════════════════════════════════════════════");
                        Log.Information("Servidor HTTP local iniciado");
                        Log.Information("IP: {IP}", IpLocal);
                        Log.Information("Puerto: {Puerto}", Puerto);
                        Log.Information("URL Base (QR): {URL}", UrlBase);
                        Log.Information("═══════════════════════════════════════════════════════");

                        _cancellationTokenSource = new CancellationTokenSource();
                        var cancellationToken = _cancellationTokenSource.Token;
                        _ = Task.Run(() => LimpiarTokensExpirados(cancellationToken));
                        _serverTask = Task.Run(() => ProcesarPeticiones(cancellationToken));

                        return true;
                    }
                    catch (HttpListenerException ex) when (ex.ErrorCode == errorPuertoEnUso || ex.ErrorCode == errorYaExiste)
                    {
                        Log.Warning(
                            "Puerto {Puerto} no disponible (código Win32 {Codigo}). Suele estar ocupado por otra aplicación o otra sesión.",
                            Puerto,
                            ex.ErrorCode);
                        LiberarIntentoListener(ref nuevo);
                        continue;
                    }
                    catch (HttpListenerException ex)
                    {
                        Log.Error(ex, "HttpListener rechazado en puerto {Puerto} (código {Codigo})", Puerto, ex.ErrorCode);
                        LiberarIntentoListener(ref nuevo);
                        LiberarListenerHuerfano();
                        return false;
                    }
                    finally
                    {
                        LiberarIntentoListener(ref nuevo);
                    }
                }

                Log.Error(
                    "No se encontró ningún puerto libre entre {Desde} y {Hasta}. Cierra otros programas en esos puertos o reinicia tras un cierre abrupto.",
                    puerto,
                    puerto + MaxPuertosAlternativos - 1);
                return false;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error inesperado al iniciar el servidor HTTP");
                return false;
            }
        });
    }

    private static void LiberarIntentoListener(ref HttpListener? l)
    {
        if (l == null)
            return;
        try { l.Abort(); } catch { /* ok */ }
        try { l.Close(); } catch { /* ok */ }
        l = null;
    }

    private void LiberarListenerHuerfano()
    {
        if (_listener == null || _listener.IsListening)
            return;
        try { _listener.Abort(); } catch { /* ok */ }
        try { _listener.Close(); } catch { /* ok */ }
        _listener = null;
    }


    /// <summary>
    /// Registra un token como activo para una sesión de firma.
    /// </summary>
    /// <param name="token">Token único de la sesión.</param>
    public void RegistrarToken(string token)
    {
        _tokensActivos[token] = DateTime.Now.AddMinutes(TimeoutTokenMinutos);
        Log.Debug("Token registrado: {Token} (expira en {Minutos} minutos)", token, TimeoutTokenMinutos);
    }

    /// <summary>
    /// Registra un token y asocia el texto de consentimiento y un título que se mostrarán
    /// en la página de firma del móvil para que el cliente pueda leerlo antes de firmar.
    /// </summary>
    /// <param name="token">Token único de la sesión.</param>
    /// <param name="textoConsentimiento">Texto completo del consentimiento (ya con placeholders sustituidos).</param>
    /// <param name="titulo">Título a mostrar en la cabecera (p.ej. "Consentimiento RGPD").</param>
    /// <param name="firmaMenorDosCajas">
    /// Si es true, en el mismo enlace aparecen dos cajas en el móvil: representante legal y menor.
    /// </param>
    public void RegistrarToken(string token, string textoConsentimiento, string? titulo = null, bool firmaMenorDosCajas = false)
    {
        RegistrarToken(token);
        _contextoPorToken[token] = new ContextoFirma
        {
            Texto = textoConsentimiento ?? string.Empty,
            Titulo = string.IsNullOrWhiteSpace(titulo) ? "Consentimiento" : titulo!,
            FirmaMenorDosCajasEnMismaPagina = firmaMenorDosCajas,
        };
    }

    /// <summary>
    /// Espera a recibir una firma para un token específico.
    /// </summary>
    /// <param name="token">Token de la sesión.</param>
    /// <param name="timeout">Tiempo máximo de espera (por defecto 5 minutos).</param>
    /// <returns>Imagen de la firma en base64, o null si expira el timeout.</returns>
    public async Task<string?> EsperarFirma(string token, TimeSpan? timeout = null)
    {
        var tiempoEspera = timeout ?? TimeSpan.FromMinutes(5);
        var inicio = DateTime.Now;

        Log.Information("Esperando firma para token: {Token} (timeout: {Timeout} minutos)", 
            token, tiempoEspera.TotalMinutes);

        while (DateTime.Now - inicio < tiempoEspera)
        {
            // PRIMERO verificar si ya se recibió la firma (puede que se haya recibido antes de esta verificación)
            if (_firmasRecibidas.TryGetValue(token, out var firma))
            {
                Log.Information(
                    "Firma/foto recibida para token: {Token}, tamaño representante/uni: {TamañoRep}, tamaño menor: {TamañoMenor}",
                    token,
                    firma.ImagenBase64?.Length ?? 0,
                    firma.ImagenFirmaMenorDual?.Length ?? 0);
                return firma.ImagenFirmaRepresentanteLegal ?? firma.ImagenBase64;
            }

            // LUEGO verificar si el token expiró (pero solo si no hay firma recibida)
            if (!_tokensActivos.ContainsKey(token))
            {
                // Si el token no está activo pero tampoco hay firma recibida, puede que haya expirado
                // Pero esperamos un poco más por si acaso la firma está llegando
                await Task.Delay(1000);
                // Verificar una vez más después de esperar
                if (_firmasRecibidas.TryGetValue(token, out var firmaRetrasada))
                {
                    Log.Information(
                        "Firma/foto recibida para token: {Token} (con retraso). Rep/uni: {TamañoRep}, menor: {TamañoMenor}",
                        token,
                        firmaRetrasada.ImagenBase64?.Length ?? 0,
                        firmaRetrasada.ImagenFirmaMenorDual?.Length ?? 0);
                    return firmaRetrasada.ImagenFirmaRepresentanteLegal ?? firmaRetrasada.ImagenBase64;
                }
                Log.Warning("Token expirado o inválido: {Token}", token);
                return null;
            }

            await Task.Delay(500); // Esperar 500ms antes de verificar de nuevo
        }

        Log.Warning("Timeout esperando firma para token: {Token}", token);
        return null;
    }

    /// <summary>
    /// Detiene el servidor HTTP.
    /// </summary>
    public void DetenerServidor()
    {
        try
        {
            if (_listener?.IsListening != true)
            {
                Log.Debug("El servidor no está activo o ya estaba detenido");
                LiberarListenerHuerfano();
                return;
            }

            _cancellationTokenSource?.Cancel();
            Task.Delay(100).Wait();

            try
            {
                _listener.Stop();
            }
            catch (ObjectDisposedException)
            {
                // Ok
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Stop del HttpListener durante detención");
            }

            try
            {
                _serverTask?.Wait(TimeSpan.FromSeconds(2));
            }
            catch (Exception ex)
            {
                Log.Debug("Esperando el task del servidor: {Msg}", ex.Message);
            }

            try
            {
                _listener?.Close();
            }
            catch (ObjectDisposedException)
            {
                Log.Debug("Listener ya cerrado al detener");
            }

            Log.Information("Servidor HTTP local detenido");
        }
        catch (ObjectDisposedException)
        {
            Log.Debug("El listener ya estaba cerrado");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al detener el servidor HTTP");
        }
        finally
        {
            try
            {
                _cancellationTokenSource?.Dispose();
            }
            catch
            {
                // ok
            }

            _cancellationTokenSource = null;
            _listener = null;
            _serverTask = null;
        }
    }

    /// <summary>
    /// Procesa las peticiones HTTP entrantes.
    /// </summary>
    private async Task ProcesarPeticiones(CancellationToken cancellationToken)
    {
        if (_listener == null) return;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Verificar que el listener sigue activo antes de esperar peticiones
                if (!_listener.IsListening)
                {
                    Log.Debug("Listener detenido, saliendo del bucle de procesamiento");
                    break;
                }

                // Esperar petición (con timeout)
                var context = await _listener.GetContextAsync();
                _ = Task.Run(() => ManejarPeticion(context, cancellationToken));
            }
            catch (ObjectDisposedException)
            {
                // El listener fue cerrado/disposed, salir normalmente
                Log.Debug("Listener cerrado, saliendo del bucle de procesamiento");
                break;
            }
            catch (HttpListenerException ex) when (ex.ErrorCode == 995)
            {
                // Error 995 = operación cancelada (normal al cerrar)
                Log.Debug("Servidor HTTP cerrado");
                break;
            }
            catch (InvalidOperationException)
            {
                // El listener no está escuchando o fue cerrado
                Log.Debug("Listener no está escuchando, saliendo del bucle");
                break;
            }
            catch (Exception ex)
            {
                // Solo loguear si el listener sigue activo
                if (_listener?.IsListening == true)
                {
                    Log.Error(ex, "Error al procesar petición HTTP");
                }
                else
                {
                    Log.Debug("Error al procesar petición (listener cerrado): {Error}", ex.Message);
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Maneja una petición HTTP individual.
    /// </summary>
    private async Task ManejarPeticion(HttpListenerContext context, CancellationToken cancellationToken)
    {
        var request = context.Request;
        var response = context.Response;

        try
        {
            var path = request.Url?.AbsolutePath ?? "";
            var remoteIp = request.RemoteEndPoint?.Address?.ToString() ?? "desconocida";
            Log.Information("Petición recibida: {Method} {Path} desde IP: {IP}", request.HttpMethod, path, remoteIp);

            // OPTIONS - Preflight CORS
            if (request.HttpMethod == "OPTIONS")
            {
                response.Headers.Add("Access-Control-Allow-Origin", "*");
                response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
                response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");
                response.StatusCode = 200;
                response.Close();
                return;
            }
            
            // GET /firma/{token} - Servir página HTML de firma
            if (request.HttpMethod == "GET" && path.StartsWith("/firma/"))
            {
                var token = path.Substring("/firma/".Length);
                await ServirPaginaFirma(context, token);
            }
            // GET /foto/{token} - Servir página HTML de foto
            else if (request.HttpMethod == "GET" && path.StartsWith("/foto/"))
            {
                var token = path.Substring("/foto/".Length);
                await ServirPaginaFoto(context, token);
            }
            // GET /styles.css - Servir CSS
            else if (request.HttpMethod == "GET" && path == "/styles.css")
            {
                await ServirArchivoEstatico(context, "styles.css", "text/css");
            }
            // GET /signature.js - Servir JavaScript de firma
            else if (request.HttpMethod == "GET" && path == "/signature.js")
            {
                await ServirArchivoEstatico(context, "signature.js", "application/javascript");
            }
            // GET /photo.js - Servir JavaScript de foto
            else if (request.HttpMethod == "GET" && path == "/photo.js")
            {
                await ServirArchivoEstatico(context, "photo.js", "application/javascript");
            }
            // POST /firma/{token} - Recibir firma
            else if (request.HttpMethod == "POST" && path.StartsWith("/firma/"))
            {
                var token = path.Substring("/firma/".Length);
                await RecibirFirma(request, response, token);
            }
            // POST /foto/{token} - Recibir foto (mismo formato que firma)
            else if (request.HttpMethod == "POST" && path.StartsWith("/foto/"))
            {
                var token = path.Substring("/foto/".Length);
                await RecibirFirma(request, response, token);
            }
            else
            {
                // 404 Not Found
                response.StatusCode = 404;
                response.Close();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al manejar petición HTTP");
            try
            {
                response.StatusCode = 500;
                var errorBytes = Encoding.UTF8.GetBytes("Error interno del servidor");
                response.OutputStream.Write(errorBytes);
                response.Close();
            }
            catch
            {
                // Ignorar errores al enviar respuesta de error
            }
        }
    }

    /// <summary>
    /// Sirve la página HTML de firma.
    /// </summary>
    private async Task ServirPaginaFirma(HttpListenerContext context, string token)
    {
        var response = context.Response;
        var request = context.Request;
        
        // Verificar que el token es válido
        if (!_tokensActivos.ContainsKey(token))
        {
            response.StatusCode = 404;
            response.Close();
            return;
        }

        try
        {
            var rutaWwwRoot = ConsentimientoPathService.ObtenerRutaWwwRoot();
            var rutaHtml = Path.Combine(rutaWwwRoot, "firma.html");

            if (!File.Exists(rutaHtml))
            {
                Log.Warning("Archivo HTML no encontrado: {Ruta}", rutaHtml);
                response.StatusCode = 404;
                response.Close();
                return;
            }

            var html = await File.ReadAllTextAsync(rutaHtml);
            
            // Reemplazar placeholder del token si existe
            html = html.Replace("{TOKEN}", token);

            // Reemplazar texto del consentimiento y título (si están registrados)
            string textoHtml;
            string tituloHtml;
            string firmaDobleValor;
            string subtituloMovil;
            if (_contextoPorToken.TryGetValue(token, out var ctx))
            {
                textoHtml = WebUtility.HtmlEncode(ctx.Texto);
                tituloHtml = WebUtility.HtmlEncode(ctx.Titulo);
                firmaDobleValor = ctx.FirmaMenorDosCajasEnMismaPagina ? "true" : "false";
                subtituloMovil = WebUtility.HtmlEncode(
                    ctx.FirmaMenorDosCajasEnMismaPagina
                        ? "Representante legal y menor: mismo documento; dos firmas arriba y abajo en esta página."
                        : "Lee el documento, marca que lo aceptas y firma abajo.");
            }
            else
            {
                textoHtml = string.Empty;
                tituloHtml = "Consentimiento";
                firmaDobleValor = "false";
                subtituloMovil = WebUtility.HtmlEncode("Lee el documento, marca que lo aceptas y firma abajo.");
            }
            html = html.Replace("{CONSENT_TEXT}", textoHtml);
            html = html.Replace("{CONSENT_TITLE}", tituloHtml);

            html = html.Replace("{FIRMA_MENOR_DOS_CAJAS}", firmaDobleValor);
            html = html.Replace("{SUBTITLE_MOBILE}", subtituloMovil);

            // Agregar headers CORS y otros necesarios para móviles
            response.Headers.Add("Access-Control-Allow-Origin", "*");
            response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
            response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");
            response.Headers.Add("Cache-Control", "no-cache, no-store, must-revalidate");
            response.Headers.Add("Pragma", "no-cache");
            response.Headers.Add("Expires", "0");
            
            response.ContentType = "text/html; charset=utf-8";
            response.StatusCode = 200;
            
            var bytes = Encoding.UTF8.GetBytes(html);
            await response.OutputStream.WriteAsync(bytes);
            response.Close();
            
            Log.Information("Página HTML servida para token: {Token} desde IP: {IP}", token, request.RemoteEndPoint?.Address);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al servir página HTML");
            response.StatusCode = 500;
            response.Close();
        }
    }

    /// <summary>
    /// Sirve la página HTML de captura de foto.
    /// </summary>
    private async Task ServirPaginaFoto(HttpListenerContext context, string token)
    {
        var response = context.Response;
        var request = context.Request;

        // Verificar que el token es válido
        if (!_tokensActivos.ContainsKey(token))
        {
            response.StatusCode = 404;
            response.Close();
            return;
        }

        try
        {
            var rutaWwwRoot = ConsentimientoPathService.ObtenerRutaWwwRoot();
            var rutaHtml = Path.Combine(rutaWwwRoot, "photo.html");

            if (!File.Exists(rutaHtml))
            {
                Log.Warning("Archivo HTML de foto no encontrado: {Ruta}", rutaHtml);
                response.StatusCode = 404;
                response.Close();
                return;
            }

            var html = await File.ReadAllTextAsync(rutaHtml);

            // Agregar headers CORS y de caché
            response.Headers.Add("Access-Control-Allow-Origin", "*");
            response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
            response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");
            response.Headers.Add("Cache-Control", "no-cache, no-store, must-revalidate");
            response.Headers.Add("Pragma", "no-cache");
            response.Headers.Add("Expires", "0");

            response.ContentType = "text/html; charset=utf-8";
            response.StatusCode = 200;

            var bytes = Encoding.UTF8.GetBytes(html);
            await response.OutputStream.WriteAsync(bytes);
            response.Close();

            Log.Information("Página HTML de foto servida para token: {Token} desde IP: {IP}", token, request.RemoteEndPoint?.Address);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al servir página HTML de foto");
            response.StatusCode = 500;
            response.Close();
        }
    }

    /// <summary>
    /// Sirve un archivo estático (CSS, JS, etc.).
    /// </summary>
    private async Task ServirArchivoEstatico(HttpListenerContext context, string nombreArchivo, string contentType)
    {
        var response = context.Response;
        try
        {
            var rutaWwwRoot = ConsentimientoPathService.ObtenerRutaWwwRoot();
            var rutaArchivo = Path.Combine(rutaWwwRoot, nombreArchivo);

            if (!File.Exists(rutaArchivo))
            {
                Log.Warning("Archivo estático no encontrado: {Ruta}", rutaArchivo);
                response.StatusCode = 404;
                response.Close();
                return;
            }

            // Agregar headers CORS (sin caché larga en JS de firma: el móvil debe recibir la versión actual)
            response.Headers.Add("Access-Control-Allow-Origin", "*");
            if (nombreArchivo.Equals("signature.js", StringComparison.OrdinalIgnoreCase) ||
                nombreArchivo.Equals("firma.html", StringComparison.OrdinalIgnoreCase))
            {
                response.Headers.Add("Cache-Control", "no-cache, no-store, must-revalidate");
                response.Headers.Add("Pragma", "no-cache");
                response.Headers.Add("Expires", "0");
            }
            else
            {
                response.Headers.Add("Cache-Control", "public, max-age=3600");
            }

            var contenido = await File.ReadAllTextAsync(rutaArchivo);
            response.ContentType = $"{contentType}; charset=utf-8";
            response.StatusCode = 200;

            var bytes = Encoding.UTF8.GetBytes(contenido);
            await response.OutputStream.WriteAsync(bytes);
            response.Close();

            Log.Debug("Archivo estático servido: {Archivo} desde IP: {IP}", nombreArchivo, context.Request.RemoteEndPoint?.Address);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al servir archivo estático: {Archivo}", nombreArchivo);
            response.StatusCode = 500;
            response.Close();
        }
    }

    /// <summary>
    /// Recibe una firma desde el dispositivo móvil.
    /// </summary>
    private async Task RecibirFirma(HttpListenerRequest request, HttpListenerResponse response, string token)
    {
        // Verificar que el token es válido
        if (!_tokensActivos.ContainsKey(token))
        {
            response.StatusCode = 404;
            response.Close();
            return;
        }

        try
        {
            // Leer el body de la petición
            using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
            var body = (await reader.ReadToEndAsync()).Trim();

            var firma = new FirmaRecibida { Token = token, FechaRecepcion = DateTime.Now };

            static string? SoloBase64DesdeCampo(string? valor)
            {
                if (string.IsNullOrEmpty(valor)) return null;
                var s = valor.Trim();
                if (s.StartsWith("data:image", StringComparison.OrdinalIgnoreCase))
                {
                    var comma = s.IndexOf(',');
                    if (comma > 0)
                        return s.Substring(comma + 1);
                }

                return s;
            }

            if (body.Length > 0 && body.StartsWith('{'))
            {
                try
                {
                    var dto = JsonSerializer.Deserialize<FirmaDobleMenorPayload>(body);
                    var fiRep = SoloBase64DesdeCampo(dto?.FirmaRepresentanteLegal);
                    var fiMen = SoloBase64DesdeCampo(dto?.FirmaMenor);
                    if (string.IsNullOrEmpty(fiRep) || string.IsNullOrEmpty(fiMen))
                        throw new InvalidOperationException("Faltan firmas dual en JSON.");

                    firma.ImagenFirmaRepresentanteLegal = fiRep!;
                    firma.ImagenFirmaMenorDual = fiMen!;
                    firma.ImagenBase64 = fiRep!;
                }
                catch (Exception exParse)
                {
                    Log.Warning(exParse, "Body JSON dual inválido, se esperaba firmaRepresentanteLegal/firmaMenor");
                    response.StatusCode = 400;
                    response.Close();
                    return;
                }
            }
            else
            {
                var imagenBase64 = body;
                if (imagenBase64.StartsWith("data:image", StringComparison.OrdinalIgnoreCase))
                {
                    var base64Index = imagenBase64.IndexOf(',');
                    if (base64Index > 0)
                        imagenBase64 = imagenBase64.Substring(base64Index + 1);
                }

                firma.ImagenBase64 = imagenBase64;
            }

            _firmasRecibidas[token] = firma;

            // Eliminar el token de activos (ya se recibió la firma)
            _tokensActivos.TryRemove(token, out _);
            _contextoPorToken.TryRemove(token, out _);

            // Disparar evento
            var args = new FirmaRecibidaEventArgs { Token = token, ImagenBase64 = firma.ImagenBase64 };
            if (!string.IsNullOrEmpty(firma.ImagenFirmaMenorDual) &&
                !string.IsNullOrEmpty(firma.ImagenFirmaRepresentanteLegal))
            {
                args.ImagenMenorConsentimientoMenorDual = firma.ImagenFirmaMenorDual;
                args.ImagenBase64 = firma.ImagenFirmaRepresentanteLegal!;
            }

            FirmaRecibida?.Invoke(this, args);

            Log.Information(
                "Firma/foto recibida para token: {Token}, rep/uni: {TRep}, menor_dual: {TMenor}",
                token,
                firma.ImagenBase64.Length,
                firma.ImagenFirmaMenorDual?.Length ?? 0);

            // Responder con éxito
            response.StatusCode = 200;
            response.ContentType = "application/json";
            var respuesta = Encoding.UTF8.GetBytes("{\"success\": true}");
            await response.OutputStream.WriteAsync(respuesta);
            response.Close();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al recibir firma");
            response.StatusCode = 500;
            response.Close();
        }
    }

    /// <summary>
    /// Limpia tokens expirados periódicamente.
    /// </summary>
    private async Task LimpiarTokensExpirados(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var ahora = DateTime.Now;
                var tokensExpirados = new List<string>();

                foreach (var kvp in _tokensActivos)
                {
                    if (kvp.Value < ahora)
                    {
                        tokensExpirados.Add(kvp.Key);
                    }
                }

                foreach (var token in tokensExpirados)
                {
                    _tokensActivos.TryRemove(token, out _);
                    _firmasRecibidas.TryRemove(token, out _);
                    _contextoPorToken.TryRemove(token, out _);
                    Log.Debug("Token expirado eliminado: {Token}", token);
                }

                await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error al limpiar tokens expirados");
            }
        }
    }

    /// <summary>
    /// Libera los recursos del servicio.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        DetenerServidor();
        _cancellationTokenSource?.Dispose();
        _listener?.Close();
        
        _disposed = true;
    }
}

/// <summary>
/// Clase para almacenar una firma recibida.
/// </summary>
internal class FirmaRecibida
{
    public string Token { get; set; } = string.Empty;
    /// <summary>Firma única (mayor) o, en dual menor, repetimos el representante para compatibilidad.</summary>
    public string ImagenBase64 { get; set; } = string.Empty;
    public DateTime FechaRecepcion { get; set; }

    /// <summary>Consentimiento menor: segunda firma capturada en la misma página del móvil.</summary>
    public string? ImagenFirmaMenorDual { get; set; }

    public string? ImagenFirmaRepresentanteLegal { get; set; }
}

/// <summary>
/// Contexto adicional asociado a una sesión de firma (texto del consentimiento, título).
/// </summary>
internal class ContextoFirma
{
    public string Texto { get; set; } = string.Empty;
    public string Titulo { get; set; } = "Consentimiento";
    public bool FirmaMenorDosCajasEnMismaPagina { get; set; }
}

/// <summary>
/// Argumentos del evento cuando se recibe una firma.
/// </summary>
public class FirmaRecibidaEventArgs : EventArgs
{
    public string Token { get; set; } = string.Empty;
    /// <summary>Representante (dual menor), firma única (mayores) o foto DNI capturada igual.</summary>
    public string ImagenBase64 { get; set; } = string.Empty;

    /// <summary>Si viene relleno junto con <see cref="ImagenBase64"/>, firma menor en mismo envío desde el móvil.</summary>
    public string? ImagenMenorConsentimientoMenorDual { get; set; }

    public bool EsFirmaConsentimientoMenorDual =>
        !string.IsNullOrEmpty(ImagenBase64) && !string.IsNullOrEmpty(ImagenMenorConsentimientoMenorDual);
}

internal sealed class FirmaDobleMenorPayload
{
    [JsonPropertyName("firmaRepresentanteLegal")]
    public string? FirmaRepresentanteLegal { get; set; }

    [JsonPropertyName("firmaMenor")]
    public string? FirmaMenor { get; set; }
}
