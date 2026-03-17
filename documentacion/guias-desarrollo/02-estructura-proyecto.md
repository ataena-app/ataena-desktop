# Estructura del Proyecto

> **Nivel:** Principiante  
> **Objetivo:** Saber qué hay en cada carpeta y archivo, y dónde poner cada cosa

---

## 📋 Índice

1. [Vista General](#-vista-general)
2. [Carpeta Models](#-carpeta-models)
3. [Carpeta ViewModels](#-carpeta-viewmodels)
4. [Carpeta Views](#-carpeta-views)
5. [Carpeta Data](#-carpeta-data)
6. [Carpeta Services](#-carpeta-services)
7. [Carpeta Assets](#-carpeta-assets)
8. [Archivos Raíz](#-archivos-raíz)
9. [Regla de Oro](#-regla-de-oro)

---

## 🗂️ Vista General

```
Ataena/
│
├── 📁 Assets/                    # Recursos (iconos, imágenes, fuentes)
│
├── 📁 Models/                    # Entidades de datos (Cliente, Cita, etc.)
│
├── 📁 ViewModels/                # Lógica de cada pantalla
│
├── 📁 Views/                     # Pantallas (archivos XAML)
│
├── 📁 Data/                      # Base de datos (DbContext, migraciones)
│
├── 📁 Services/                  # Servicios auxiliares (navegación, etc.)
│
├── 📁 Converters/                # Conversores para XAML
│
├── 📄 App.axaml                  # Configuración visual global
├── 📄 App.axaml.cs               # Código de inicio de la app
├── 📄 Program.cs                 # Punto de entrada (Main)
└── 📄 Ataena.csproj           # Configuración del proyecto
```

---

## 📦 Carpeta Models

### ¿Qué va aquí?

Las **entidades de datos**. Clases que representan los objetos de tu negocio.

### Ubicación:
```
Ataena/Models/
```

### Archivos típicos:
```
Models/
├── Cliente.cs
├── Cita.cs
├── Trabajo.cs
├── Consentimiento.cs
└── Configuracion.cs
```

### Ejemplo de archivo:

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
    public DateTime FechaRegistro { get; set; } = DateTime.Now;
    public bool EsVip { get; set; }
    public string? Notas { get; set; }
    
    // Relaciones con otras entidades
    public List<Cita> Citas { get; set; } = new();
    public List<Trabajo> Trabajos { get; set; } = new();
    
    // Propiedades calculadas (no se guardan en BD)
    public string NombreCompleto => $"{Nombre} {Apellidos}";
}
```

### ¿Cuándo crear un nuevo Model?

- Cuando necesitas guardar un nuevo tipo de dato en la base de datos
- Ejemplo: Quieres guardar productos → Creas `Models/Producto.cs`

---

## 🧠 Carpeta ViewModels

### ¿Qué va aquí?

La **lógica de cada pantalla**. Cada pantalla (View) tiene su ViewModel correspondiente.

### Ubicación:
```
Ataena/ViewModels/
```

### Archivos típicos:
```
ViewModels/
├── ViewModelBase.cs          # Clase base (heredan todos)
├── MainWindowViewModel.cs    # ViewModel de la ventana principal
├── DashboardViewModel.cs     # ViewModel del dashboard
├── ClientesViewModel.cs      # ViewModel de la lista de clientes
├── ClienteDetalleViewModel.cs # ViewModel del detalle de un cliente
└── AgendaViewModel.cs        # ViewModel de la agenda
```

### Convención de nombres:

```
Vista:          ClientesView.axaml
ViewModel:      ClientesViewModel.cs
                ^^^^^^^^ mismo nombre + "ViewModel"
```

### Ejemplo de archivo:

```csharp
// ViewModels/ClientesViewModel.cs
namespace Ataena.ViewModels;

public partial class ClientesViewModel : ViewModelBase
{
    private readonly AtaenaDbContext _db = new();

    // Propiedades que la View puede mostrar
    [ObservableProperty]
    private ObservableCollection<Cliente> _clientes = new();

    [ObservableProperty]
    private string _busqueda = string.Empty;

    [ObservableProperty]
    private Cliente? _clienteSeleccionado;

    // Comandos que la View puede ejecutar
    [RelayCommand]
    private async Task CargarClientes()
    {
        var lista = await _db.Clientes.ToListAsync();
        Clientes = new ObservableCollection<Cliente>(lista);
    }

    [RelayCommand]
    private async Task Guardar()
    {
        await _db.SaveChangesAsync();
    }
}
```

### ¿Cuándo crear un nuevo ViewModel?

- Cada vez que crees una nueva pantalla (View)
- Si una pantalla es muy compleja, puedes dividirla en varios ViewModels

---

## 🖼️ Carpeta Views

### ¿Qué va aquí?

Las **pantallas** de la aplicación. Archivos XAML que definen cómo se ve cada pantalla.

### Ubicación:
```
Ataena/Views/
```

### Archivos típicos:
```
Views/
├── MainWindow.axaml          # Ventana principal
├── MainWindow.axaml.cs       # Código behind (mínimo)
├── DashboardView.axaml       # Pantalla de inicio
├── ClientesView.axaml        # Lista de clientes
├── ClienteDetalleView.axaml  # Detalle de un cliente
└── AgendaView.axaml          # Calendario de citas
```

### Estructura de un archivo View:

Cada View tiene **2 archivos**:

```
ClientesView.axaml       ← Diseño visual (XAML)
ClientesView.axaml.cs    ← Código behind (C#, mínimo)
```

### Ejemplo de archivo XAML:

```xml
<!-- Views/ClientesView.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:Ataena.ViewModels"
             x:Class="Ataena.Views.ClientesView"
             x:DataType="vm:ClientesViewModel">
    
    <DockPanel>
        <!-- Barra de búsqueda -->
        <TextBox DockPanel.Dock="Top"
                 Text="{Binding Busqueda}"
                 Watermark="Buscar cliente..."/>
        
        <!-- Lista de clientes -->
        <ListBox ItemsSource="{Binding Clientes}"
                 SelectedItem="{Binding ClienteSeleccionado}">
            <ListBox.ItemTemplate>
                <DataTemplate>
                    <TextBlock Text="{Binding NombreCompleto}"/>
                </DataTemplate>
            </ListBox.ItemTemplate>
        </ListBox>
    </DockPanel>
    
</UserControl>
```

### Ejemplo de código behind (mínimo):

```csharp
// Views/ClientesView.axaml.cs
namespace Ataena.Views;

public partial class ClientesView : UserControl
{
    public ClientesView()
    {
        InitializeComponent();
    }
}
```

### ¿Cuándo crear una nueva View?

- Cada pantalla nueva de la aplicación
- Un popup o diálogo también es una View

---

## 🗄️ Carpeta Data

### ¿Qué va aquí?

Todo lo relacionado con la **base de datos**: DbContext, configuraciones, migraciones.

### Ubicación:
```
Ataena/Data/
```

### Archivos típicos:
```
Data/
├── AtaenaDbContext.cs     # Conexión a la BD
├── Migrations/               # Migraciones (generadas automáticamente)
│   ├── 20251205_Initial.cs
│   └── ...
└── Configurations/           # (Opcional) Configuraciones de tablas
    └── ClienteConfiguration.cs
```

### Ejemplo de DbContext:

```csharp
// Data/AtaenaDbContext.cs
namespace Ataena.Data;

public class AtaenaDbContext : DbContext
{
    // Cada DbSet es una tabla
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Cita> Citas => Set<Cita>();
    public DbSet<Trabajo> Trabajos => Set<Trabajo>();
    public DbSet<Consentimiento> Consentimientos => Set<Consentimiento>();
    public DbSet<Configuracion> Configuracion => Set<Configuracion>();

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        var appData = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData);
        var dbPath = Path.Combine(appData, "Ataena", "data.db");
        
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
        options.UseSqlite($"Data Source={dbPath}");
    }
}
```

### ¿Cuándo modificar Data/?

- Cuando añades una nueva entidad (Model), añádela al DbContext
- Después ejecuta: `dotnet ef migrations add NombreMigracion`

---

## ⚙️ Carpeta Services

### ¿Qué va aquí?

**Servicios auxiliares** que no pertenecen a una pantalla específica.

### Ubicación:
```
Ataena/Services/
```

### Archivos típicos:
```
Services/
├── NavigationService.cs      # Navegar entre pantallas
├── DialogService.cs          # Mostrar popups y confirmaciones
├── EmailService.cs           # Enviar emails
└── BackupService.cs          # Hacer backups de la BD
```

### Ejemplo de servicio:

```csharp
// Services/DialogService.cs
namespace Ataena.Services;

public class DialogService
{
    public async Task<bool> ConfirmarAsync(string mensaje)
    {
        // Mostrar diálogo de confirmación
        // Retorna true si el usuario acepta
    }
    
    public async Task MostrarErrorAsync(string mensaje)
    {
        // Mostrar diálogo de error
    }
}
```

### ¿Cuándo crear un nuevo Service?

- Lógica que se usa en múltiples ViewModels
- Funcionalidades externas (email, archivos, etc.)
- Cuando un ViewModel se vuelve muy grande, extrae lógica a servicios

---

## 🎨 Carpeta Assets

### ¿Qué va aquí?

**Recursos estáticos**: imágenes, iconos, fuentes.

### Ubicación:
```
Ataena/Assets/
```

### Estructura típica:
```
Assets/
├── app-icon.ico              # Icono de la aplicación
├── logo.png                  # Logo del estudio
├── Fonts/                    # Fuentes personalizadas
│   └── CustomFont.ttf
└── Icons/                    # Iconos de la UI
    ├── cliente.svg
    └── calendario.svg
```

### Cómo usar un recurso en XAML:

```xml
<Image Source="/Assets/logo.png" Width="100"/>
```

---

## 📄 Archivos Raíz

### Program.cs

**Punto de entrada** de la aplicación. Normalmente no lo tocas.

```csharp
// Program.cs
public class Program
{
    public static void Main(string[] args) 
        => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
```

### App.axaml

**Configuración visual global**: temas, estilos, recursos compartidos.

```xml
<!-- App.axaml -->
<Application xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="Ataena.App">
    <Application.Styles>
        <FluentTheme />
    </Application.Styles>
</Application>
```

### App.axaml.cs

**Código de inicio**: qué ventana abrir, inicializar servicios, aplicar migraciones.

```csharp
// App.axaml.cs
public partial class App : Application
{
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };
        }
        base.OnFrameworkInitializationCompleted();
    }
}
```

### Ataena.csproj

**Configuración del proyecto**: versión de .NET, paquetes NuGet, etc.

---

## 🏆 Regla de Oro

### ¿Dónde pongo mi código?

```
┌─────────────────────────────────────────────────────────────────┐
│  PREGUNTA                           CARPETA                     │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ¿Es un dato que se guarda?         → Models/                   │
│                                                                 │
│  ¿Es cómo se ve algo?               → Views/                    │
│                                                                 │
│  ¿Es lógica de una pantalla?        → ViewModels/               │
│                                                                 │
│  ¿Tiene que ver con la BD?          → Data/                     │
│                                                                 │
│  ¿Es lógica compartida/externa?     → Services/                 │
│                                                                 │
│  ¿Es una imagen/icono/fuente?       → Assets/                   │
│                                                                 │
│  ¿Convierte datos para XAML?        → Converters/               │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🔗 Siguiente documento

→ `03-flujo-trabajo.md` - Paso a paso: cómo añadir una funcionalidad completa

