# Introducción al Stack Tecnológico

> **Nivel:** Principiante  
> **Objetivo:** Entender qué tecnologías usamos y para qué sirve cada una

---

## 📋 Índice

1. [Visión General](#-visión-general)
2. [.NET y C#](#-net-y-c)
3. [Avalonia UI](#-avalonia-ui)
4. [Patrón MVVM](#-patrón-mvvm)
5. [Entity Framework Core](#-entity-framework-core)
6. [SQLite](#-sqlite)
7. [FluentAvalonia](#-fluentavalonia)
8. [CommunityToolkit.Mvvm](#-communitytoolkitmvvm)

---

## 🎯 Visión General

Este proyecto usa el siguiente stack:

```
┌─────────────────────────────────────────────────────────────────┐
│                         STACK                                   │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   INTERFAZ DE USUARIO                                           │
│   ├── Avalonia UI        → Framework para crear ventanas        │
│   ├── FluentAvalonia     → Tema visual Windows 11               │
│   └── XAML               → Lenguaje para diseñar pantallas      │
│                                                                 │
│   LÓGICA DE LA APLICACIÓN                                       │
│   ├── C# 12              → Lenguaje de programación             │
│   ├── .NET 9             → Plataforma de ejecución              │
│   └── MVVM               → Patrón de arquitectura               │
│                                                                 │
│   DATOS                                                         │
│   ├── Entity Framework   → ORM (conecta C# con la BD)           │
│   └── SQLite             → Base de datos local                  │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🟣 .NET y C#

### ¿Qué es .NET?

**.NET** es una plataforma de desarrollo creada por Microsoft. Permite crear aplicaciones de todo tipo: web, móvil, escritorio, juegos, etc.

### ¿Qué es C#?

**C#** (se pronuncia "C sharp") es el lenguaje de programación principal de .NET. Es similar a Java o TypeScript.

### Ejemplo básico de C#:

```csharp
// Definir una clase
public class Cliente
{
    // Propiedades (datos)
    public string Nombre { get; set; }
    public string Telefono { get; set; }
    
    // Método (acción)
    public void Saludar()
    {
        Console.WriteLine($"Hola, soy {Nombre}");
    }
}

// Usar la clase
var cliente = new Cliente();
cliente.Nombre = "María";
cliente.Saludar(); // Imprime: "Hola, soy María"
```

---

## 🖼️ Avalonia UI

### ¿Qué es?

**Avalonia** es un framework para crear aplicaciones de escritorio con interfaz gráfica. Es como WPF pero multiplataforma (Windows, Mac, Linux).

### ¿Por qué Avalonia y no WPF?

| Característica | WPF | Avalonia |
|----------------|-----|----------|
| Plataformas | Solo Windows | Windows, Mac, Linux |
| IDE necesario | Visual Studio | Cualquiera (Cursor, VS Code) |
| Modernidad | 2006 | Activo en 2025 |
| Open Source | No | Sí |

### ¿Cómo funciona?

Avalonia usa **XAML** para definir la interfaz y **C#** para la lógica:

```
┌──────────────────┐     ┌──────────────────┐
│   MainWindow     │     │   MainWindow     │
│     .axaml       │ ←── │   .axaml.cs      │
│                  │     │                  │
│   (Diseño UI)    │     │   (Código C#)    │
│   Botones,       │     │   Qué pasa al    │
│   textos, etc.   │     │   hacer click    │
└──────────────────┘     └──────────────────┘
```

### Ejemplo de XAML:

```xml
<!-- Esto es XAML - define cómo se ve la pantalla -->
<Window Title="Mi Aplicación">
    <StackPanel>
        <TextBlock Text="Hola Mundo"/>
        <Button Content="Pulsa aquí" Click="OnButtonClick"/>
    </StackPanel>
</Window>
```

---

## 📐 Patrón MVVM

### ¿Qué es MVVM?

**MVVM** (Model-View-ViewModel) es una forma de organizar el código. Separa:

- **Model:** Los datos (ej: Cliente, Cita)
- **View:** La interfaz (XAML)
- **ViewModel:** La lógica que conecta ambos

### ¿Por qué usarlo?

```
SIN MVVM (código mezclado):
┌─────────────────────────────────────┐
│ MainWindow.xaml.cs                  │
│ ├── Código de interfaz              │
│ ├── Código de base de datos         │
│ ├── Lógica de negocio               │
│ └── TODO JUNTO = CAOS 😱            │
└─────────────────────────────────────┘

CON MVVM (código separado):
┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐
│     MODEL       │  │   VIEWMODEL     │  │      VIEW       │
│                 │  │                 │  │                 │
│  Cliente.cs     │  │ ClientesVM.cs   │  │ Clientes.axaml  │
│  - Nombre       │←─│ - Lista         │←─│ - ListBox       │
│  - Telefono     │  │ - Guardar()     │  │ - Botón         │
│                 │  │ - Buscar()      │  │                 │
└─────────────────┘  └─────────────────┘  └─────────────────┘
      DATOS             LÓGICA              INTERFAZ
```

### Flujo de datos:

```
Usuario hace click en "Guardar"
         │
         ▼
    VIEW (XAML)
    detecta el click
         │
         ▼
    VIEWMODEL
    ejecuta comando GuardarCommand
         │
         ▼
    MODEL + DATABASE
    guarda el cliente en SQLite
         │
         ▼
    VIEWMODEL
    actualiza la lista
         │
         ▼
    VIEW (XAML)
    muestra la lista actualizada automáticamente
```

---

## 🗄️ Entity Framework Core

### ¿Qué es?

**Entity Framework Core (EF Core)** es un **ORM** (Object-Relational Mapper). Traduce entre objetos C# y tablas de base de datos.

### ¿Por qué usarlo?

Sin EF Core, tendrías que escribir SQL manualmente:

```csharp
// SIN Entity Framework (SQL manual) 😫
var sql = "INSERT INTO Clientes (Nombre, Telefono) VALUES (@nombre, @tel)";
var command = new SqliteCommand(sql, connection);
command.Parameters.AddWithValue("@nombre", cliente.Nombre);
command.Parameters.AddWithValue("@tel", cliente.Telefono);
command.ExecuteNonQuery();
```

Con EF Core:

```csharp
// CON Entity Framework 😊
_db.Clientes.Add(cliente);
await _db.SaveChangesAsync();
```

### Conceptos clave:

| Concepto | Qué es | Ejemplo |
|----------|--------|---------|
| **DbContext** | Conexión a la BD | `AtaenaDbContext` |
| **DbSet** | Una tabla | `DbSet<Cliente> Clientes` |
| **Entidad** | Una fila de la tabla | `Cliente` |
| **Migración** | Cambio en la estructura | Añadir campo `Email` |

### Ejemplo de consulta:

```csharp
// Obtener todos los clientes
var clientes = await _db.Clientes.ToListAsync();

// Buscar por nombre
var maria = await _db.Clientes
    .Where(c => c.Nombre.Contains("María"))
    .ToListAsync();

// Obtener cliente con sus citas
var cliente = await _db.Clientes
    .Include(c => c.Citas)
    .FirstOrDefaultAsync(c => c.Id == 5);
```

---

## 💾 SQLite

### ¿Qué es?

**SQLite** es una base de datos que se guarda en un único archivo `.db`. No necesita instalar nada adicional.

### ¿Por qué SQLite?

| Característica | MySQL/PostgreSQL | SQLite |
|----------------|------------------|--------|
| Instalación | Requiere servidor | Nada, solo un archivo |
| Configuración | Compleja | Ninguna |
| Rendimiento | Alto (millones de registros) | Alto (miles de registros) |
| Distribución | Complicada | Copiar el archivo |
| Para este proyecto | Excesivo | **Perfecto** ✅ |

### Ubicación del archivo:

```
C:\Users\{Tu Usuario}\AppData\Local\Ataena\data.db
```

### Ver el contenido:

Puedes abrir el archivo `.db` con herramientas como:
- DB Browser for SQLite (gratis)
- DBeaver (gratis)
- Extensión SQLite en VS Code/Cursor

---

## 🎨 FluentAvalonia

### ¿Qué es?

**FluentAvalonia** es una librería que hace que Avalonia se vea como Windows 11.

### Sin FluentAvalonia vs Con FluentAvalonia:

```
SIN FluentAvalonia:          CON FluentAvalonia:
┌─────────────────────┐      ┌─────────────────────┐
│ [Botón plano]       │      │ [ Botón moderno  ]  │
│                     │      │     con sombra      │
│ Lista básica        │      │ Lista con hover     │
│ - Item 1            │      │ ▸ Item 1            │
│ - Item 2            │      │ ▸ Item 2            │
│                     │      │                     │
│ Aspecto Windows XP  │      │ Aspecto Windows 11  │
└─────────────────────┘      └─────────────────────┘
```

### Componentes que incluye:

- NavigationView (menú lateral)
- CommandBar (barra de herramientas)
- InfoBar (notificaciones)
- ContentDialog (popups)
- Iconos Fluent
- Tema claro/oscuro automático

---

## 🧰 CommunityToolkit.Mvvm

### ¿Qué es?

Es una librería de Microsoft que simplifica MVVM. Reduce el código repetitivo.

### Sin CommunityToolkit vs Con CommunityToolkit:

```csharp
// SIN CommunityToolkit (mucho código) 😫
public class ClientesViewModel : INotifyPropertyChanged
{
    private string _nombre;
    public string Nombre
    {
        get => _nombre;
        set
        {
            if (_nombre != value)
            {
                _nombre = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Nombre)));
            }
        }
    }
    
    public event PropertyChangedEventHandler PropertyChanged;
    
    private ICommand _guardarCommand;
    public ICommand GuardarCommand => _guardarCommand ??= new RelayCommand(Guardar);
    
    private void Guardar() { /* ... */ }
}

// CON CommunityToolkit (poco código) 😊
public partial class ClientesViewModel : ObservableObject
{
    [ObservableProperty]
    private string _nombre;
    
    [RelayCommand]
    private void Guardar() { /* ... */ }
}
```

### Atributos principales:

| Atributo | Qué hace |
|----------|----------|
| `[ObservableProperty]` | Crea propiedad con notificación automática |
| `[RelayCommand]` | Crea comando a partir de un método |
| `ObservableObject` | Clase base con INotifyPropertyChanged |

---

## 📚 Resumen

| Tecnología | Para qué la usamos |
|------------|-------------------|
| **.NET 9 / C#** | Lenguaje y plataforma base |
| **Avalonia** | Crear ventanas e interfaces |
| **XAML** | Diseñar cómo se ven las pantallas |
| **MVVM** | Organizar el código limpiamente |
| **CommunityToolkit** | Escribir menos código MVVM |
| **FluentAvalonia** | Que se vea bonito (Windows 11) |
| **Entity Framework** | Conectar con la base de datos fácilmente |
| **SQLite** | Guardar los datos en un archivo |

---

## 🔗 Siguiente documento

→ `02-estructura-proyecto.md` - Qué hay en cada carpeta del proyecto

