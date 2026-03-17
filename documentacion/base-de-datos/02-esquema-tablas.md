# Esquema de Tablas

> **Versión:** 1.0  
> **Fecha:** Diciembre 2025  
> **Estado:** Planificación

---

## 📋 Índice

1. [Resumen de Tablas](#-resumen-de-tablas)
2. [Tabla: Cliente](#-tabla-cliente)
3. [Tabla: Cita](#-tabla-cita)
4. [Tabla: Trabajo](#-tabla-trabajo)
5. [Tabla: Consentimiento](#-tabla-consentimiento)
6. [Tabla: Configuracion](#-tabla-configuracion)
7. [Enumeraciones](#-enumeraciones)
8. [Código C# Completo](#-código-c-completo)

---

## 📊 Resumen de Tablas

| Tabla | Descripción | Registros esperados |
|-------|-------------|---------------------|
| **Cliente** | Datos de los clientes | Cientos a miles |
| **Cita** | Agenda de citas | Miles |
| **Trabajo** | Tatuajes y piercings realizados | Miles |
| **Consentimiento** | Documentos firmados | Miles |
| **Configuracion** | Ajustes del estudio | 1 registro |

### Diagrama de relaciones

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           DIAGRAMA DE RELACIONES                            │
└─────────────────────────────────────────────────────────────────────────────┘

                              ┌───────────────────┐
                              │      CLIENTE      │
                              ├───────────────────┤
                              │ Id (PK)           │
                              │ Nombre            │
                              │ Apellidos         │
                              │ Telefono          │
                              │ Email             │
                              │ Dni               │
                              │ FechaNacimiento   │
                              │ Alergias          │
                              │ Notas             │
                              │ EsVip             │
                              │ FechaRegistro     │
                              └─────────┬─────────┘
                                        │
              ┌─────────────────────────┼─────────────────────────┐
              │                         │                         │
              │ 1:N                     │ 1:N                     │ 1:N
              ▼                         ▼                         ▼
    ┌─────────────────┐       ┌─────────────────┐       ┌─────────────────┐
    │      CITA       │       │     TRABAJO     │       │  CONSENTIMIENTO │
    ├─────────────────┤       ├─────────────────┤       ├─────────────────┤
    │ Id (PK)         │       │ Id (PK)         │       │ Id (PK)         │
    │ ClienteId (FK)  │       │ ClienteId (FK)  │       │ ClienteId (FK)  │
    │ Fecha           │       │ CitaId (FK)?    │       │ TrabajoId (FK)? │
    │ HoraInicio      │       │ Tipo            │       │ Tipo            │
    │ DuracionMinutos │       │ Descripcion     │       │ FechaFirma      │
    │ TipoCita        │       │ ZonaCuerpo      │       │ RutaDocumento   │
    │ Descripcion     │       │ Estilo          │       │ Firmado         │
    │ Estado          │       │ Precio          │       │ Notas           │
    │ EmailEnviado    │       │ Fecha           │       └─────────────────┘
    │ Notas           │       │ FotosJson       │
    └─────────────────┘       │ Notas           │
              │               └─────────────────┘
              │                         │
              │           1:1           │
              └────────────────────────►│
                    (opcional)


    ┌─────────────────┐
    │  CONFIGURACION  │  (Tabla independiente)
    ├─────────────────┤
    │ Id (PK)         │
    │ NombreEstudio   │
    │ Direccion       │
    │ Telefono        │
    │ Email           │
    │ SmtpConfig      │
    │ TemaOscuro      │
    └─────────────────┘
```

---

## 👤 Tabla: Cliente

### Descripción
Almacena los datos personales de cada cliente del estudio.

### Campos

| Campo | Tipo C# | Tipo SQLite | Obligatorio | Descripción |
|-------|---------|-------------|-------------|-------------|
| `Id` | `int` | INTEGER | ✅ PK | Identificador único, autoincremental |
| `Nombre` | `string` | TEXT | ✅ | Nombre del cliente |
| `Apellidos` | `string` | TEXT | ✅ | Apellidos del cliente |
| `Telefono` | `string` | TEXT | ✅ | Teléfono principal (único) |
| `Email` | `string?` | TEXT | ❌ | Correo electrónico |
| `Dni` | `string?` | TEXT | ❌ | Documento Nacional de Identidad |
| `FechaNacimiento` | `DateTime?` | TEXT | ❌ | Fecha de nacimiento |
| `Alergias` | `string?` | TEXT | ❌ | Alergias conocidas (importante para tintas) |
| `Notas` | `string?` | TEXT | ❌ | Notas generales sobre el cliente |
| `EsVip` | `bool` | INTEGER | ✅ | Cliente VIP (default: false) |
| `Activo` | `bool` | INTEGER | ✅ | Cliente activo (default: true) |
| `FechaRegistro` | `DateTime` | TEXT | ✅ | Fecha de registro (default: now) |

### Índices

| Índice | Campos | Único |
|--------|--------|-------|
| `IX_Cliente_Telefono` | Telefono | ✅ |
| `IX_Cliente_Nombre` | Nombre, Apellidos | ❌ |

### Relaciones

| Relación | Tabla relacionada | Tipo | Descripción |
|----------|-------------------|------|-------------|
| Citas | Cita | 1:N | Un cliente puede tener muchas citas |
| Trabajos | Trabajo | 1:N | Un cliente puede tener muchos trabajos |
| Consentimientos | Consentimiento | 1:N | Un cliente tiene varios consentimientos |

### Código C#

```csharp
// Models/Cliente.cs
namespace Ataena.Models;

public class Cliente
{
    public int Id { get; set; }
    
    public string Nombre { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Dni { get; set; }
    public DateTime? FechaNacimiento { get; set; }
    public string? Alergias { get; set; }
    public string? Notas { get; set; }
    public bool EsVip { get; set; } = false;
    public bool Activo { get; set; } = true;
    public DateTime FechaRegistro { get; set; } = DateTime.Now;
    
    // Navegación
    public List<Cita> Citas { get; set; } = new();
    public List<Trabajo> Trabajos { get; set; } = new();
    public List<Consentimiento> Consentimientos { get; set; } = new();
    
    // Propiedades calculadas
    public string NombreCompleto => $"{Nombre} {Apellidos}";
    public int? Edad => FechaNacimiento.HasValue
        ? (int)((DateTime.Today - FechaNacimiento.Value).TotalDays / 365.25)
        : null;
    public string FechaNacimientoConEdad => FechaNacimiento.HasValue
        ? Edad.HasValue
            ? $"{FechaNacimiento.Value:dd/MM/yyyy} ({Edad} años)"
            : FechaNacimiento.Value.ToString("dd/MM/yyyy")
        : "No especificada";
}
```

---

## 📅 Tabla: Cita

### Descripción
Almacena las citas programadas en la agenda del estudio.

### Campos

| Campo | Tipo C# | Tipo SQLite | Obligatorio | Descripción |
|-------|---------|-------------|-------------|-------------|
| `Id` | `int` | INTEGER | ✅ PK | Identificador único |
| `ClienteId` | `int` | INTEGER | ✅ FK | ID del cliente |
| `Fecha` | `DateTime` | TEXT | ✅ | Fecha de la cita |
| `HoraInicio` | `TimeSpan` | TEXT | ✅ | Hora de inicio |
| `DuracionMinutos` | `int` | INTEGER | ✅ | Duración estimada en minutos |
| `TipoCita` | `TipoCita` | INTEGER | ✅ | Tipo: Tatuaje, Piercing, Consulta, Retoque |
| `Descripcion` | `string?` | TEXT | ❌ | Descripción del trabajo a realizar |
| `Estado` | `EstadoCita` | INTEGER | ✅ | Estado de la cita |
| `EmailEnviado` | `bool` | INTEGER | ✅ | Si se envió email de confirmación |
| `FechaEmailEnviado` | `DateTime?` | TEXT | ❌ | Cuándo se envió el email |
| `Notas` | `string?` | TEXT | ❌ | Notas internas |
| `FechaCreacion` | `DateTime` | TEXT | ✅ | Cuándo se creó la cita |

### Índices

| Índice | Campos | Único |
|--------|--------|-------|
| `IX_Cita_ClienteId` | ClienteId | ❌ |
| `IX_Cita_Fecha` | Fecha | ❌ |
| `IX_Cita_Estado` | Estado | ❌ |

### Código C#

```csharp
// Models/Cita.cs
namespace Ataena.Models;

public class Cita
{
    public int Id { get; set; }
    
    public int ClienteId { get; set; }
    public DateTime Fecha { get; set; }
    public TimeSpan HoraInicio { get; set; }
    public int DuracionMinutos { get; set; } = 60;
    public TipoCita TipoCita { get; set; } = TipoCita.Tatuaje;
    public string? Descripcion { get; set; }
    public EstadoCita Estado { get; set; } = EstadoCita.Pendiente;
    public bool EmailEnviado { get; set; } = false;
    public DateTime? FechaEmailEnviado { get; set; }
    public string? Notas { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.Now;
    
    // Navegación
    public Cliente Cliente { get; set; } = null!;
    public Trabajo? Trabajo { get; set; }
    
    // Propiedades calculadas
    public DateTime FechaHoraInicio => Fecha.Date + HoraInicio;
    public DateTime FechaHoraFin => FechaHoraInicio.AddMinutes(DuracionMinutos);
    public bool EsPasada => FechaHoraInicio < DateTime.Now;
    public bool EsHoy => Fecha.Date == DateTime.Today;
}
```

---

## 🎨 Tabla: Trabajo

### Descripción
Almacena los tatuajes y piercings realizados.

### Campos

| Campo | Tipo C# | Tipo SQLite | Obligatorio | Descripción |
|-------|---------|-------------|-------------|-------------|
| `Id` | `int` | INTEGER | ✅ PK | Identificador único |
| `ClienteId` | `int` | INTEGER | ✅ FK | ID del cliente |
| `CitaId` | `int?` | INTEGER | ❌ FK | ID de la cita (puede ser walk-in) |
| `Tipo` | `TipoTrabajo` | INTEGER | ✅ | Tatuaje o Piercing |
| `Descripcion` | `string` | TEXT | ✅ | Descripción del trabajo |
| `ZonaCuerpo` | `string` | TEXT | ✅ | Zona del cuerpo |
| `Estilo` | `string?` | TEXT | ❌ | Estilo del tatuaje (solo tatuajes) |
| `Tamano` | `string?` | TEXT | ❌ | Tamaño aproximado |
| `Colores` | `bool` | INTEGER | ✅ | Si tiene colores (solo tatuajes) |
| `Precio` | `decimal` | TEXT | ✅ | Precio cobrado |
| `Fecha` | `DateTime` | TEXT | ✅ | Fecha de realización |
| `DuracionMinutos` | `int` | INTEGER | ✅ | Duración real |
| `FotosJson` | `string?` | TEXT | ❌ | JSON con rutas de fotos |
| `Notas` | `string?` | TEXT | ❌ | Notas sobre el trabajo |
| `FechaCreacion` | `DateTime` | TEXT | ✅ | Fecha de registro |

### Índices

| Índice | Campos | Único |
|--------|--------|-------|
| `IX_Trabajo_ClienteId` | ClienteId | ❌ |
| `IX_Trabajo_Fecha` | Fecha | ❌ |
| `IX_Trabajo_Tipo` | Tipo | ❌ |

### Código C#

```csharp
// Models/Trabajo.cs
namespace Ataena.Models;

public class Trabajo
{
    public int Id { get; set; }
    
    public int ClienteId { get; set; }
    public int? CitaId { get; set; }
    public TipoTrabajo Tipo { get; set; } = TipoTrabajo.Tatuaje;
    public string Descripcion { get; set; } = string.Empty;
    public string ZonaCuerpo { get; set; } = string.Empty;
    public string? Estilo { get; set; }
    public string? Tamano { get; set; }
    public bool Colores { get; set; } = false;
    public decimal Precio { get; set; }
    public DateTime Fecha { get; set; } = DateTime.Now;
    public int DuracionMinutos { get; set; }
    public string? FotosJson { get; set; }
    public string? Notas { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.Now;
    
    // Navegación
    public Cliente Cliente { get; set; } = null!;
    public Cita? Cita { get; set; }
    public Consentimiento? Consentimiento { get; set; }
    
    // Propiedades calculadas
    public List<string> Fotos => string.IsNullOrEmpty(FotosJson) 
        ? new List<string>() 
        : System.Text.Json.JsonSerializer.Deserialize<List<string>>(FotosJson) ?? new();
}
```

---

## 📝 Tabla: Consentimiento

### Descripción
Almacena los consentimientos firmados por los clientes.

### Tipos de consentimiento

| Tipo | Código | Cuándo se firma | Obligatorio |
|------|--------|-----------------|-------------|
| **RGPD** | `RGPD` | Al registrar cliente | ✅ Sí |
| **Imágenes** | `Imagenes` | Al registrar cliente | ❌ Opcional |
| **Por trabajo** | `Trabajo` | Antes de cada trabajo | ✅ Sí |

### Campos

| Campo | Tipo C# | Tipo SQLite | Obligatorio | Descripción |
|-------|---------|-------------|-------------|-------------|
| `Id` | `int` | INTEGER | ✅ PK | Identificador único |
| `ClienteId` | `int` | INTEGER | ✅ FK | ID del cliente |
| `TrabajoId` | `int?` | INTEGER | ❌ FK | ID del trabajo (solo tipo Trabajo) |
| `Tipo` | `TipoConsentimiento` | INTEGER | ✅ | Tipo de consentimiento |
| `FechaFirma` | `DateTime` | TEXT | ✅ | Fecha y hora de firma |
| `RutaDocumento` | `string?` | TEXT | ❌ | Ruta al PDF firmado |
| `Firmado` | `bool` | INTEGER | ✅ | Si está firmado |
| `Notas` | `string?` | TEXT | ❌ | Notas adicionales |

### Índices

| Índice | Campos | Único |
|--------|--------|-------|
| `IX_Consentimiento_ClienteId` | ClienteId | ❌ |
| `IX_Consentimiento_Tipo` | Tipo | ❌ |
| `IX_Consentimiento_TrabajoId` | TrabajoId | ❌ |

### Reglas de negocio

```
┌─────────────────────────────────────────────────────────────────┐
│ REGLAS DE CONSENTIMIENTOS                                       │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│ • RGPD: 1 por cliente, obligatorio para registrar               │
│ • Imágenes: 1 por cliente, opcional                             │
│ • Trabajo: 1 por cada trabajo, obligatorio                      │
│                                                                 │
│ • TrabajoId es NULL si Tipo = RGPD o Imagenes                   │
│ • TrabajoId es obligatorio si Tipo = Trabajo                    │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### Código C#

```csharp
// Models/Consentimiento.cs
namespace Ataena.Models;

public class Consentimiento
{
    public int Id { get; set; }
    
    public int ClienteId { get; set; }
    public int? TrabajoId { get; set; }
    public TipoConsentimiento Tipo { get; set; }
    public DateTime FechaFirma { get; set; } = DateTime.Now;
    public string? RutaDocumento { get; set; }
    public bool Firmado { get; set; } = false;
    public string? Notas { get; set; }
    
    // Navegación
    public Cliente Cliente { get; set; } = null!;
    public Trabajo? Trabajo { get; set; }
    
    // Propiedades calculadas
    public bool TieneDocumento => !string.IsNullOrEmpty(RutaDocumento);
}
```

---

## ⚙️ Tabla: Configuracion

### Descripción
Almacena la configuración del estudio. Solo existe 1 registro.

### Campos

| Campo | Tipo C# | Tipo SQLite | Obligatorio | Descripción |
|-------|---------|-------------|-------------|-------------|
| `Id` | `int` | INTEGER | ✅ PK | Siempre = 1 |
| `NombreEstudio` | `string` | TEXT | ✅ | Nombre del estudio |
| `Direccion` | `string?` | TEXT | ❌ | Dirección física |
| `Telefono` | `string?` | TEXT | ❌ | Teléfono del estudio |
| `Email` | `string?` | TEXT | ❌ | Email del estudio |
| `LogoPath` | `string?` | TEXT | ❌ | Ruta al logo |
| `SmtpServidor` | `string?` | TEXT | ❌ | Servidor SMTP |
| `SmtpPuerto` | `int` | INTEGER | ✅ | Puerto SMTP (default: 587) |
| `SmtpUsuario` | `string?` | TEXT | ❌ | Usuario SMTP |
| `SmtpPassword` | `string?` | TEXT | ❌ | Password SMTP (cifrado) |
| `SmtpUsarSsl` | `bool` | INTEGER | ✅ | Usar SSL |
| `TemaOscuro` | `bool` | INTEGER | ✅ | Tema oscuro activo |
| `IdiomaApp` | `string` | TEXT | ✅ | Idioma (default: "es") |

### Código C#

```csharp
// Models/Configuracion.cs
namespace Ataena.Models;

public class Configuracion
{
    public int Id { get; set; } = 1;
    
    // Datos del estudio
    public string NombreEstudio { get; set; } = "Mi Estudio";
    public string? Direccion { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public string? LogoPath { get; set; }
    
    // Configuración SMTP
    public string? SmtpServidor { get; set; }
    public int SmtpPuerto { get; set; } = 587;
    public string? SmtpUsuario { get; set; }
    public string? SmtpPassword { get; set; }
    public bool SmtpUsarSsl { get; set; } = true;
    
    // Preferencias
    public bool TemaOscuro { get; set; } = true;
    public string IdiomaApp { get; set; } = "es";
    
    // Propiedades calculadas
    public bool SmtpConfigurado => !string.IsNullOrEmpty(SmtpServidor) && 
                                   !string.IsNullOrEmpty(SmtpUsuario);
}
```

---

## 🔢 Enumeraciones

### Código C#

```csharp
// Models/Enums.cs
namespace Ataena.Models;

/// <summary>
/// Tipo de cita
/// </summary>
public enum TipoCita
{
    Tatuaje = 0,
    Piercing = 1,
    Consulta = 2,
    Retoque = 3
}

/// <summary>
/// Estado de una cita
/// </summary>
public enum EstadoCita
{
    Pendiente = 0,
    Confirmada = 1,
    EnProceso = 2,
    Completada = 3,
    Cancelada = 4,
    NoShow = 5
}

/// <summary>
/// Tipo de trabajo realizado
/// </summary>
public enum TipoTrabajo
{
    Tatuaje = 0,
    Piercing = 1
}

/// <summary>
/// Tipo de consentimiento
/// </summary>
public enum TipoConsentimiento
{
    RGPD = 0,
    Imagenes = 1,
    Trabajo = 2
}
```

---

## 💾 Código C# Completo

### DbContext

```csharp
// Data/AtaenaDbContext.cs
using Microsoft.EntityFrameworkCore;
using Ataena.Models;

namespace Ataena.Data;

public class AtaenaDbContext : DbContext
{
    // ══════════════════════════════════════════════════════════════
    // TABLAS
    // ══════════════════════════════════════════════════════════════
    
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Cita> Citas => Set<Cita>();
    public DbSet<Trabajo> Trabajos => Set<Trabajo>();
    public DbSet<Consentimiento> Consentimientos => Set<Consentimiento>();
    public DbSet<Configuracion> Configuracion => Set<Configuracion>();

    // ══════════════════════════════════════════════════════════════
    // CONFIGURACIÓN DE CONEXIÓN
    // ══════════════════════════════════════════════════════════════
    
    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        var appData = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData);
        var folder = Path.Combine(appData, "Ataena");
        var dbPath = Path.Combine(folder, "data.db");
        
        Directory.CreateDirectory(folder);
        options.UseSqlite($"Data Source={dbPath}");
    }

    // ══════════════════════════════════════════════════════════════
    // CONFIGURACIÓN DE MODELOS
    // ══════════════════════════════════════════════════════════════
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Cliente
        modelBuilder.Entity<Cliente>(entity =>
        {
            entity.HasIndex(e => e.Telefono).IsUnique();
            entity.HasIndex(e => new { e.Nombre, e.Apellidos });
        });

        // Cita
        modelBuilder.Entity<Cita>(entity =>
        {
            entity.HasIndex(e => e.Fecha);
            entity.HasIndex(e => e.Estado);
            
            entity.HasOne(e => e.Cliente)
                  .WithMany(c => c.Citas)
                  .HasForeignKey(e => e.ClienteId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Trabajo
        modelBuilder.Entity<Trabajo>(entity =>
        {
            entity.HasIndex(e => e.Fecha);
            
            entity.HasOne(e => e.Cliente)
                  .WithMany(c => c.Trabajos)
                  .HasForeignKey(e => e.ClienteId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            entity.HasOne(e => e.Cita)
                  .WithOne(c => c.Trabajo)
                  .HasForeignKey<Trabajo>(e => e.CitaId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Consentimiento
        modelBuilder.Entity<Consentimiento>(entity =>
        {
            entity.HasIndex(e => e.Tipo);
            
            entity.HasOne(e => e.Cliente)
                  .WithMany(c => c.Consentimientos)
                  .HasForeignKey(e => e.ClienteId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            entity.HasOne(e => e.Trabajo)
                  .WithOne(t => t.Consentimiento)
                  .HasForeignKey<Consentimiento>(e => e.TrabajoId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Configuración - Seed inicial
        modelBuilder.Entity<Configuracion>().HasData(new Configuracion
        {
            Id = 1,
            NombreEstudio = "Ataena",
            TemaOscuro = true,
            IdiomaApp = "es"
        });
    }
}
```

---

## 📋 Comandos para crear las tablas

```powershell
# 1. Navegar al proyecto
cd Ataena

# 2. Crear la migración inicial
dotnet ef migrations add Inicial

# 3. Aplicar la migración (crea la BD)
dotnet ef database update
```

---

## 📚 Documentos Relacionados

- `01-general.md` - Visión general de la base de datos
- `03-backup-exportar.md` - Operaciones de backup (próximo documento)
- `../guias-desarrollo/04-base-de-datos.md` - Guía de uso de EF Core

---

> **Nota:** Este esquema está diseñado para crecer. Las tablas de Artista, Producto y Factura se pueden añadir en el futuro sin afectar las existentes.

