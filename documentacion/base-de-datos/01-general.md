# Base de Datos - Documento General

> **Versión:** 1.0  
> **Fecha:** Diciembre 2025  
> **Estado:** Planificación

---

## 📋 Índice

1. [Tecnología Elegida](#-tecnología-elegida)
2. [Ubicación del Archivo](#-ubicación-del-archivo)
3. [Diagrama de Entidades](#-diagrama-de-entidades)
4. [Tablas del Sistema](#-tablas-del-sistema)
5. [Tipos de Consentimiento](#-tipos-de-consentimiento)
6. [Migraciones](#-migraciones)
7. [Escalabilidad Futura](#-escalabilidad-futura)

---

## 🛠️ Tecnología Elegida

| Componente | Tecnología | Versión |
|------------|------------|---------|
| **Motor de BD** | SQLite | 3.x |
| **ORM** | Entity Framework Core | 8.0 |
| **Proveedor** | Microsoft.EntityFrameworkCore.Sqlite | 8.0.10 |

### ¿Por qué SQLite?

| Ventaja | Descripción |
|---------|-------------|
| **Sin servidor** | No requiere instalación de MySQL, PostgreSQL, etc. |
| **Archivo único** | Toda la BD es un solo archivo `.db` |
| **Empaquetable** | Se distribuye junto con el instalador |
| **Rendimiento** | Más que suficiente para miles de registros |
| **Backup simple** | Copiar el archivo = backup completo |

### ¿Por qué Entity Framework Core?

| Ventaja | Descripción |
|---------|-------------|
| **Code First** | Definimos clases C# → EF crea las tablas |
| **Migraciones** | Cambios en el modelo → EF actualiza la BD |
| **LINQ** | Consultas tipadas, sin SQL manual |
| **Relaciones** | Navegación entre entidades automática |

---

## 📁 Ubicación del Archivo

La base de datos se almacena en la carpeta de datos de la aplicación del usuario:

```
Windows:
C:\Users\{Usuario}\AppData\Local\Ataena\data.db
```

### Estructura de carpetas de la aplicación:

```
C:\Users\{Usuario}\AppData\Local\Ataena\
│
├── data.db                 ← Base de datos principal
├── data.db-wal             ← Write-Ahead Log (rendimiento)
├── data.db-shm             ← Shared memory (temporal)
│
├── backups/                ← Copias de seguridad
│   ├── backup_2025-12-01.db
│   └── backup_2025-12-05.db
│
├── documentos/             ← Consentimientos firmados (PDF)
│   ├── cliente_1/
│   │   ├── consentimiento_rgpd.pdf
│   │   ├── consentimiento_imagenes.pdf
│   │   └── trabajo_15_consentimiento.pdf
│   └── cliente_2/
│       └── ...
│
└── fotos/                  ← Fotos de trabajos
    ├── trabajo_1/
    │   ├── antes.jpg
    │   └── despues.jpg
    └── trabajo_2/
        └── ...
```

### Código para obtener la ruta:

```csharp
var appDataPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "Ataena"
);

var dbPath = Path.Combine(appDataPath, "data.db");
```

---

## 📊 Diagrama de Entidades

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         DIAGRAMA DE ENTIDADES                           │
└─────────────────────────────────────────────────────────────────────────┘

    ┌───────────────────┐
    │      CLIENTE      │
    ├───────────────────┤
    │ Id                │
    │ Nombre            │
    │ Apellidos         │
    │ Telefono          │
    │ Email             │
    │ FechaNacimiento   │
    │ Alergias          │
    │ Notas             │
    │ FechaRegistro     │
    │ ConsentimientoRGPD│───────┐
    │ ConsentimientoImg │       │
    └────────┬──────────┘       │
             │                  │
             │ 1:N              │
             ▼                  │
    ┌───────────────────┐       │
    │       CITA        │       │
    ├───────────────────┤       │
    │ Id                │       │
    │ ClienteId (FK)    │       │
    │ Fecha             │       │
    │ HoraInicio        │       │
    │ Duracion          │       │
    │ Tipo              │ (Tatuaje/Piercing/Consulta)
    │ Descripcion       │       │
    │ Estado            │       │
    │ EmailEnviado      │       │
    └───────────────────┘       │
             │                  │
             │                  │
             ▼                  │
    ┌───────────────────┐       │
    │      TRABAJO      │       │
    ├───────────────────┤       │
    │ Id                │       │
    │ ClienteId (FK)    │       │
    │ CitaId (FK)       │ (opcional, puede ser sin cita previa)
    │ Tipo              │ (Tatuaje/Piercing)
    │ Descripcion       │       │
    │ ZonaCuerpo        │       │
    │ Estilo            │ (solo tatuajes)
    │ Precio            │       │
    │ Fecha             │       │
    │ FotosJson         │       │
    │ Notas             │       │
    └────────┬──────────┘       │
             │                  │
             │ 1:1              │
             ▼                  │
    ┌───────────────────┐       │
    │   CONSENTIMIENTO  │◄──────┘
    ├───────────────────┤
    │ Id                │
    │ ClienteId (FK)    │
    │ TrabajoId (FK)    │ (null si es RGPD o Imágenes)
    │ Tipo              │ (RGPD/Imagenes/Trabajo)
    │ FechaFirma        │
    │ RutaDocumento     │ (ruta al PDF firmado)
    │ Firmado           │
    └───────────────────┘


    ┌───────────────────┐
    │   CONFIGURACION   │  (Tabla independiente, 1 solo registro)
    ├───────────────────┤
    │ Id                │
    │ NombreEstudio     │
    │ Direccion         │
    │ Telefono          │
    │ Email             │
    │ LogoPath          │
    │ SmtpServidor      │
    │ SmtpPuerto        │
    │ SmtpUsuario       │
    │ SmtpPassword      │ (cifrado)
    │ TemaOscuro        │
    └───────────────────┘
```

---

## 📋 Tablas del Sistema

### Resumen de tablas:

| Tabla | Descripción | Relaciones |
|-------|-------------|------------|
| **Cliente** | Datos personales del cliente | Tiene muchas Citas, Trabajos y Consentimientos |
| **Cita** | Agenda de citas | Pertenece a un Cliente, puede tener un Trabajo |
| **Trabajo** | Tatuaje o piercing realizado | Pertenece a un Cliente, tiene un Consentimiento |
| **Consentimiento** | Documentos firmados | Pertenece a un Cliente, opcionalmente a un Trabajo |
| **Configuracion** | Datos del estudio y ajustes | Independiente (1 registro) |

### Detalle de cada tabla:

> **Nota:** El detalle completo de campos, tipos y validaciones está en el documento `02-esquema-tablas.md`

---

## 📝 Tipos de Consentimiento

El sistema maneja **3 tipos de consentimientos**:

| Tipo | Código | Cuándo se firma | Guardado |
|------|--------|-----------------|----------|
| **RGPD** | `RGPD` | 1 vez por cliente, al registrarse | Cliente guarda copia, nosotros registramos que firmó |
| **Imágenes** | `IMAGENES` | 1 vez por cliente (opcional) | Autoriza uso en redes sociales |
| **Por trabajo** | `TRABAJO` | 1 por cada tatuaje/piercing | Obligatorio antes de cada trabajo |

### Flujo de consentimientos:

```
NUEVO CLIENTE
     │
     ├──→ Firma Consentimiento RGPD (obligatorio)
     │         └──→ Se guarda registro en BD + PDF en carpeta
     │
     ├──→ Firma Consentimiento Imágenes (opcional)
     │         └──→ Se guarda registro en BD + PDF en carpeta
     │
     └──→ Cliente registrado ✓


NUEVO TRABAJO (Tatuaje/Piercing)
     │
     ├──→ Verificar que tiene RGPD firmado
     │
     ├──→ Firma Consentimiento de Trabajo (obligatorio)
     │         └──→ Se vincula al trabajo específico
     │
     └──→ Trabajo registrado ✓
```

---

## 🔄 Migraciones

Entity Framework Core gestiona los cambios en el esquema de la base de datos mediante **migraciones**.

### Comandos principales:

```powershell
# Crear una nueva migración
dotnet ef migrations add NombreDescriptivo

# Aplicar migraciones pendientes
dotnet ef database update

# Ver migraciones pendientes
dotnet ef migrations list

# Revertir última migración (si no se ha aplicado)
dotnet ef migrations remove
```

### Ejemplo de flujo:

```
1. Añades un campo nuevo a la clase Cliente
2. Ejecutas: dotnet ef migrations add AgregarCampoAlergias
3. EF genera archivo de migración en /Migrations
4. Ejecutas: dotnet ef database update
5. La BD se actualiza sin perder datos
```

### Migraciones en producción:

Cuando la app se instala en el PC del usuario, las migraciones se aplican automáticamente al iniciar:

```csharp
// En Program.cs o App.axaml.cs
using var db = new AtaenaDbContext();
db.Database.Migrate(); // Aplica migraciones pendientes
```

---

## 🚀 Escalabilidad Futura

El diseño actual es simple pero preparado para crecer. Posibles ampliaciones:

### Tabla Artista (futuro)

Si el estudio crece y necesita gestionar varios tatuadores/piercers:

```
┌───────────────────┐
│      ARTISTA      │
├───────────────────┤
│ Id                │
│ Nombre            │
│ Especialidad      │ (Tatuaje/Piercing/Ambos)
│ Email             │
│ Telefono          │
│ Activo            │
└───────────────────┘

→ Cita tendría ArtistaId (FK)
→ Trabajo tendría ArtistaId (FK)
```

### Tabla Producto/Material (futuro)

Para gestionar inventario de tintas, agujas, etc.:

```
┌───────────────────┐
│     PRODUCTO      │
├───────────────────┤
│ Id                │
│ Nombre            │
│ Categoria         │ (Tinta/Aguja/Material)
│ Stock             │
│ StockMinimo       │
│ Proveedor         │
└───────────────────┘
```

### Tabla Factura (futuro)

Para facturación y contabilidad:

```
┌───────────────────┐
│      FACTURA      │
├───────────────────┤
│ Id                │
│ ClienteId (FK)    │
│ TrabajoId (FK)    │
│ Numero            │
│ Fecha             │
│ Total             │
│ Estado            │
└───────────────────┘
```

---

## 📚 Documentos Relacionados

- `02-esquema-tablas.md` - Detalle completo de cada tabla (campos, tipos, validaciones)
- `03-backup-exportar.md` - Operaciones de backup, exportar e importar datos

---

> **Siguiente paso:** Crear el documento de esquema de tablas con el detalle de cada campo.

