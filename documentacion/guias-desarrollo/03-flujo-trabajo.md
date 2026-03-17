# Flujo de Trabajo

> **Nivel:** Principiante  
> **Objetivo:** Aprender el proceso paso a paso para añadir funcionalidades

---

## 📋 Índice

1. [El Ciclo de Desarrollo](#-el-ciclo-de-desarrollo)
2. [Ejemplo Práctico: Añadir Clientes](#-ejemplo-práctico-añadir-clientes)
3. [Paso 1: Crear el Modelo](#paso-1-crear-el-modelo)
4. [Paso 2: Añadir al DbContext](#paso-2-añadir-al-dbcontext)
5. [Paso 3: Crear la Migración](#paso-3-crear-la-migración)
6. [Paso 4: Crear el ViewModel](#paso-4-crear-el-viewmodel)
7. [Paso 5: Crear la View](#paso-5-crear-la-view)
8. [Paso 6: Conectar y Probar](#paso-6-conectar-y-probar)
9. [Resumen del Flujo](#-resumen-del-flujo)

---

## 🔄 El Ciclo de Desarrollo

Siempre que añadas algo nuevo, sigue este orden:

```
┌─────────────────────────────────────────────────────────────────┐
│                    CICLO DE DESARROLLO                          │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│    1. MODEL          ¿Qué datos necesito guardar?               │
│        │                                                        │
│        ▼                                                        │
│    2. DBCONTEXT      Añadir la tabla a la base de datos         │
│        │                                                        │
│        ▼                                                        │
│    3. MIGRACIÓN      Crear/aplicar cambios en la BD             │
│        │                                                        │
│        ▼                                                        │
│    4. VIEWMODEL      ¿Qué lógica necesita la pantalla?          │
│        │                                                        │
│        ▼                                                        │
│    5. VIEW           ¿Cómo se ve la pantalla?                   │
│        │                                                        │
│        ▼                                                        │
│    6. PROBAR         Ejecutar y verificar                       │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## 📝 Ejemplo Práctico: Añadir Clientes

Vamos a crear la funcionalidad de **gestión de clientes** paso a paso.

**Objetivo:** 
- Ver lista de clientes
- Añadir nuevo cliente
- Editar cliente existente
- Eliminar cliente

---

## Paso 1: Crear el Modelo

### ¿Qué datos necesito?

Piensa qué información quieres guardar de un cliente.

### Crear el archivo:

```
📁 Models/
   └── 📄 Cliente.cs   ← CREAR ESTE ARCHIVO
```

### Código:

```csharp
// Models/Cliente.cs
using System;
using System.Collections.Generic;

namespace Ataena.Models;

public class Cliente
{
    // Clave primaria (obligatoria)
    public int Id { get; set; }
    
    // Datos básicos
    public string Nombre { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string? Email { get; set; }  // ? = puede ser null
    
    // Datos adicionales
    public DateTime FechaRegistro { get; set; } = DateTime.Now;
    public DateTime? FechaNacimiento { get; set; }
    public string? Alergias { get; set; }
    public string? Notas { get; set; }
    public bool EsVip { get; set; }
    
    // Relaciones (las veremos después)
    public List<Cita> Citas { get; set; } = new();
    public List<Trabajo> Trabajos { get; set; } = new();
    
    // Propiedad calculada (NO se guarda en BD)
    public string NombreCompleto => $"{Nombre} {Apellidos}";
}
```

### Consejos:

- `string.Empty` es mejor que `""` para inicializar strings
- Usa `?` para campos opcionales (pueden ser null)
- `Id` siempre debe existir y EF lo auto-incrementa
- Las propiedades calculadas (`=>`) no se guardan en la BD

---

## Paso 2: Añadir al DbContext

### Abrir el archivo:

```
📁 Data/
   └── 📄 AtaenaDbContext.cs   ← MODIFICAR ESTE ARCHIVO
```

### Añadir el DbSet:

```csharp
// Data/AtaenaDbContext.cs
using Microsoft.EntityFrameworkCore;
using Ataena.Models;

namespace Ataena.Data;

public class AtaenaDbContext : DbContext
{
    // AÑADIR ESTA LÍNEA:
    public DbSet<Cliente> Clientes => Set<Cliente>();
    
    // Configuración de la conexión
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

### ¿Por qué `DbSet<Cliente>`?

- `DbSet` representa una tabla en la base de datos
- Cada `DbSet` que añadas = una tabla nueva
- El nombre (`Clientes`) será el nombre de la tabla

---

## Paso 3: Crear la Migración

### ¿Qué es una migración?

Es un archivo que describe los cambios en la estructura de la BD. EF Core los genera automáticamente.

### Ejecutar en terminal:

```powershell
# Navegar al proyecto
cd Ataena

# Crear la migración
dotnet ef migrations add CrearTablaClientes

# Aplicar la migración (crea/actualiza la BD)
dotnet ef database update
```

### ¿Qué pasó?

```
📁 Data/
   └── 📁 Migrations/
       ├── 📄 20251205120000_CrearTablaClientes.cs      ← NUEVO
       └── 📄 AtaenaDbContextModelSnapshot.cs        ← NUEVO
```

### Si algo sale mal:

```powershell
# Eliminar última migración (si no se ha aplicado)
dotnet ef migrations remove

# Volver a intentar
dotnet ef migrations add CrearTablaClientes
```

---

## Paso 4: Crear el ViewModel

### Crear el archivo:

```
📁 ViewModels/
   └── 📄 ClientesViewModel.cs   ← CREAR ESTE ARCHIVO
```

### Código:

```csharp
// ViewModels/ClientesViewModel.cs
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Ataena.Data;
using Ataena.Models;

namespace Ataena.ViewModels;

public partial class ClientesViewModel : ViewModelBase
{
    private readonly AtaenaDbContext _db = new();

    // ═══════════════════════════════════════════════════════════
    // PROPIEDADES (datos que la View puede mostrar)
    // ═══════════════════════════════════════════════════════════
    
    [ObservableProperty]
    private ObservableCollection<Cliente> _clientes = new();

    [ObservableProperty]
    private Cliente? _clienteSeleccionado;

    [ObservableProperty]
    private string _busqueda = string.Empty;

    // Propiedades para el formulario de nuevo/editar cliente
    [ObservableProperty]
    private string _nombre = string.Empty;

    [ObservableProperty]
    private string _apellidos = string.Empty;

    [ObservableProperty]
    private string _telefono = string.Empty;

    [ObservableProperty]
    private string _email = string.Empty;

    // ═══════════════════════════════════════════════════════════
    // COMANDOS (acciones que la View puede ejecutar)
    // ═══════════════════════════════════════════════════════════

    [RelayCommand]
    private async Task CargarClientes()
    {
        var lista = await _db.Clientes
            .OrderBy(c => c.Nombre)
            .ToListAsync();
        
        Clientes = new ObservableCollection<Cliente>(lista);
    }

    [RelayCommand]
    private async Task Buscar()
    {
        if (string.IsNullOrWhiteSpace(Busqueda))
        {
            await CargarClientes();
            return;
        }

        var lista = await _db.Clientes
            .Where(c => c.Nombre.Contains(Busqueda) || 
                        c.Apellidos.Contains(Busqueda) ||
                        c.Telefono.Contains(Busqueda))
            .ToListAsync();
        
        Clientes = new ObservableCollection<Cliente>(lista);
    }

    [RelayCommand]
    private async Task GuardarCliente()
    {
        // Validación básica
        if (string.IsNullOrWhiteSpace(Nombre) || string.IsNullOrWhiteSpace(Telefono))
            return;

        var cliente = new Cliente
        {
            Nombre = Nombre,
            Apellidos = Apellidos,
            Telefono = Telefono,
            Email = string.IsNullOrWhiteSpace(Email) ? null : Email
        };

        _db.Clientes.Add(cliente);
        await _db.SaveChangesAsync();

        // Limpiar formulario y recargar lista
        LimpiarFormulario();
        await CargarClientes();
    }

    [RelayCommand]
    private async Task EliminarCliente()
    {
        if (ClienteSeleccionado == null)
            return;

        _db.Clientes.Remove(ClienteSeleccionado);
        await _db.SaveChangesAsync();
        
        await CargarClientes();
    }

    // ═══════════════════════════════════════════════════════════
    // MÉTODOS PRIVADOS (auxiliares)
    // ═══════════════════════════════════════════════════════════

    private void LimpiarFormulario()
    {
        Nombre = string.Empty;
        Apellidos = string.Empty;
        Telefono = string.Empty;
        Email = string.Empty;
    }
}
```

### Explicación de los atributos:

| Atributo | Qué hace |
|----------|----------|
| `[ObservableProperty]` | Crea propiedad que notifica cambios a la View |
| `[RelayCommand]` | Crea comando que la View puede ejecutar |
| `partial class` | Permite que el toolkit genere código adicional |

---

## Paso 5: Crear la View

### Crear el archivo:

```
📁 Views/
   ├── 📄 ClientesView.axaml      ← CREAR ESTE
   └── 📄 ClientesView.axaml.cs   ← CREAR ESTE
```

### Código XAML:

```xml
<!-- Views/ClientesView.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:Ataena.ViewModels"
             x:Class="Ataena.Views.ClientesView"
             x:DataType="vm:ClientesViewModel">

    <Grid RowDefinitions="Auto,*,Auto" Margin="20">
        
        <!-- FILA 0: Barra de búsqueda -->
        <Grid ColumnDefinitions="*,Auto" Margin="0,0,0,10">
            <TextBox Grid.Column="0"
                     Text="{Binding Busqueda}"
                     Watermark="🔍 Buscar cliente..."
                     Margin="0,0,10,0"/>
            <Button Grid.Column="1"
                    Content="Buscar"
                    Command="{Binding BuscarCommand}"/>
        </Grid>

        <!-- FILA 1: Lista de clientes -->
        <ListBox Grid.Row="1"
                 ItemsSource="{Binding Clientes}"
                 SelectedItem="{Binding ClienteSeleccionado}"
                 Margin="0,0,0,10">
            <ListBox.ItemTemplate>
                <DataTemplate>
                    <StackPanel Margin="5">
                        <TextBlock Text="{Binding NombreCompleto}" 
                                   FontWeight="Bold"/>
                        <TextBlock Text="{Binding Telefono}" 
                                   Opacity="0.7"/>
                    </StackPanel>
                </DataTemplate>
            </ListBox.ItemTemplate>
        </ListBox>

        <!-- FILA 2: Formulario y botones -->
        <StackPanel Grid.Row="2" Spacing="10">
            <Grid ColumnDefinitions="*,*" RowDefinitions="Auto,Auto,Auto">
                <TextBox Grid.Row="0" Grid.Column="0"
                         Text="{Binding Nombre}"
                         Watermark="Nombre"
                         Margin="0,0,5,5"/>
                <TextBox Grid.Row="0" Grid.Column="1"
                         Text="{Binding Apellidos}"
                         Watermark="Apellidos"
                         Margin="5,0,0,5"/>
                <TextBox Grid.Row="1" Grid.Column="0"
                         Text="{Binding Telefono}"
                         Watermark="Teléfono"
                         Margin="0,0,5,5"/>
                <TextBox Grid.Row="1" Grid.Column="1"
                         Text="{Binding Email}"
                         Watermark="Email (opcional)"
                         Margin="5,0,0,5"/>
            </Grid>
            
            <StackPanel Orientation="Horizontal" Spacing="10">
                <Button Content="💾 Guardar"
                        Command="{Binding GuardarClienteCommand}"/>
                <Button Content="🗑️ Eliminar"
                        Command="{Binding EliminarClienteCommand}"/>
                <Button Content="🔄 Recargar"
                        Command="{Binding CargarClientesCommand}"/>
            </StackPanel>
        </StackPanel>
        
    </Grid>

</UserControl>
```

### Código behind:

```csharp
// Views/ClientesView.axaml.cs
using Avalonia.Controls;

namespace Ataena.Views;

public partial class ClientesView : UserControl
{
    public ClientesView()
    {
        InitializeComponent();
    }
}
```

---

## Paso 6: Conectar y Probar

### Añadir la View a la ventana principal:

Modifica `MainWindow.axaml` para incluir la nueva View:

```xml
<!-- Views/MainWindow.axaml -->
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:views="using:Ataena.Views"
        xmlns:vm="using:Ataena.ViewModels"
        x:Class="Ataena.Views.MainWindow"
        Title="Ataena CRM">

    <!-- Incluir la vista de clientes -->
    <views:ClientesView DataContext="{Binding ClientesVM}"/>

</Window>
```

### Modificar MainWindowViewModel:

```csharp
// ViewModels/MainWindowViewModel.cs
namespace Ataena.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public ClientesViewModel ClientesVM { get; } = new();
}
```

### Probar:

```powershell
dotnet run --project Ataena
```

---

## 📊 Resumen del Flujo

```
┌─────────────────────────────────────────────────────────────────┐
│                    CHECKLIST NUEVA FUNCIONALIDAD                │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  □ 1. Crear Model en Models/                                    │
│       └── Define los datos que quieres guardar                  │
│                                                                 │
│  □ 2. Añadir DbSet en Data/AtaenaDbContext.cs                │
│       └── public DbSet<MiModelo> MisModelos => Set<MiModelo>(); │
│                                                                 │
│  □ 3. Crear migración                                           │
│       └── dotnet ef migrations add NombreDescriptivo            │
│       └── dotnet ef database update                             │
│                                                                 │
│  □ 4. Crear ViewModel en ViewModels/                            │
│       └── Propiedades con [ObservableProperty]                  │
│       └── Comandos con [RelayCommand]                           │
│                                                                 │
│  □ 5. Crear View en Views/                                      │
│       └── Archivo .axaml (diseño)                               │
│       └── Archivo .axaml.cs (código behind mínimo)              │
│                                                                 │
│  □ 6. Conectar View con ViewModel                               │
│       └── DataContext en XAML o en código                       │
│                                                                 │
│  □ 7. Probar                                                    │
│       └── dotnet run --project Ataena                        │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🔗 Siguiente documento

→ `04-base-de-datos.md` - Guía detallada de Entity Framework Core

