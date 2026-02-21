using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using InkStudio.Models;

namespace InkStudio.Data;

/// <summary>
/// Contexto de base de datos para InkStudio CRM.
/// Usa SQLite como motor de base de datos.
/// </summary>
/// <remarks>
/// La base de datos se almacena en:
/// %LOCALAPPDATA%\InkStudio\data.db
/// </remarks>
public class InkStudioDbContext : DbContext
{
    #region DbSets (Tablas)

    /// <summary>
    /// Tabla de clientes del estudio.
    /// </summary>
    public DbSet<Cliente> Clientes => Set<Cliente>();

    /// <summary>
    /// Tabla de citas agendadas.
    /// </summary>
    public DbSet<Cita> Citas => Set<Cita>();

    /// <summary>
    /// Tabla de trabajos realizados.
    /// </summary>
    public DbSet<Trabajo> Trabajos => Set<Trabajo>();

    /// <summary>
    /// Tabla de consentimientos firmados.
    /// </summary>
    public DbSet<Consentimiento> Consentimientos => Set<Consentimiento>();

    /// <summary>
    /// Tabla de configuración (singleton, solo 1 registro).
    /// </summary>
    public DbSet<Configuracion> Configuracion => Set<Configuracion>();

    /// <summary>
    /// Tabla de días festivos (nacionales, autonómicos y locales).
    /// </summary>
    public DbSet<DiaFestivo> DiasFestivos => Set<DiaFestivo>();

    #endregion

    #region Configuración de Conexión

    /// <summary>
    /// Configura la conexión a la base de datos SQLite.
    /// </summary>
    /// <param name="options">Builder de opciones de configuración.</param>
    /// <remarks>
    /// Crea automáticamente la carpeta si no existe.
    /// Ruta: %LOCALAPPDATA%\InkStudio\data.db
    /// </remarks>
    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        var appData = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData);
        var folder = Path.Combine(appData, "InkStudio");
        var dbPath = Path.Combine(folder, "data.db");

        // Crear carpeta si no existe
        Directory.CreateDirectory(folder);

        options.UseSqlite($"Data Source={dbPath}");
    }

    #endregion

    #region Configuración de Modelos

    /// <summary>
    /// Configura las relaciones y restricciones del modelo de datos.
    /// </summary>
    /// <param name="modelBuilder">Builder para configurar el modelo.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigurarCliente(modelBuilder);
        ConfigurarCita(modelBuilder);
        ConfigurarTrabajo(modelBuilder);
        ConfigurarConsentimiento(modelBuilder);
        ConfigurarDiaFestivo(modelBuilder);
        ConfigurarDatosIniciales(modelBuilder);
    }

    /// <summary>
    /// Configura la entidad Cliente.
    /// </summary>
    /// <param name="modelBuilder">Builder del modelo.</param>
    private static void ConfigurarCliente(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cliente>(entity =>
        {
            // DNI único (obligatorio)
            entity.HasIndex(e => e.Dni).IsUnique()
                .HasFilter("[Dni] IS NOT NULL"); // Solo aplicar unicidad si DNI no es null

            // Índice para búsquedas por nombre
            entity.HasIndex(e => new { e.Nombre, e.Apellidos });
            
            // Teléfono NO es único (puede haber múltiples clientes con el mismo teléfono)
        });
    }

    /// <summary>
    /// Configura la entidad Cita y sus relaciones.
    /// </summary>
    /// <param name="modelBuilder">Builder del modelo.</param>
    private static void ConfigurarCita(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cita>(entity =>
        {
            // Índices para consultas frecuentes
            entity.HasIndex(e => e.Fecha);
            entity.HasIndex(e => e.Estado);

            // Relación: Cliente -> Citas (1:N)
            entity.HasOne(e => e.Cliente)
                  .WithMany(c => c.Citas)
                  .HasForeignKey(e => e.ClienteId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Relación: Trabajo -> Citas (1:N opcional)
            entity.HasOne(e => e.Trabajo)
                  .WithMany(t => t.Citas)
                  .HasForeignKey(e => e.TrabajoId)
                  .OnDelete(DeleteBehavior.SetNull);
        });
    }

    /// <summary>
    /// Configura la entidad Trabajo y sus relaciones.
    /// </summary>
    /// <param name="modelBuilder">Builder del modelo.</param>
    private static void ConfigurarTrabajo(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Trabajo>(entity =>
        {
            // Índice por fecha
            entity.HasIndex(e => e.Fecha);

            // Relación: Cliente -> Trabajos (1:N)
            entity.HasOne(e => e.Cliente)
                  .WithMany(c => c.Trabajos)
                  .HasForeignKey(e => e.ClienteId)
                  .OnDelete(DeleteBehavior.Cascade);

            // La relación Trabajo -> Citas (1:N) se configura desde Cita (ver ConfigurarCita)
        });
    }

    /// <summary>
    /// Configura la entidad Consentimiento y sus relaciones.
    /// </summary>
    /// <param name="modelBuilder">Builder del modelo.</param>
    private static void ConfigurarConsentimiento(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Consentimiento>(entity =>
        {
            // Índice por tipo
            entity.HasIndex(e => e.Tipo);

            // Relación: Cliente -> Consentimientos (1:N)
            entity.HasOne(e => e.Cliente)
                  .WithMany(c => c.Consentimientos)
                  .HasForeignKey(e => e.ClienteId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Relación: Trabajo -> Consentimiento (1:1 opcional)
            entity.HasOne(e => e.Trabajo)
                  .WithOne(t => t.Consentimiento)
                  .HasForeignKey<Consentimiento>(e => e.TrabajoId)
                  .OnDelete(DeleteBehavior.SetNull);
        });
    }

    /// <summary>
    /// Configura la entidad DiaFestivo.
    /// </summary>
    /// <param name="modelBuilder">Builder del modelo.</param>
    private static void ConfigurarDiaFestivo(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DiaFestivo>(entity =>
        {
            // Índice compuesto para búsquedas por año y mes
            entity.HasIndex(e => new { e.Anio, e.Fecha });
            
            // Índice único para evitar duplicados (misma fecha + nombre)
            entity.HasIndex(e => new { e.Fecha, e.Nombre }).IsUnique();
        });
    }

    /// <summary>
    /// Configura los datos iniciales (seed) de la base de datos.
    /// </summary>
    /// <param name="modelBuilder">Builder del modelo.</param>
    private static void ConfigurarDatosIniciales(ModelBuilder modelBuilder)
    {
        // Configuración inicial del estudio (instalación para Estudio Erzulie)
        modelBuilder.Entity<Configuracion>().HasData(new Configuracion
        {
            Id = 1,
            NombreEstudio = "Estudio Erzulie",
            Direccion = "Calle Núñez de Reinoso, Guadalajara",
            // El CIF se incluirá en los documentos legales usando esta dirección o texto adicional
            TemaOscuro = true,
            IdiomaApp = "es"
        });
    }

    #endregion
}
