using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Serilog;

namespace Ataena.Services;

/// <summary>
/// Carga el CHANGELOG embebido en la app y recuerda qué versión ya vio el usuario.
/// </summary>
public static class ChangelogService
{
    private static readonly Regex EncabezadoVersion = new(
        @"^##\s+\[(?<ver>[^\]]+)\](?:\s*-\s*(?<fecha>.+))?\s*$",
        RegexOptions.Multiline | RegexOptions.Compiled);

    private static string RutaEstado =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Ataena",
            "app-state.json");

    private static string RutaArchivoChangelog =>
        Path.Combine(AppContext.BaseDirectory, "Changelog", "CHANGELOG.md");

    /// <summary>
    /// Entrada de una versión en el changelog.
    /// </summary>
    public sealed class EntradaChangelog
    {
        public required Version Version { get; init; }
        public string VersionTexto { get; init; } = string.Empty;
        public string? Fecha { get; init; }
        public string ContenidoMarkdown { get; init; } = string.Empty;
    }

    private sealed class AppEstadoPersistido
    {
        public string? UltimaVersionChangelogVista { get; set; }
    }

    /// <summary>
    /// Versión del changelog que el usuario ya cerró (null si nunca).
    /// </summary>
    public static Version? ObtenerUltimaVersionVista()
    {
        var estado = CargarEstado();
        if (string.IsNullOrWhiteSpace(estado.UltimaVersionChangelogVista))
            return null;

        return Version.TryParse(estado.UltimaVersionChangelogVista, out var v) ? v : null;
    }

    /// <summary>
    /// True si la versión instalada es más nueva que la última vista en el changelog.
    /// </summary>
    public static bool DebeMostrarTrasActualizacion()
    {
        var actual = ActualizacionService.ObtenerVersionActual();
        var entradasNuevas = ObtenerEntradasNuevas(ObtenerUltimaVersionVista());
        if (entradasNuevas.Count == 0)
            return false;

        var ultimaVista = ObtenerUltimaVersionVista();
        if (ultimaVista is null)
            return true;

        return actual > ultimaVista;
    }

    /// <summary>
    /// Guarda la versión actual como ya vista (no volver a mostrar hasta otra actualización).
    /// </summary>
    public static void MarcarVersionActualComoVista()
    {
        try
        {
            var actual = ActualizacionService.ObtenerVersionActual();
            var estado = CargarEstado();
            estado.UltimaVersionChangelogVista = actual.ToString();
            GuardarEstado(estado);
            Log.Debug("Changelog marcado como visto para versión {Version}", actual);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "No se pudo guardar estado del changelog");
        }
    }

    /// <summary>
    /// Entradas con versión estrictamente mayor que <paramref name="desdeVersion"/> (o todas si es null).
    /// </summary>
    public static IReadOnlyList<EntradaChangelog> ObtenerEntradasNuevas(Version? desdeVersion)
    {
        var todas = CargarTodasLasEntradas();
        if (desdeVersion is null)
        {
            var actual = ActualizacionService.ObtenerVersionActual();
            var soloActual = todas.Where(e => e.Version == actual).ToList();
            return soloActual.Count > 0 ? soloActual : todas.Take(1).ToList();
        }

        return todas
            .Where(e => e.Version > desdeVersion)
            .OrderByDescending(e => e.Version)
            .ToList();
    }

    /// <summary>
    /// Todas las versiones documentadas, de la más reciente a la más antigua.
    /// </summary>
    public static IReadOnlyList<EntradaChangelog> CargarTodasLasEntradas()
    {
        try
        {
            if (!File.Exists(RutaArchivoChangelog))
            {
                Log.Warning("CHANGELOG no encontrado en {Ruta}", RutaArchivoChangelog);
                return Array.Empty<EntradaChangelog>();
            }

            var texto = File.ReadAllText(RutaArchivoChangelog, Encoding.UTF8);
            return Parsear(texto);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al cargar CHANGELOG");
            return Array.Empty<EntradaChangelog>();
        }
    }

    /// <summary>
    /// Texto listo para mostrar en el modal (varias versiones concatenadas).
    /// </summary>
    public static string CombinarEntradasParaUi(IEnumerable<EntradaChangelog> entradas)
    {
        var sb = new StringBuilder();
        var lista = entradas.OrderByDescending(e => e.Version).ToList();

        for (var i = 0; i < lista.Count; i++)
        {
            var e = lista[i];
            if (i > 0)
                sb.AppendLine().AppendLine(new string('─', 36)).AppendLine();

            var cabecera = $"Versión {e.VersionTexto}";
            if (!string.IsNullOrWhiteSpace(e.Fecha))
                cabecera += $"  ({e.Fecha.Trim()})";

            sb.AppendLine(cabecera);
            sb.AppendLine();
            sb.AppendLine(FormatearContenidoParaUi(e.ContenidoMarkdown));
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Convierte markdown ligero del changelog a texto legible en UI.
    /// </summary>
    public static string FormatearContenidoParaUi(string markdown)
    {
        var sb = new StringBuilder();
        foreach (var raw in markdown.Split('\n'))
        {
            var linea = raw.TrimEnd('\r');
            var t = linea.Trim();

            if (t.StartsWith("### ", StringComparison.Ordinal))
            {
                sb.AppendLine();
                sb.AppendLine(t[4..]);
                continue;
            }

            if (t.StartsWith("- ", StringComparison.Ordinal))
            {
                sb.AppendLine("  • " + t[2..]);
                continue;
            }

            if (string.IsNullOrWhiteSpace(t))
            {
                if (sb.Length > 0 && !sb.ToString().EndsWith(Environment.NewLine + Environment.NewLine, StringComparison.Ordinal))
                    sb.AppendLine();
                continue;
            }

            sb.AppendLine(t);
        }

        return sb.ToString().Trim();
    }

    private static List<EntradaChangelog> Parsear(string texto)
    {
        var resultado = new List<EntradaChangelog>();
        var matches = EncabezadoVersion.Matches(texto);
        if (matches.Count == 0)
            return resultado;

        for (var i = 0; i < matches.Count; i++)
        {
            var m = matches[i];
            var verTexto = m.Groups["ver"].Value.Trim();
            if (!Version.TryParse(verTexto, out var version))
                continue;

            var inicio = m.Index + m.Length;
            var fin = i + 1 < matches.Count ? matches[i + 1].Index : texto.Length;
            var cuerpo = texto[inicio..fin].Trim();

            resultado.Add(new EntradaChangelog
            {
                Version = version,
                VersionTexto = verTexto,
                Fecha = m.Groups["fecha"].Success ? m.Groups["fecha"].Value.Trim() : null,
                ContenidoMarkdown = cuerpo
            });
        }

        return resultado.OrderByDescending(e => e.Version).ToList();
    }

    private static AppEstadoPersistido CargarEstado()
    {
        try
        {
            if (!File.Exists(RutaEstado))
                return new AppEstadoPersistido();

            var json = File.ReadAllText(RutaEstado, Encoding.UTF8);
            return JsonSerializer.Deserialize<AppEstadoPersistido>(json) ?? new AppEstadoPersistido();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Estado de app corrupto o ilegible, se reinicia");
            return new AppEstadoPersistido();
        }
    }

    private static void GuardarEstado(AppEstadoPersistido estado)
    {
        var dir = Path.GetDirectoryName(RutaEstado);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(estado, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(RutaEstado, json, Encoding.UTF8);
    }
}
