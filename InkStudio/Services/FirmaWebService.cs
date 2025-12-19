using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace InkStudio.Services;

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

    private const int PuertoPorDefecto = 8080;
    private const int TimeoutTokenMinutos = 10; // Los tokens expiran después de 10 minutos

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

                // Crear listener
                _listener = new HttpListener();
                _listener.Prefixes.Add($"http://+:{Puerto}/"); // Escuchar en todas las interfaces

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
            // Verificar si el token expiró
            if (!_tokensActivos.ContainsKey(token))
            {
                Log.Warning("Token expirado o inválido: {Token}", token);
                return null;
            }

            // Verificar si ya se recibió la firma
            if (_firmasRecibidas.TryGetValue(token, out var firma))
            {
                Log.Information("Firma recibida para token: {Token}", token);
                return firma.ImagenBase64;
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
            _cancellationTokenSource?.Cancel();
            _listener?.Stop();
            _listener?.Close();
            
            Log.Information("Servidor HTTP local detenido");
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

        while (!cancellationToken.IsCancellationRequested && _listener.IsListening)
        {
            try
            {
                // Esperar petición (con timeout)
                var context = await _listener.GetContextAsync();
                _ = Task.Run(() => ManejarPeticion(context, cancellationToken));
            }
            catch (HttpListenerException ex) when (ex.ErrorCode == 995)
            {
                // Error 995 = operación cancelada (normal al cerrar)
                Log.Debug("Servidor HTTP cerrado");
                break;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error al procesar petición HTTP");
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
            Log.Debug("Petición recibida: {Method} {Path}", request.HttpMethod, path);

            // GET /firma/{token} - Servir página HTML
            if (request.HttpMethod == "GET" && path.StartsWith("/firma/"))
            {
                var token = path.Substring("/firma/".Length);
                await ServirPaginaFirma(response, token);
            }
            // POST /firma/{token} - Recibir firma
            else if (request.HttpMethod == "POST" && path.StartsWith("/firma/"))
            {
                var token = path.Substring("/firma/".Length);
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
    private async Task ServirPaginaFirma(HttpListenerResponse response, string token)
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

            response.ContentType = "text/html; charset=utf-8";
            response.StatusCode = 200;
            
            var bytes = Encoding.UTF8.GetBytes(html);
            await response.OutputStream.WriteAsync(bytes);
            response.Close();

            Log.Debug("Página HTML servida para token: {Token}", token);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al servir página HTML");
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

            // Disparar evento
            FirmaRecibida?.Invoke(this, new FirmaRecibidaEventArgs
            {
                Token = token,
                ImagenBase64 = imagenBase64
            });

            Log.Information("Firma recibida para token: {Token}", token);

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
/// Argumentos del evento cuando se recibe una firma.
/// </summary>
public class FirmaRecibidaEventArgs : EventArgs
{
    public string Token { get; set; } = string.Empty;
    public string ImagenBase64 { get; set; } = string.Empty;
}

