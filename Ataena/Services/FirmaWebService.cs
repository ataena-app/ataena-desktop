using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace Ataena.Services;

/// <summary>
/// Servicio para gestionar el servidor HTTP local que recibe firmas desde dispositivos móviles.
/// </summary>
/// <remarks>
/// El servidor se inicia en un puerto local (por defecto 8080) y permite que dispositivos
/// móviles en la misma red WiFi se conecten para firmar consentimientos.
/// </remarks>
public class FirmaWebService : IDisposable
{
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
    private const int TimeoutTokenMinutos = 10; // Los tokens expiran después de 10 minutos
    private static bool _firewallConfigurado = false; // Flag para evitar configurar múltiples veces

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
            Log.Warning("El servidor ya está activo");
            return Task.FromResult(true);
        }

        return Task.Run(() =>
        {
            try
            {
                // Detectar IP local
                IpLocal = DetectarIPLocal();
                if (string.IsNullOrEmpty(IpLocal))
                {
                    Log.Error("No se pudo detectar la IP local");
                    return false;
                }

                Puerto = puerto;
                UrlBase = $"http://{IpLocal}:{Puerto}";

                // Configurar firewall automáticamente si es necesario
                if (!_firewallConfigurado)
                {
                    ConfigurarFirewall(puerto);
                    _firewallConfigurado = true;
                }

                // Crear listener
                _listener = new HttpListener();
                // Intentar escuchar en todas las interfaces primero
                try
                {
                    // Usar * en lugar de + para evitar problemas de permisos
                    _listener.Prefixes.Add($"http://*:{Puerto}/"); // Escuchar en todas las interfaces
                    Log.Information("Configurado para escuchar en todas las interfaces (puerto {Puerto})", Puerto);
                }
                catch (HttpListenerException ex)
                {
                    // Si falla (sin permisos de admin), intentar solo en localhost
                    Log.Warning("No se pueden usar todas las interfaces. Error: {Error}. Intentando solo localhost...", ex.Message);
                    _listener.Prefixes.Add($"http://localhost:{Puerto}/");
                    // Actualizar URL base para usar localhost
                    UrlBase = $"http://localhost:{Puerto}";
                    Log.Warning("Solo disponible en localhost. El móvil no podrá conectarse.");
                }

                // Iniciar servidor
                _listener.Start();
                Log.Information("═══════════════════════════════════════════════════════");
                Log.Information("Servidor HTTP local iniciado");
                Log.Information("IP: {IP}", IpLocal);
                Log.Information("Puerto: {Puerto}", Puerto);
                Log.Information("URL Base: {URL}", UrlBase);
                Log.Information("═══════════════════════════════════════════════════════");

                // Crear token de cancelación
                _cancellationTokenSource = new CancellationTokenSource();
                var cancellationToken = _cancellationTokenSource.Token;

                // Iniciar tarea de limpieza de tokens expirados
                _ = Task.Run(() => LimpiarTokensExpirados(cancellationToken));

                // Iniciar tarea del servidor
                _serverTask = Task.Run(() => ProcesarPeticiones(cancellationToken));

                return true;
            }
            catch (HttpListenerException ex)
            {
                Log.Error(ex, "Error al iniciar el servidor HTTP. ¿Tienes permisos de administrador?");
                return false;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error inesperado al iniciar el servidor HTTP");
                return false;
            }
        });
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
    public void RegistrarToken(string token, string textoConsentimiento, string? titulo = null)
    {
        RegistrarToken(token);
        _contextoPorToken[token] = new ContextoFirma
        {
            Texto = textoConsentimiento ?? string.Empty,
            Titulo = string.IsNullOrWhiteSpace(titulo) ? "Consentimiento" : titulo!,
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
                Log.Information("Firma/foto recibida para token: {Token}, tamaño base64: {Tamaño} caracteres", 
                    token, firma.ImagenBase64?.Length ?? 0);
                return firma.ImagenBase64;
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
                    Log.Information("Firma/foto recibida para token: {Token} (con retraso), tamaño base64: {Tamaño} caracteres", 
                        token, firmaRetrasada.ImagenBase64?.Length ?? 0);
                    return firmaRetrasada.ImagenBase64;
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
        if (!(_listener?.IsListening ?? false))
        {
            Log.Debug("El servidor no está activo");
            return;
        }

        try
        {
            // Cancelar primero para que el bucle de procesamiento se detenga
            _cancellationTokenSource?.Cancel();
            
            // Esperar un poco para que las peticiones en curso terminen
            Task.Delay(100).Wait();
            
            // Detener el listener
            _listener?.Stop();
            
            // Esperar a que termine el task del servidor si existe
            try
            {
                _serverTask?.Wait(TimeSpan.FromSeconds(2));
            }
            catch (Exception ex)
            {
                Log.Debug("Error al esperar que termine el task del servidor: {Error}", ex.Message);
            }
            
            // Cerrar el listener
            _listener?.Close();
            
            Log.Information("Servidor HTTP local detenido");
        }
        catch (ObjectDisposedException)
        {
            // Ya estaba cerrado, no es un error
            Log.Debug("El listener ya estaba cerrado");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al detener el servidor HTTP");
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
            if (_contextoPorToken.TryGetValue(token, out var ctx))
            {
                textoHtml = WebUtility.HtmlEncode(ctx.Texto);
                tituloHtml = WebUtility.HtmlEncode(ctx.Titulo);
            }
            else
            {
                textoHtml = string.Empty;
                tituloHtml = "Consentimiento";
            }
            html = html.Replace("{CONSENT_TEXT}", textoHtml);
            html = html.Replace("{CONSENT_TITLE}", tituloHtml);

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

            // Agregar headers CORS
            response.Headers.Add("Access-Control-Allow-Origin", "*");
            response.Headers.Add("Cache-Control", "public, max-age=3600");
            
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
            var body = await reader.ReadToEndAsync();

            // Parsear JSON (formato esperado: {"firma": "data:image/png;base64,..."})
            // Por ahora, asumimos que viene como base64 directo o en formato data URL
            string imagenBase64 = body.Trim();

            // Si viene como data URL, extraer solo el base64
            if (imagenBase64.StartsWith("data:image"))
            {
                var base64Index = imagenBase64.IndexOf(',');
                if (base64Index > 0)
                {
                    imagenBase64 = imagenBase64.Substring(base64Index + 1);
                }
            }

            // Guardar la firma
            var firma = new FirmaRecibida
            {
                Token = token,
                ImagenBase64 = imagenBase64,
                FechaRecepcion = DateTime.Now
            };

            _firmasRecibidas[token] = firma;

            // Eliminar el token de activos (ya se recibió la firma)
            _tokensActivos.TryRemove(token, out _);
            _contextoPorToken.TryRemove(token, out _);

            // Disparar evento
            FirmaRecibida?.Invoke(this, new FirmaRecibidaEventArgs
            {
                Token = token,
                ImagenBase64 = imagenBase64
            });

            Log.Information("Firma/foto recibida para token: {Token}, tamaño base64: {Tamaño} caracteres", 
                token, imagenBase64?.Length ?? 0);

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
    public string ImagenBase64 { get; set; } = string.Empty;
    public DateTime FechaRecepcion { get; set; }
}

/// <summary>
/// Contexto adicional asociado a una sesión de firma (texto del consentimiento, título).
/// </summary>
internal class ContextoFirma
{
    public string Texto { get; set; } = string.Empty;
    public string Titulo { get; set; } = "Consentimiento";
}

/// <summary>
/// Argumentos del evento cuando se recibe una firma.
/// </summary>
public class FirmaRecibidaEventArgs : EventArgs
{
    public string Token { get; set; } = string.Empty;
    public string ImagenBase64 { get; set; } = string.Empty;
}

