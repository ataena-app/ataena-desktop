# Guía de Base de Datos

> **Nivel:** Principiante a Intermedio  
> **Objetivo:** Dominar Entity Framework Core para gestionar datos

---

## 📋 Índice

1. [Conceptos Básicos](#-conceptos-básicos)
2. [Crear Entidades](#-crear-entidades)
3. [Configurar DbContext](#-configurar-dbcontext)
4. [Migraciones](#-migraciones)
5. [Operaciones CRUD](#-operaciones-crud)
6. [Consultas con LINQ](#-consultas-con-linq)
7. [Relaciones entre Tablas](#-relaciones-entre-tablas)
8. [Errores Comunes](#-errores-comunes)

---

## 📚 Conceptos Básicos

### Vocabulario

| Término | Significado | Ejemplo |
|---------|-------------|---------|
| **Entidad** | Clase que representa una tabla | `Cliente`, `Cita` |
| **DbContext** | Conexión a la base de datos | `AtaenaDbContext` |
| **DbSet** | Representa una tabla | `DbSet<Cliente>` |
| **Migración** | Script que modifica la BD | Añadir columna `Email` |
| **LINQ** | Lenguaje de consultas en C# | `.Where()`, `.Select()` |
| **ORM** | Mapea objetos a tablas | Entity Framework |

### Flujo de datos

```
   C# (tu código)              Base de datos
┌─────────────────┐          ┌─────────────────┐
│                 │          │                 │
│  var cliente =  │          │  INSERT INTO    │
│  new Cliente(); │  ──────► │  Clientes       │
│  _db.Add(...)   │          │  VALUES (...)   │
│                 │          │                 │
│  var lista =    │          │  SELECT * FROM  │
│  _db.Clientes   │  ◄────── │  Clientes       │
│    .ToList();   │          │                 │
│                 │          │                 │
└─────────────────┘          └─────────────────┘
     EF Core traduce automáticamente
```

---

## 🏗️ Crear Entidades

### Estructura básica de una entidad

```csharp
// Models/Cliente.cs
namespace Ataena.Models;

public class Cliente
{
    // ══════════════════════════════════════════════
    // CLAVE PRIMARIA (obligatoria)
    // ══════════════════════════════════════════════
    public int Id { get; set; }  // EF detecta "Id" automáticamente
    
    // ══════════════════════════════════════════════
    // CAMPOS OBLIGATORIOS
    // ══════════════════════════════════════════════
    public string Nombre { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    
    // ══════════════════════════════════════════════
    // CAMPOS OPCIONALES (pueden ser null)
    // ══════════════════════════════════════════════
    public string? Email { get; set; }
    public string? Notas { get; set; }
    public DateTime? FechaNacimiento { get; set; }
    
    // ══════════════════════════════════════════════
    // CAMPOS CON VALOR POR DEFECTO
    // ══════════════════════════════════════════════
    public DateTime FechaRegistro { get; set; } = DateTime.Now;
    public bool Activo { get; set; } = true;
    
    // ══════════════════════════════════════════════
    // RELACIONES (Foreign Keys)
    // ══════════════════════════════════════════════
    public List<Cita> Citas { get; set; } = new();
    
    // ══════════════════════════════════════════════
    // PROPIEDADES CALCULADAS (NO se guardan en BD)
    // ══════════════════════════════════════════════
    public string NombreCompleto => $"{Nombre} {Apellidos}";
}
```

### Tipos de datos C# → SQLite

| C# | SQLite | Uso |
|----|--------|-----|
| `int` | INTEGER | IDs, cantidades |
| `string` | TEXT | Textos |
| `bool` | INTEGER (0/1) | Sí/No |
| `DateTime` | TEXT | Fechas |
| `decimal` | TEXT | Dinero |
| `double` | REAL | Decimales |
| `byte[]` | BLOB | Archivos |

### Convenciones de nombres

```csharp
// ✅ CORRECTO
public class Cliente           // Singular, PascalCase
{
    public int Id { get; set; }            // "Id" = clave primaria automática
    public int ClienteId { get; set; }     // "TablaId" = también funciona
}

// ❌ INCORRECTO
public class clientes          // No usar minúsculas ni plural
{
    public int id { get; set; }            // No usar minúsculas
}
```

---

## 🔌 Configurar DbContext

### Archivo básico

```csharp
// Data/AtaenaDbContext.cs
using Microsoft.EntityFrameworkCore;
using Ataena.Models;

namespace Ataena.Data;

public class AtaenaDbContext : DbContext
{
    // ══════════════════════════════════════════════
    // TABLAS (una línea por cada entidad)
    // ══════════════════════════════════════════════
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Cita> Citas => Set<Cita>();
    public DbSet<Trabajo> Trabajos => Set<Trabajo>();
    public DbSet<Consentimiento> Consentimientos => Set<Consentimiento>();

    // ══════════════════════════════════════════════
    // CONFIGURACIÓN DE CONEXIÓN
    // ══════════════════════════════════════════════
    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        // Ruta donde se guarda el archivo .db
        var appData = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData);
        var folder = Path.Combine(appData, "Ataena");
        var dbPath = Path.Combine(folder, "data.db");
        
        // Crear carpeta si no existe
        Directory.CreateDirectory(folder);
        
        // Configurar SQLite
        options.UseSqlite($"Data Source={dbPath}");
    }

    // ══════════════════════════════════════════════
    // CONFIGURACIÓN AVANZADA (opcional)
    // ══════════════════════════════════════════════
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configurar índices, relaciones, etc.
        modelBuilder.Entity<Cliente>()
            .HasIndex(c => c.Telefono)
            .IsUnique();
    }
}
```

### Añadir nueva tabla

```csharp
// 1. Crear la entidad en Models/
public class Producto { ... }

// 2. Añadir DbSet en AtaenaDbContext
public DbSet<Producto> Productos => Set<Producto>();

// 3. Crear migración
// dotnet ef migrations add AgregarProductos
// dotnet ef database update
```

---

## 🔄 Migraciones

### ¿Qué son?

Las migraciones son "versiones" de tu base de datos. Cada vez que cambias un Model, creas una migración que actualiza la BD sin perder datos.

### Comandos principales

```powershell
# Navegar al proyecto
cd Ataena

# Crear migración
dotnet ef migrations add NombreDescriptivo

# Aplicar migraciones pendientes
dotnet ef database update

# Ver lista de migraciones
dotnet ef migrations list

# Eliminar última migración (si NO se ha aplicado)
dotnet ef migrations remove

# Generar script SQL (para ver qué hace)
dotnet ef migrations script
```

### Flujo de trabajo

```
1. Modificas un Model (ej: añades campo Email)
              │
              ▼
2. Ejecutas: dotnet ef migrations add AgregarEmail
              │
              ▼
3. EF genera archivo en Migrations/
              │
              ▼
4. Ejecutas: dotnet ef database update
              │
              ▼
5. La BD se actualiza sin perder datos ✅
```

### Ejemplos de migraciones

```powershell
# Después de crear las entidades iniciales
dotnet ef migrations add Inicial

# Después de añadir campo Email a Cliente
dotnet ef migrations add AgregarEmailCliente

# Después de crear tabla Producto
dotnet ef migrations add CrearTablaProductos

# Después de añadir relación Cliente-Cita
dotnet ef migrations add RelacionClienteCita
```

### Aplicar migraciones automáticamente

En `App.axaml.cs`:

```csharp
public override void OnFrameworkInitializationCompleted()
{
    // Aplicar migraciones al iniciar la app
    using var db = new AtaenaDbContext();
    db.Database.Migrate();
    
    // Resto del código...
}
```

---

## ✏️ Operaciones CRUD

### CREATE (Crear)

```csharp
// Crear un cliente nuevo
var cliente = new Cliente
{
    Nombre = "María",
    Apellidos = "García",
    Telefono = "612345678",
    Email = "maria@email.com"
};

_db.Clientes.Add(cliente);
await _db.SaveChangesAsync();

// Después de guardar, cliente.Id tiene el ID asignado
Console.WriteLine($"Cliente creado con ID: {cliente.Id}");
```

### READ (Leer)

```csharp
// Obtener todos
var todos = await _db.Clientes.ToListAsync();

// Obtener uno por ID
var cliente = await _db.Clientes.FindAsync(5);

// Obtener uno con condición
var maria = await _db.Clientes
    .FirstOrDefaultAsync(c => c.Nombre == "María");

// Obtener varios con condición
var vips = await _db.Clientes
    .Where(c => c.EsVip)
    .ToListAsync();
```

### UPDATE (Actualizar)

```csharp
// Opción 1: Buscar y modificar
var cliente = await _db.Clientes.FindAsync(5);
if (cliente != null)
{
    cliente.Telefono = "698765432";
    cliente.EsVip = true;
    await _db.SaveChangesAsync();
}

// Opción 2: Si ya tienes el objeto
clienteExistente.Email = "nuevo@email.com";
await _db.SaveChangesAsync();
```

### DELETE (Eliminar)

```csharp
// Opción 1: Buscar y eliminar
var cliente = await _db.Clientes.FindAsync(5);
if (cliente != null)
{
    _db.Clientes.Remove(cliente);
    await _db.SaveChangesAsync();
}

// Opción 2: Si ya tienes el objeto
_db.Clientes.Remove(clienteExistente);
await _db.SaveChangesAsync();
```

---

## 🔍 Consultas con LINQ

### Filtrar (.Where)

```csharp
// Clientes VIP
var vips = await _db.Clientes
    .Where(c => c.EsVip)
    .ToListAsync();

// Clientes con email
var conEmail = await _db.Clientes
    .Where(c => c.Email != null)
    .ToListAsync();

// Múltiples condiciones
var filtrado = await _db.Clientes
    .Where(c => c.EsVip && c.Activo)
    .ToListAsync();
```

### Ordenar (.OrderBy)

```csharp
// Ordenar por nombre (A-Z)
var ordenado = await _db.Clientes
    .OrderBy(c => c.Nombre)
    .ToListAsync();

// Ordenar por fecha (más reciente primero)
var recientes = await _db.Clientes
    .OrderByDescending(c => c.FechaRegistro)
    .ToListAsync();
```

### Buscar texto (.Contains)

```csharp
var busqueda = "mar";

var resultados = await _db.Clientes
    .Where(c => c.Nombre.ToLower().Contains(busqueda.ToLower()) ||
                c.Apellidos.ToLower().Contains(busqueda.ToLower()) ||
                c.Telefono.Contains(busqueda))
    .ToListAsync();
```

### Limitar resultados (.Take, .Skip)

```csharp
// Primeros 10
var primeros = await _db.Clientes
    .Take(10)
    .ToListAsync();

// Paginación (página 2, 10 por página)
var pagina2 = await _db.Clientes
    .Skip(10)
    .Take(10)
    .ToListAsync();
```

### Contar (.Count)

```csharp
// Total de clientes
var total = await _db.Clientes.CountAsync();

// Total de VIPs
var totalVips = await _db.Clientes
    .CountAsync(c => c.EsVip);
```

### Existe (.Any)

```csharp
// ¿Existe algún cliente con este teléfono?
var existe = await _db.Clientes
    .AnyAsync(c => c.Telefono == "612345678");
```

### Seleccionar campos (.Select)

```csharp
// Solo nombres (no toda la entidad)
var nombres = await _db.Clientes
    .Select(c => c.NombreCompleto)
    .ToListAsync();

// Proyección a objeto anónimo
var resumenes = await _db.Clientes
    .Select(c => new { c.Id, c.NombreCompleto, c.Telefono })
    .ToListAsync();
```

---

## 🔗 Relaciones entre Tablas

### Uno a Muchos (1:N)

Un cliente tiene muchas citas:

```csharp
// Models/Cliente.cs
public class Cliente
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    
    // Un cliente tiene MUCHAS citas
    public List<Cita> Citas { get; set; } = new();
}

// Models/Cita.cs
public class Cita
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; }
    
    // Una cita pertenece a UN cliente
    public int ClienteId { get; set; }      // Foreign Key
    public Cliente Cliente { get; set; }     // Navegación
}
```

### Cargar relaciones (.Include)

```csharp
// SIN Include: cliente.Citas está vacío
var cliente = await _db.Clientes.FindAsync(5);
// cliente.Citas = [] (vacío)

// CON Include: cliente.Citas tiene datos
var clienteConCitas = await _db.Clientes
    .Include(c => c.Citas)
    .FirstOrDefaultAsync(c => c.Id == 5);
// clienteConCitas.Citas = [Cita1, Cita2, ...]
```

### Include múltiple

```csharp
// Cliente con todas sus relaciones
var cliente = await _db.Clientes
    .Include(c => c.Citas)
    .Include(c => c.Trabajos)
    .Include(c => c.Consentimientos)
    .FirstOrDefaultAsync(c => c.Id == 5);
```

### Crear con relación

```csharp
// Opción 1: Crear cita con ClienteId
var cita = new Cita
{
    Fecha = DateTime.Now.AddDays(7),
    ClienteId = 5  // ID del cliente existente
};
_db.Citas.Add(cita);

// Opción 2: Crear cita añadiendo al cliente
var cliente = await _db.Clientes.FindAsync(5);
cliente.Citas.Add(new Cita { Fecha = DateTime.Now.AddDays(7) });

await _db.SaveChangesAsync();
```

---

## ⚠️ Errores Comunes

### Error: "No se encuentra el comando 'ef'"

```powershell
# Solución: instalar herramienta EF
dotnet tool install --global dotnet-ef
```

### Error: "No se puede crear migración"

```powershell
# Asegúrate de estar en la carpeta del proyecto
cd Ataena
dotnet ef migrations add Nombre
```

### Error: "La tabla ya existe"

```powershell
# Elimina la BD y vuelve a aplicar migraciones
# (solo en desarrollo, perderás datos)
del $env:LOCALAPPDATA\Ataena\data.db
dotnet ef database update
```

### Error: "Object reference not set"

```csharp
// ❌ MAL: puede ser null
var cliente = await _db.Clientes.FindAsync(999);
var nombre = cliente.Nombre;  // Error si no existe

// ✅ BIEN: verificar null
var cliente = await _db.Clientes.FindAsync(999);
if (cliente != null)
{
    var nombre = cliente.Nombre;
}
```

### Error: "Cannot track multiple instances"

```csharp
// ❌ MAL: múltiples DbContext
var db1 = new AtaenaDbContext();
var db2 = new AtaenaDbContext();
var cliente = await db1.Clientes.FindAsync(5);
db2.Clientes.Update(cliente);  // Error!

// ✅ BIEN: un solo DbContext
var cliente = await _db.Clientes.FindAsync(5);
cliente.Nombre = "Nuevo";
await _db.SaveChangesAsync();
```

---

## 🔗 Siguiente documento

→ `05-crear-pantalla.md` - Guía completa de XAML y Views

