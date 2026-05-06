# Ataena CRM - Documento de Idea General

> **Versión:** 3.1  
> **Fecha:** Febrero 2026  
> **Autor:** Jose Vallejo  
> **Estado:** En Desarrollo - Fase 4 en progreso (~90% del proyecto)

---

## 📋 Índice

1. [Descripción del Proyecto](#-descripción-del-proyecto)
2. [Público Objetivo](#-público-objetivo)
3. [Stack Tecnológico](#-stack-tecnológico)
4. [Arquitectura](#-arquitectura)
5. [Estructura del Proyecto](#-estructura-del-proyecto)
6. [Módulos Funcionales](#-módulos-funcionales)
7. [Modelo de Datos](#-modelo-de-datos)
8. [Interfaz de Usuario](#-interfaz-de-usuario)
9. [Seguridad (Fase Posterior)](#-seguridad-fase-posterior)
10. [Entorno de Desarrollo](#-entorno-de-desarrollo)

---

## 📝 Descripción del Proyecto

**Ataena CRM** es una aplicación de escritorio para Windows diseñada específicamente para la gestión de clientes en **estudios de tatuajes**.

### Características principales:

- **100% Local:** Base de datos SQLite instalada junto con la aplicación
- **Sin dependencias externas:** No requiere conexión a internet ni servidores
- **Instalador único:** Ejecutable de instalación que incluye todo lo necesario
- **Interfaz moderna:** Diseño Windows 11 con tema Fluent

### Problema que resuelve:

Muchos estudios de tatuajes gestionan sus clientes con libretas, Excel, o herramientas genéricas no adaptadas a sus necesidades. Ataena CRM ofrece una solución simple, profesional y específica para tatuadores.

---

## 🎯 Público Objetivo

**Estudios de tatuajes** y tatuadores independientes.

### Necesidades específicas del sector:

- Galería de trabajos realizados
- Consentimientos informados
- Historial de diseños por cliente
- Gestión de citas y sesiones
- Registro de zonas tatuadas

### Perfil del usuario:

- Tatuador independiente o estudio pequeño (1-5 artistas)
- Conocimientos informáticos básicos
- Necesita solución simple y rápida
- No quiere depender de suscripciones mensuales

---

## 🛠️ Stack Tecnológico

### Decisiones tomadas:

| Componente | Tecnología | Justificación |
|------------|------------|---------------|
| **Framework UI** | Avalonia UI 11 | Multiplataforma, moderno, funciona en Cursor/VS Code |
| **Tema visual** | FluentAvalonia | Aspecto Windows 11 nativo, profesional |
| **Patrón arquitectura** | MVVM | Simple, probado, fácil de mantener |
| **MVVM Toolkit** | CommunityToolkit.Mvvm | Source Generators, poco boilerplate |
| **Base de datos** | SQLite | Local, sin servidor, empaquetable |
| **ORM** | Entity Framework Core 9 | Maduro, migraciones automáticas |
| **Lenguaje** | C# 12 / .NET 9 | Moderno, rendimiento nativo |
| **IDE** | Cursor | Desarrollo completo sin Visual Studio |

### Paquetes NuGet principales:

```xml
<!-- UI -->
<PackageReference Include="Avalonia" Version="11.2.1" />
<PackageReference Include="Avalonia.Desktop" Version="11.2.1" />
<PackageReference Include="Avalonia.Themes.Fluent" Version="11.2.1" />
<PackageReference Include="FluentAvaloniaUI" Version="2.1.0" />

<!-- MVVM -->
<PackageReference Include="CommunityToolkit.Mvvm" Version="8.3.2" />

<!-- Base de datos -->
<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="9.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="9.0.0" />
```

---

## 📐 Arquitectura

### Patrón: MVVM (Model-View-ViewModel)

Se eligió una arquitectura **simple y directa**, evitando sobre-ingeniería como Clean Architecture, que sería excesiva para un proyecto desarrollado por una sola persona.

```
┌─────────────────────────────────────────────────────────────────┐
│                         ARQUITECTURA                            │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   ┌─────────────┐     ┌─────────────┐     ┌─────────────┐      │
│   │             │     │             │     │             │      │
│   │    VIEW     │ ←── │  VIEWMODEL  │ ←── │    MODEL    │      │
│   │   (XAML)    │     │    (C#)     │     │  (Entidad)  │      │
│   │             │     │             │     │             │      │
│   └─────────────┘     └─────────────┘     └─────────────┘      │
│         ↑                   ↑                   ↑               │
│         │                   │                   │               │
│    Interfaz de         Lógica de           Datos y             │
│     usuario           presentación         reglas              │
│                             │                                   │
│                             ↓                                   │
│                    ┌─────────────────┐                         │
│                    │   DbContext     │                         │
│                    │   (EF Core)     │                         │
│                    └────────┬────────┘                         │
│                             │                                   │
│                             ↓                                   │
│                    ┌─────────────────┐                         │
│                    │     SQLite      │                         │
│                    │   (database.db) │                         │
│                    └─────────────────┘                         │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### Principios de diseño:

1. **Simplicidad:** Un solo proyecto, carpetas organizadas
2. **Pragmatismo:** EF Core directo en ViewModels (sin capas innecesarias)
3. **Mantenibilidad:** Código fácil de entender hoy y dentro de 2 años
4. **Escalabilidad gradual:** Añadir complejidad solo cuando se necesite

---

## 📁 Estructura del Proyecto

```
Ataena/
│
├── 📁 Assets/                    # Recursos estáticos
│   ├── 📁 Fonts/                 # Fuentes personalizadas
│   ├── 📁 Icons/                 # Iconos de la aplicación
│   └── app-icon.ico              # Icono principal
│
├── 📁 Models/                    # Entidades de datos
│   ├── Cliente.cs
│   ├── Cita.cs
│   ├── Trabajo.cs
│   ├── Nota.cs
│   └── Usuario.cs
│
├── 📁 Data/                      # Capa de datos
│   ├── AtaenaDbContext.cs        # Contexto de EF Core
│   └── 📁 Migrations/            # Migraciones de BD
│
├── 📁 ViewModels/                # Lógica de presentación
│   ├── ViewModelBase.cs          # Clase base
│   ├── MainViewModel.cs          # ViewModel principal
│   ├── DashboardViewModel.cs
│   ├── ClientesViewModel.cs
│   ├── ClienteDetalleViewModel.cs
│   ├── AgendaViewModel.cs
│   └── ConfiguracionViewModel.cs
│
├── 📁 Views/                     # Interfaces de usuario (XAML)
│   ├── MainWindow.axaml
│   ├── DashboardView.axaml
│   ├── ClientesView.axaml
│   ├── ClienteDetalleView.axaml
│   ├── AgendaView.axaml
│   └── ConfiguracionView.axaml
│
├── 📁 Services/                  # Servicios auxiliares
│   ├── NavigationService.cs      # Navegación entre vistas
│   └── DialogService.cs          # Diálogos y confirmaciones
│
├── 📁 Converters/                # Conversores para XAML
│   └── BoolToVisibilityConverter.cs
│
├── App.axaml                     # Configuración de la app
├── App.axaml.cs                  # Código de inicio
├── Program.cs                    # Entry point
└── Ataena.csproj                 # Archivo de proyecto
```

---

## 🧩 Módulos Funcionales

### 1. Dashboard (Pantalla principal)

| Elemento | Descripción |
|----------|-------------|
| Resumen del día | Citas programadas para hoy |
| Estadísticas | Total clientes, citas del mes, ingresos |
| Accesos rápidos | Nuevo cliente, nueva cita |
| Alertas | Citas pendientes de confirmar |

### 2. Gestión de Clientes

| Funcionalidad | Descripción |
|---------------|-------------|
| Listado | Vista de todos los clientes con búsqueda |
| Ficha completa | Datos personales, historial, notas |
| CRUD | Crear, editar, eliminar clientes |
| Búsqueda | Por nombre, teléfono, email |
| Etiquetas | VIP, nuevo, frecuente, etc. |

### 3. Agenda / Citas

| Funcionalidad | Descripción |
|---------------|-------------|
| Calendario | Vista día, semana, mes |
| Crear cita | Asociar cliente, duración, descripción |
| Estados | Pendiente, confirmada, completada, cancelada |
| Recordatorios | Visual en dashboard |

### 4. Trabajos / Galería

| Funcionalidad | Descripción |
|---------------|-------------|
| Registro | Tatuaje realizado con fotos (antes/después) |
| Galería | Portfolio de trabajos por cliente |
| Detalles | Zona del cuerpo, estilo, precio, duración |
| Consentimiento | Documento firmado asociado al trabajo |

### 5. Configuración

| Funcionalidad | Descripción |
|---------------|-------------|
| Datos negocio | Nombre, logo, información |
| Backup | Exportar/importar base de datos |
| Tema | Claro/oscuro |
| Acerca de | Versión, créditos |

---

## 🗄️ Modelo de Datos

### Diagrama de entidades:

```
┌─────────────────┐       ┌─────────────────┐
│     CLIENTE     │       │      CITA       │
├─────────────────┤       ├─────────────────┤
│ Id              │───┐   │ Id              │
│ Nombre          │   │   │ Fecha           │
│ Apellidos       │   │   │ Duracion        │
│ Telefono        │   └──→│ ClienteId (FK)  │
│ Email           │       │ Descripcion     │
│ FechaRegistro   │       │ Estado          │
│ EsVip           │       └─────────────────┘
│ Notas           │
└────────┬────────┘       ┌─────────────────┐
         │                │     TRABAJO     │
         │                ├─────────────────┤
         │                │ Id              │
         └───────────────→│ ClienteId (FK)  │
                          │ Titulo          │
                          │ Descripcion     │
                          │ Precio          │
                          │ Fecha           │
                          │ Fotos (JSON)    │
                          └─────────────────┘
```

### Entidades principales:

```csharp
public class Cliente
{
    public int Id { get; set; }
    public string Nombre { get; set; }
    public string Apellidos { get; set; }
    public string Telefono { get; set; }
    public string? Email { get; set; }
    public DateTime FechaRegistro { get; set; }
    public bool EsVip { get; set; }
    public string? Notas { get; set; }
    
    // Navegación
    public List<Cita> Citas { get; set; }
    public List<Trabajo> Trabajos { get; set; }
}

public class Cita
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; }
    public TimeSpan Duracion { get; set; }
    public string Descripcion { get; set; }
    public EstadoCita Estado { get; set; }
    
    public int ClienteId { get; set; }
    public Cliente Cliente { get; set; }
}

public class Trabajo
{
    public int Id { get; set; }
    public string Titulo { get; set; }
    public string? Descripcion { get; set; }
    public decimal Precio { get; set; }
    public DateTime Fecha { get; set; }
    public string? FotosJson { get; set; } // Lista de rutas serializada
    
    public int ClienteId { get; set; }
    public Cliente Cliente { get; set; }
}

public enum EstadoCita
{
    Pendiente,
    Confirmada,
    Completada,
    Cancelada
}
```

---

## 🎨 Interfaz de Usuario

### Diseño general:

```
┌─────────────────────────────────────────────────────────────────────┐
│  🖋️ Ataena CRM - [Nombre del estudio]                         _ □ ✕     │
├─────────────┬───────────────────────────────────────────────────────┤
│             │                                                       │
│  🏠 Dashboard│                                                       │
│             │              CONTENIDO PRINCIPAL                      │
│  👥 Clientes │              (Vista actual)                           │
│             │                                                       │
│  📅 Agenda  │                                                       │
│             │                                                       │
│  💼 Trabajos │                                                       │
│             │                                                       │
│  ─────────  │                                                       │
│             │                                                       │
│  ⚙️ Config  │                                                       │
│             │                                                       │
└─────────────┴───────────────────────────────────────────────────────┘
```

### Características visuales (FluentAvalonia):

- ✅ Tema Windows 11 (Fluent Design)
- ✅ Soporte tema claro/oscuro
- ✅ NavigationView (menú lateral)
- ✅ Bordes redondeados
- ✅ Iconos Fluent
- ✅ Efecto Mica (si el sistema lo soporta)

---

## 🔐 Seguridad (Fase Posterior)

> **Nota:** La seguridad se implementará en una fase posterior del desarrollo.

### Capas de seguridad planificadas:

1. **Autenticación:** Login con usuario/contraseña (BCrypt)
2. **Cifrado de BD:** SQLCipher (AES-256)
3. **Cifrado de campos sensibles:** Datos personales cifrados individualmente
4. **Backup cifrado:** Copias de seguridad protegidas
5. **Auditoría:** Log de acciones del usuario

---

## 💻 Entorno de Desarrollo

### Requisitos:

| Requisito | Versión |
|-----------|---------|
| .NET SDK | 9.0 o superior |
| IDE | Cursor / VS Code |
| Sistema operativo | Windows 10/11 |

### Configuración inicial:

```powershell
# Verificar .NET instalado
dotnet --version

# Instalar workload de Avalonia (si no está)
dotnet new install Avalonia.Templates

# Crear proyecto (cuando empecemos)
dotnet new avalonia.app -n Ataena -o Ataena
```

### Extensiones recomendadas para Cursor:

- C# Dev Kit (Microsoft)
- Avalonia for VS Code
- XAML Language Support

### Comandos útiles:

```powershell
# Compilar
dotnet build

# Ejecutar
dotnet run

# Crear migración de BD
dotnet ef migrations add NombreMigracion

# Aplicar migraciones
dotnet ef database update
```

---

## 📅 Próximos Pasos

> **Ver documento detallado:** `02-roadmap.md`

### Fase 1: Fundamentos ✅
- [x] Configurar proyecto Avalonia base
- [x] Configurar FluentAvalonia
- [x] Crear estructura de carpetas
- [x] Implementar modelos de datos
- [x] Configurar Entity Framework Core + SQLite
- [x] Crear primera migración
- [x] Implementar MainWindow con navegación
- [x] Desarrollar Dashboard

### Fase 2: Módulos Principales ✅
- [x] Desarrollar módulo de Clientes (CRUD completo)
- [x] Desarrollar módulo de Agenda (calendario y citas)
- [x] Desarrollar módulo de Trabajos (CRUD completo)
- [x] Sistema de Logs integrado
- [x] Pulir interfaz y UX (diseño moderno con degradados)
- [x] Modales con efectos visuales modernos
- [x] Estilos globales de botones con degradados

### Fase 3: Funcionalidades Adicionales ✅
- [x] **Módulo de Consentimientos** (100% completado)
  - ✅ Sistema de firma dual (móvil táctil / PC mouse)
  - ✅ Generación de PDFs legalmente válidos
  - ✅ Integración en flujos de Clientes (RGPD e Imágenes opcionales)
  - ✅ Integración en Trabajos (consentimiento por trabajo)
  - ✅ Vista de Consentimientos global con filtros
  - Ver plan detallado: `documentacion/03-plan-consentimientos.md`
- [x] **Módulo de Configuración** (completado)
  - ✅ Datos del estudio
  - ✅ Configuración de backups
- [x] **Fotos de Trabajos** (completado)
  - ✅ Captura desde móvil (antes/después)
  - ✅ Almacenamiento local
  - ✅ Galería en modal de trabajo
- [x] **Sistema de Backup y Restauración** (completado)
  - ✅ Crear backups locales
  - ✅ Sincronización con nube (OneDrive, Google Drive, Dropbox)
  - ✅ Restaurar backups
  - ✅ Rotación automática de backups
  - ✅ Configuración de frecuencia y retención

### Completado recientemente ✅
- [x] Configuración SMTP y sistema de emails (recordatorio de citas)
- [x] Escáner de DNI (WIA) e Impresora de consentimientos
- [x] Renombrado del proyecto a Ataena

### Pendiente ⏳
- [ ] Crear instalador (Inno Setup o Velopack)
- [ ] Icono de aplicación profesional
- [ ] Testing manual completo
- [ ] Tema claro/oscuro
- [ ] Implementar seguridad (login, cifrado)

---

## 📚 Referencias

- [Avalonia UI Documentation](https://docs.avaloniaui.net/)
- [FluentAvalonia GitHub](https://github.com/amwx/FluentAvalonia)
- [CommunityToolkit.Mvvm](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/)
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)

---

> **Documento creado como guía inicial del proyecto. Se actualizará conforme avance el desarrollo.**

