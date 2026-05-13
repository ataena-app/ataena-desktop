using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Ataena.Data;
using Ataena.Models;
using Serilog;

namespace Ataena.Services;

/// <summary>
/// Servicio para gestionar días festivos.
/// Integra la API pública Nager.Date para festivos nacionales/autonómicos
/// y permite añadir festivos locales personalizados.
/// </summary>
public class FestivosService
{
    private static readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    private const string NAGER_API_URL = "https://date.nager.at/api/v3/PublicHolidays";
    private const string CODIGO_PAIS = "ES";
    private const string CODIGO_AUTONOMIA = "CM"; // Castilla-La Mancha

    /// <summary>
    /// Obtiene los festivos de un año desde la API y la base de datos.
    /// Combina festivos de la API con festivos locales personalizados.
    /// </summary>
    /// <param name="anio">Año a consultar.</param>
    /// <param name="forzarActualizacionApi">Si true, consulta la API aunque ya existan datos.</param>
    /// <returns>Lista de festivos para el año.</returns>
    public async Task<List<DiaFestivo>> ObtenerFestivosAnioAsync(int anio, bool forzarActualizacionApi = false)
    {
        using var db = new AtaenaDbContext();

        // Verificar si ya tenemos festivos de API para este año
        var festivosExistentes = await db.DiasFestivos
            .Where(f => f.Anio == anio && !f.EsPersonalizado)
            .CountAsync();

        // Si no hay festivos de API o se fuerza actualización, consultar API
        if (festivosExistentes == 0 || forzarActualizacionApi)
        {
            await SincronizarFestivosApiAsync(anio);
        }

        // Obtener todos los festivos del año (API + personalizados)
        var festivos = await db.DiasFestivos
            .Where(f => f.Anio == anio && f.Activo)
            .OrderBy(f => f.Fecha)
            .ToListAsync();

        return festivos;
    }

    /// <summary>
    /// Obtiene los festivos de un mes específico.
    /// </summary>
    public async Task<List<DiaFestivo>> ObtenerFestivosMesAsync(int anio, int mes)
    {
        var festivosAnio = await ObtenerFestivosAnioAsync(anio);
        return festivosAnio.Where(f => f.Fecha.Month == mes).ToList();
    }

    /// <summary>
    /// Verifica si una fecha específica es festivo.
    /// </summary>
    public async Task<DiaFestivo?> ObtenerFestivoPorFechaAsync(DateTime fecha)
    {
        using var db = new AtaenaDbContext();
        return await db.DiasFestivos
            .Where(f => f.Fecha.Date == fecha.Date && f.Activo)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Sincroniza los festivos desde la API Nager.Date.
    /// </summary>
    private async Task SincronizarFestivosApiAsync(int anio)
    {
        try
        {
            Log.Information("🔄 Sincronizando festivos de España para {Anio} desde Nager.Date API", anio);

            var url = $"{NAGER_API_URL}/{anio}/{CODIGO_PAIS}";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                Log.Warning("⚠️ API Nager.Date respondió con error: {StatusCode}", response.StatusCode);
                return;
            }

            var json = await response.Content.ReadAsStringAsync();
            var festivosApi = JsonSerializer.Deserialize<List<NagerHoliday>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (festivosApi == null || festivosApi.Count == 0)
            {
                Log.Warning("⚠️ No se obtuvieron festivos de la API para {Anio}", anio);
                return;
            }

            using var db = new AtaenaDbContext();

            // Eliminar festivos de API anteriores del mismo año (para actualizar)
            var festivosAnteriores = await db.DiasFestivos
                .Where(f => f.Anio == anio && !f.EsPersonalizado)
                .ToListAsync();
            
            if (festivosAnteriores.Any())
            {
                db.DiasFestivos.RemoveRange(festivosAnteriores);
            }

            // Filtrar festivos que aplican a nivel nacional o a Castilla-La Mancha
            var festivosFiltrados = festivosApi.Where(f => 
                f.Counties == null || // Nacional (sin subdivisiones)
                f.Counties.Length == 0 ||
                f.Counties.Any(c => c.Contains(CODIGO_AUTONOMIA, StringComparison.OrdinalIgnoreCase))
            ).ToList();

            foreach (var festivo in festivosFiltrados)
            {
                var nuevoFestivo = new DiaFestivo
                {
                    Fecha = festivo.Date,
                    Nombre = festivo.LocalName ?? festivo.Name ?? "Festivo",
                    NombreIngles = festivo.Name,
                    Tipo = DeterminarTipoFestivo(festivo),
                    Activo = true,
                    EsPersonalizado = false,
                    CodigoSubdivision = festivo.Counties?.FirstOrDefault(c => c.Contains(CODIGO_AUTONOMIA)),
                    Anio = anio,
                    EsFijo = festivo.Fixed,
                    ColorFondo = DeterminarColorFestivo(festivo)
                };

                db.DiasFestivos.Add(nuevoFestivo);
            }

            await db.SaveChangesAsync();
            Log.Information("✅ Sincronizados {Count} festivos para {Anio}", festivosFiltrados.Count, anio);
        }
        catch (HttpRequestException ex)
        {
            Log.Error(ex, "❌ Error de red al consultar API de festivos");
        }
        catch (TaskCanceledException ex)
        {
            Log.Warning(ex, "⏱️ Timeout al consultar API de festivos");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "❌ Error al sincronizar festivos desde API");
        }
    }

    /// <summary>
    /// Determina el tipo de festivo según los datos de la API.
    /// </summary>
    private static TipoFestivo DeterminarTipoFestivo(NagerHoliday festivo)
    {
        if (festivo.Counties == null || festivo.Counties.Length == 0)
            return TipoFestivo.Nacional;

        if (festivo.Counties.Any(c => c.Contains(CODIGO_AUTONOMIA)))
            return TipoFestivo.Autonomico;

        return TipoFestivo.Nacional;
    }

    /// <summary>
    /// Determina el color del festivo según su tipo.
    /// </summary>
    private static string DeterminarColorFestivo(NagerHoliday festivo)
    {
        var tipo = DeterminarTipoFestivo(festivo);
        return tipo switch
        {
            TipoFestivo.Nacional => "#dc2626",   // Rojo
            TipoFestivo.Autonomico => "#ea580c", // Naranja
            TipoFestivo.Local => "#7c3aed",      // Morado
            _ => "#dc2626"
        };
    }

    /// <summary>
    /// Añade un festivo local personalizado.
    /// </summary>
    public async Task<DiaFestivo> AgregarFestivoLocalAsync(DateTime fecha, string nombre, string? notas = null)
    {
        using var db = new AtaenaDbContext();

        var festivo = new DiaFestivo
        {
            Fecha = fecha,
            Nombre = nombre,
            Tipo = TipoFestivo.Local,
            Activo = true,
            EsPersonalizado = true,
            Anio = fecha.Year,
            EsFijo = true,
            ColorFondo = "#7c3aed", // Morado para locales
            Notas = notas
        };

        db.DiasFestivos.Add(festivo);
        await db.SaveChangesAsync();

        Log.Information("📅 Festivo local añadido: {Nombre} el {Fecha}", nombre, fecha.ToString("dd/MM/yyyy"));
        return festivo;
    }

    /// <summary>
    /// Inicializa los festivos locales de Guadalajara (si no existen).
    /// </summary>
    public async Task InicializarFestivosLocalesGuadalajaraAsync(int anio)
    {
        using var db = new AtaenaDbContext();

        // Verificar si ya existen festivos locales para este año
        var existenLocales = await db.DiasFestivos
            .AnyAsync(f => f.Anio == anio && f.EsPersonalizado && f.Tipo == TipoFestivo.Local);

        if (existenLocales)
        {
            Log.Debug("Festivos locales de Guadalajara ya inicializados para {Anio}", anio);
            return;
        }

        // Festivos locales de Guadalajara (fechas fijas aproximadas)
        var festivosLocales = new[]
        {
            (Fecha: new DateTime(anio, 1, 23), Nombre: "San Ildefonso (Guadalajara)", Notas: "Patrón de Guadalajara"),
            (Fecha: new DateTime(anio, 9, 14), Nombre: "Fiestas de Guadalajara", Notas: "Inicio de fiestas patronales"),
            (Fecha: new DateTime(anio, 10, 25), Nombre: "Día de Castilla-La Mancha", Notas: "Festivo autonómico")
        };

        foreach (var (fecha, nombre, notas) in festivosLocales)
        {
            var festivo = new DiaFestivo
            {
                Fecha = fecha,
                Nombre = nombre,
                Tipo = TipoFestivo.Local,
                Activo = true,
                EsPersonalizado = true,
                Anio = anio,
                EsFijo = true,
                ColorFondo = "#7c3aed",
                Notas = notas
            };
            db.DiasFestivos.Add(festivo);
        }

        await db.SaveChangesAsync();
        Log.Information("📅 Festivos locales de Guadalajara inicializados para {Anio}", anio);
    }

    /// <summary>
    /// Elimina un festivo personalizado.
    /// </summary>
    public async Task<bool> EliminarFestivoAsync(int festivoId)
    {
        using var db = new AtaenaDbContext();
        
        var festivo = await db.DiasFestivos.FindAsync(festivoId);
        if (festivo == null)
            return false;

        // Solo permitir eliminar festivos personalizados
        if (!festivo.EsPersonalizado)
        {
            Log.Warning("No se puede eliminar un festivo de API: {FestivoId}", festivoId);
            return false;
        }

        db.DiasFestivos.Remove(festivo);
        await db.SaveChangesAsync();
        
        Log.Information("🗑️ Festivo eliminado: {Nombre}", festivo.Nombre);
        return true;
    }

    /// <summary>
    /// Desactiva/oculta un festivo (sin eliminarlo).
    /// </summary>
    public async Task<bool> DesactivarFestivoAsync(int festivoId)
    {
        using var db = new AtaenaDbContext();
        
        var festivo = await db.DiasFestivos.FindAsync(festivoId);
        if (festivo == null)
            return false;

        festivo.Activo = false;
        await db.SaveChangesAsync();
        
        return true;
    }
}

/// <summary>
/// Modelo de respuesta de la API Nager.Date.
/// </summary>
internal class NagerHoliday
{
    public DateTime Date { get; set; }
    public string? LocalName { get; set; }
    public string? Name { get; set; }
    public string? CountryCode { get; set; }
    public bool Fixed { get; set; }
    public bool Global { get; set; }
    public string[]? Counties { get; set; }
    public int? LaunchYear { get; set; }

    [JsonPropertyName("types")]
    public string[]? Types { get; set; }
}
