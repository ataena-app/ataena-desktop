using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Ataena.Services;

/// <summary>
/// Servicio de comprobación e instalación de actualizaciones desde GitHub Releases.
/// </summary>
/// <remarks>
/// Flujo:
/// 1. Consulta la API de GitHub Releases y obtiene el último release.
/// 2. Compara la versión publicada con la del ensamblado.
/// 3. Si hay versión nueva, expone el asset "Ataena-Setup-X.Y.Z.exe" para descarga.
/// 4. Descarga el instalador a la carpeta temporal.
/// 5. Lo ejecuta con "/VERYSILENT /NORESTART /CLOSEAPPLICATIONS" y cierra la app.
///
/// No depende de Velopack. Requiere que los releases del repositorio incluyan
/// el instalador de Inno Setup como asset con nombre "Ataena-Setup-*.exe".
/// </remarks>
public static class ActualizacionService
{
    // Repositorio desde el que se publican las releases (coincide con el que aparece en Ataena.iss).
    private const string GitHubOwner = "Jvalfdev";
    private const string GitHubRepo = "desktop-myos-app";
    private const string UserAgent = "Ataena-CRM-Updater";

    private static readonly HttpClient _http = new()
    {
        Timeout = TimeSpan.FromSeconds(20),
    };

    static ActualizacionService()
    {
        if (!_http.DefaultRequestHeaders.UserAgent.Any())
        {
            _http.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
            _http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        }
    }

    /// <summary>
    /// Información devuelta por <see cref="ComprobarAsync"/>.
    /// </summary>
    public sealed class ResultadoComprobacion
    {
        public bool HayActualizacion { get; init; }
        public Version VersionActual { get; init; } = new(0, 0, 0);
        public Version? VersionDisponible { get; init; }
        public string? UrlDescarga { get; init; }
        public string? NombreArchivo { get; init; }
        public string? NotasRelease { get; init; }
        public string? UrlRelease { get; init; }
    }

    /// <summary>
    /// Versión actual según el ensamblado (AssemblyVersion o InformationalVersion).
    /// </summary>
    public static Version ObtenerVersionActual()
    {
        var asm = Assembly.GetExecutingAssembly();
        var info = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(info))
        {
            // Puede venir con sufijos tipo "1.0.0+commit". Nos quedamos con la parte numérica.
            var limpio = new string(info.TakeWhile(c => char.IsDigit(c) || c == '.').ToArray());
            if (Version.TryParse(limpio, out var vInfo))
                return vInfo;
        }
        return asm.GetName().Version ?? new Version(0, 0, 0);
    }

    /// <summary>
    /// Consulta GitHub Releases y determina si hay una versión más reciente.
    /// Nunca lanza excepciones: ante cualquier error devuelve <c>HayActualizacion = false</c>.
    /// </summary>
    public static async Task<ResultadoComprobacion> ComprobarAsync(CancellationToken ct = default)
    {
        var actual = ObtenerVersionActual();
        try
        {
            // Usamos /releases (no /releases/latest) para incluir también prereleases.
            var url = $"https://api.github.com/repos/{GitHubOwner}/{GitHubRepo}/releases?per_page=10";
            using var resp = await _http.GetAsync(url, ct).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
            {
                Serilog.Log.Warning("ActualizacionService: GitHub respondió {Code}", (int)resp.StatusCode);
                return new ResultadoComprobacion { VersionActual = actual };
            }

            var json = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            var releases = JsonSerializer.Deserialize<GitHubRelease[]>(json);

            // Tomamos el release no-draft más reciente con tag parseable (el primero del array)
            var release = releases?
                .Where(r => !r.Draft && !string.IsNullOrWhiteSpace(r.TagName))
                .FirstOrDefault();

            if (release is null || string.IsNullOrWhiteSpace(release.TagName))
                return new ResultadoComprobacion { VersionActual = actual };

            var tag = release.TagName.TrimStart('v', 'V').Trim();
            if (!Version.TryParse(tag, out var nueva))
            {
                Serilog.Log.Warning("ActualizacionService: no se pudo parsear tag {Tag}", release.TagName);
                return new ResultadoComprobacion { VersionActual = actual };
            }

            var asset = release.Assets?
                .FirstOrDefault(a => a.Name is not null
                                     && a.Name.StartsWith("Ataena-Setup", StringComparison.OrdinalIgnoreCase)
                                     && a.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase));

            var hay = nueva > actual && asset is not null;
            return new ResultadoComprobacion
            {
                HayActualizacion = hay,
                VersionActual = actual,
                VersionDisponible = nueva,
                UrlDescarga = asset?.BrowserDownloadUrl,
                NombreArchivo = asset?.Name,
                NotasRelease = release.Body,
                UrlRelease = release.HtmlUrl,
            };
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "ActualizacionService: error comprobando actualizaciones");
            return new ResultadoComprobacion { VersionActual = actual };
        }
    }

    /// <summary>
    /// Descarga el instalador a la carpeta temporal y devuelve su ruta local.
    /// </summary>
    public static async Task<string> DescargarAsync(
        ResultadoComprobacion info,
        IProgress<double>? progreso = null,
        CancellationToken ct = default)
    {
        if (info.UrlDescarga is null || info.NombreArchivo is null)
            throw new InvalidOperationException("No hay URL de descarga disponible.");

        var carpetaTemp = Path.Combine(Path.GetTempPath(), "Ataena", "updates");
        Directory.CreateDirectory(carpetaTemp);
        var destino = Path.Combine(carpetaTemp, info.NombreArchivo);

        using var resp = await _http.GetAsync(info.UrlDescarga, HttpCompletionOption.ResponseHeadersRead, ct)
            .ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();

        var total = resp.Content.Headers.ContentLength ?? -1L;
        await using var origen = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        await using var fs = new FileStream(destino, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true);

        var buffer = new byte[81920];
        long leido = 0;
        int n;
        while ((n = await origen.ReadAsync(buffer.AsMemory(0, buffer.Length), ct).ConfigureAwait(false)) > 0)
        {
            await fs.WriteAsync(buffer.AsMemory(0, n), ct).ConfigureAwait(false);
            leido += n;
            if (total > 0 && progreso is not null)
                progreso.Report(Math.Min(1.0, leido / (double)total));
        }

        Serilog.Log.Information("ActualizacionService: descargado {Archivo} ({Bytes} bytes)", destino, leido);
        return destino;
    }

    /// <summary>
    /// Lanza el instalador descargado en modo silencioso. NO cierra Ataena por nuestra cuenta:
    /// dejamos que Inno Setup la detecte vía Restart Manager (/CLOSEAPPLICATIONS) y la relance
    /// automáticamente al terminar (/RESTARTAPPLICATIONS). Si fuésemos nosotros los que matamos
    /// el proceso, Inno Setup no la registraría y al terminar el install no habría nada que
    /// reabrir, dando la sensación al usuario de que la app crasheó.
    /// </summary>
    /// <param name="rutaInstalador">Ruta al .exe descargado.</param>
    public static void EjecutarInstalador(string rutaInstalador)
    {
        if (!File.Exists(rutaInstalador))
            throw new FileNotFoundException("No se encuentra el instalador descargado.", rutaInstalador);

        // /VERYSILENT          → sin diálogos
        // /SUPPRESSMSGBOXES    → auto-OK a mensajes
        // /NORESTART           → no reiniciar Windows si no es imprescindible
        // /CLOSEAPPLICATIONS   → Inno detecta y cierra Ataena vía Restart Manager
        // /RESTARTAPPLICATIONS → la relanza al terminar la instalación
        var psi = new ProcessStartInfo
        {
            FileName = rutaInstalador,
            Arguments = "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /CLOSEAPPLICATIONS /RESTARTAPPLICATIONS",
            UseShellExecute = true, // Necesario para que Windows pida UAC al instalador
        };

        Process.Start(psi);
        Serilog.Log.Information(
            "ActualizacionService: instalador lanzado en silencioso. Inno Setup cerrará y relanzará Ataena.");
    }

    // ---------- DTOs para la API de GitHub ----------

    private sealed class GitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public string? TagName { get; set; }

        [JsonPropertyName("html_url")]
        public string? HtmlUrl { get; set; }

        [JsonPropertyName("body")]
        public string? Body { get; set; }

        [JsonPropertyName("prerelease")]
        public bool Prerelease { get; set; }

        [JsonPropertyName("draft")]
        public bool Draft { get; set; }

        [JsonPropertyName("assets")]
        public GitHubAsset[]? Assets { get; set; }
    }

    private sealed class GitHubAsset
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("browser_download_url")]
        public string? BrowserDownloadUrl { get; set; }

        [JsonPropertyName("size")]
        public long Size { get; set; }
    }
}
