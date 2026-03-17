# Guía de Documentación de Código

> **Nivel:** Todos  
> **Objetivo:** Estándares para documentar código en Ataena

---

## 📋 Índice

1. [¿Por qué documentar?](#-por-qué-documentar)
2. [XML Documentation Comments](#-xml-documentation-comments)
3. [Qué documentar](#-qué-documentar)
4. [Ejemplos por tipo de archivo](#-ejemplos-por-tipo-de-archivo)
5. [Regiones (#region)](#-regiones-region)
6. [Comentarios en línea](#-comentarios-en-línea)
7. [Buenas prácticas](#-buenas-prácticas)
8. [Errores comunes](#-errores-comunes)

---

## 🎯 ¿Por qué documentar?

| Beneficio | Descripción |
|-----------|-------------|
| **IntelliSense** | Muestra descripción al pasar el cursor |
| **Onboarding** | Nuevos desarrolladores entienden el código rápido |
| **Mantenibilidad** | Facilita cambios futuros |
| **Auto-documentación** | Genera HTML/PDF automáticamente |

---

## 📝 XML Documentation Comments

En C#, la documentación se hace con comentarios que empiezan con `///`.

### Estructura básica

```csharp
/// <summary>
/// Descripción breve de qué hace.
/// </summary>
/// <remarks>
/// Información adicional, detalles de implementación, etc.
/// </remarks>
/// <param name="parametro">Descripción del parámetro.</param>
/// <returns>Qué retorna el método.</returns>
/// <exception cref="Exception">Cuándo lanza esta excepción.</exception>
/// <example>
/// Ejemplo de uso:
/// <code>
/// var resultado = MiMetodo("valor");
/// </code>
/// </example>
/// <seealso cref="OtraClase"/>
```

### Tags más usados

| Tag | Uso |
|-----|-----|
| `<summary>` | Descripción principal (obligatorio) |
| `<remarks>` | Información adicional |
| `<param name="">` | Descripción de parámetro |
| `<returns>` | Qué retorna el método |
| `<exception cref="">` | Excepciones que puede lanzar |
| `<see cref="">` | Referencia a otro elemento |
| `<seealso cref="">` | "Ver también" |
| `<example>` | Ejemplo de uso |
| `<code>` | Bloque de código |
| `<value>` | Descripción de propiedad |

---

## 📂 Qué documentar

### ✅ Siempre documentar

- Clases públicas
- Métodos públicos y protegidos
- Propiedades públicas
- Enums y sus valores
- Interfaces
- Eventos

### ⚠️ Opcional (pero recomendado)

- Campos privados complejos
- Métodos privados con lógica compleja
- Constantes con significado no obvio

### ❌ No documentar

- Getters/setters triviales sin lógica
- Código obvio (ej: `/// <summary>Gets the Id</summary>` para `Id`)
- Implementaciones de interface obvias

---

## 📁 Ejemplos por tipo de archivo

### Modelos (Models/)

```csharp
namespace Ataena.Models;

/// <summary>
/// Entidad que representa a un cliente del estudio.
/// </summary>
/// <remarks>
/// Tabla principal del CRM. Soporta soft delete mediante
/// el campo <see cref="Activo"/>.
/// </remarks>
public class Cliente
{
    #region Identificación

    /// <summary>
    /// Identificador único (clave primaria).
    /// </summary>
    public int Id { get; set; }

    #endregion

    #region Datos Personales

    /// <summary>
    /// Nombre del cliente. Campo obligatorio.
    /// </summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Alergias conocidas del cliente.
    /// Crítico para seguridad con tintas.
    /// </summary>
    public string? Alergias { get; set; }

    #endregion

    #region Propiedades Calculadas

    /// <summary>
    /// Nombre completo (Nombre + Apellidos).
    /// </summary>
    public string NombreCompleto => $"{Nombre} {Apellidos}";

    #endregion
}
```

### ViewModels (ViewModels/)

```csharp
namespace Ataena.ViewModels;

/// <summary>
/// ViewModel para la gestión de clientes.
/// Implementa operaciones CRUD y búsqueda.
/// </summary>
public partial class ClientesViewModel : ViewModelBase
{
    #region Campos Privados

    /// <summary>
    /// Contexto de base de datos.
    /// </summary>
    private readonly AtaenaDbContext _db = new();

    #endregion

    #region Propiedades Observables

    /// <summary>
    /// Lista de clientes mostrados en la UI.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<Cliente> _clientes = new();

    #endregion

    #region Comandos

    /// <summary>
    /// Guarda el cliente actual (crear o actualizar).
    /// </summary>
    /// <remarks>
    /// Valida nombre y teléfono antes de guardar.
    /// El teléfono debe ser único.
    /// </remarks>
    [RelayCommand]
    private async Task GuardarCliente()
    {
        // Implementación...
    }

    #endregion
}
```

### Enums

```csharp
namespace Ataena.Models;

/// <summary>
/// Estado del ciclo de vida de una cita.
/// </summary>
public enum EstadoCita
{
    /// <summary>
    /// Cita creada, pendiente de confirmación.
    /// </summary>
    Pendiente = 0,

    /// <summary>
    /// Cita confirmada por el cliente.
    /// </summary>
    Confirmada = 1,

    /// <summary>
    /// El cliente no se presentó.
    /// </summary>
    NoShow = 5
}
```

### Conversores (Converters/)

```csharp
namespace Ataena.Converters;

/// <summary>
/// Convierte <see cref="EstadoCita"/> a color para la UI.
/// </summary>
/// <remarks>
/// Colores:
/// - Pendiente: Naranja
/// - Confirmada: Verde
/// - Cancelada: Rojo
/// </remarks>
public class EstadoCitaToColorConverter : IValueConverter
{
    /// <summary>
    /// Instancia estática para uso en XAML.
    /// </summary>
    public static readonly EstadoCitaToColorConverter Instance = new();

    /// <summary>
    /// Convierte estado a color.
    /// </summary>
    /// <param name="value">Estado de la cita.</param>
    /// <param name="targetType">Tipo destino (Brush).</param>
    /// <param name="parameter">No usado.</param>
    /// <param name="culture">Cultura actual.</param>
    /// <returns>SolidColorBrush del color correspondiente.</returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // Implementación...
    }
}
```

### Code-behind (Views/*.axaml.cs)

```csharp
namespace Ataena.Views;

/// <summary>
/// Vista principal del Dashboard.
/// </summary>
/// <remarks>
/// Muestra resumen del día: citas, estadísticas y alertas.
/// </remarks>
public partial class DashboardView : UserControl
{
    /// <summary>
    /// Inicializa el componente.
    /// </summary>
    public DashboardView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Carga datos al mostrar la vista.
    /// </summary>
    private async void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is DashboardViewModel vm)
        {
            await vm.CargarDatosCommand.ExecuteAsync(null);
        }
    }
}
```

---

## 🗂️ Regiones (#region)

Usa regiones para organizar código en secciones lógicas:

```csharp
public class MiViewModel
{
    #region Campos Privados
    // ...
    #endregion

    #region Propiedades
    // ...
    #endregion

    #region Comandos
    // ...
    #endregion

    #region Métodos Privados
    // ...
    #endregion
}
```

### Regiones estándar para ViewModels

1. `Campos Privados`
2. `Propiedades - [Categoría]` (ej: "Propiedades - Lista y Selección")
3. `Comandos - [Categoría]`
4. `Métodos Privados`

### Regiones estándar para Modelos

1. `Identificación`
2. `Datos Personales` / `Datos Principales`
3. `Estado y Configuración`
4. `Navegación (Relaciones)`
5. `Propiedades Calculadas`

---

## 💬 Comentarios en línea

Para explicar lógica específica dentro de métodos:

```csharp
private async Task CargarCitasHoy()
{
    var hoy = DateTime.Today;
    var citas = await _db.Citas
        .Include(c => c.Cliente)
        .Where(c => c.Fecha.Date == hoy)
        .ToListAsync();

    // Ordenar en memoria: SQLite no soporta OrderBy con TimeSpan
    var citasOrdenadas = citas.OrderBy(c => c.HoraInicio).ToList();

    CitasHoy = new ObservableCollection<Cita>(citasOrdenadas);
}
```

### Cuándo usar comentarios en línea

- Explicar "por qué", no "qué"
- Workarounds o limitaciones técnicas
- Algoritmos complejos
- Decisiones de diseño no obvias

---

## ✅ Buenas prácticas

### 1. Sé conciso pero completo

```csharp
// ❌ Demasiado corto
/// <summary>Cliente.</summary>

// ❌ Demasiado largo
/// <summary>
/// Esta clase representa la entidad de cliente que se utiliza
/// en todo el sistema para almacenar información sobre los
/// clientes del estudio de tatuajes incluyendo sus datos
/// personales y preferencias...
/// </summary>

// ✅ Justo
/// <summary>
/// Entidad que representa a un cliente del estudio.
/// Contiene datos personales, de contacto y preferencias.
/// </summary>
```

### 2. Usa `<remarks>` para detalles extra

```csharp
/// <summary>
/// Elimina el cliente seleccionado.
/// </summary>
/// <remarks>
/// Implementa soft delete: solo marca Activo = false.
/// Los datos se conservan para histórico.
/// </remarks>
```

### 3. Documenta casos especiales

```csharp
/// <summary>
/// Edad calculada del cliente.
/// </summary>
/// <value>
/// Edad en años, o null si no hay fecha de nacimiento.
/// </value>
public int? Edad => ...
```

### 4. Referencias cruzadas con `<see cref="">`

```csharp
/// <summary>
/// Convierte <see cref="EstadoCita"/> a color.
/// </summary>
/// <seealso cref="EstadoCita"/>
```

---

## ❌ Errores comunes

### 1. Documentación que no añade valor

```csharp
// ❌ Obvio, no añade nada
/// <summary>
/// Gets or sets the Id.
/// </summary>
public int Id { get; set; }

// ✅ Mejor: añade contexto
/// <summary>
/// Identificador único del cliente (clave primaria).
/// </summary>
public int Id { get; set; }
```

### 2. Documentación desactualizada

```csharp
// ❌ El código cambió pero la doc no
/// <summary>
/// Envía email al cliente.  // Ya no envía email!
/// </summary>
public void GuardarCliente() { ... }
```

### 3. Copiar/pegar sin adaptar

```csharp
// ❌ Copió de otra clase
/// <summary>
/// Guarda el cliente.  // Pero esto es para Citas!
/// </summary>
public void GuardarCita() { ... }
```

---

## 🛠️ Generar documentación HTML

Puedes generar documentación HTML automática:

```xml
<!-- En .csproj -->
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
</PropertyGroup>
```

Esto genera un archivo `.xml` con la documentación que herramientas como DocFX pueden convertir a HTML.

---

## 📝 Checklist de documentación

Antes de hacer commit, verifica:

- [ ] Clases nuevas tienen `<summary>`
- [ ] Métodos públicos documentados
- [ ] Parámetros tienen `<param>`
- [ ] Propiedades complejas documentadas
- [ ] Enums y valores documentados
- [ ] Regiones organizan el código
- [ ] Sin documentación obvia/redundante
- [ ] Documentación actualizada con cambios

---

## 📚 Recursos

- [XML Documentation Comments (Microsoft)](https://docs.microsoft.com/dotnet/csharp/language-reference/xmldoc/)
- [Recommended XML tags (Microsoft)](https://docs.microsoft.com/dotnet/csharp/language-reference/xmldoc/recommended-tags)

---

> **Recuerda:** Una buena documentación es aquella que explica el "por qué", no solo el "qué". El código ya dice qué hace; la documentación debe explicar por qué lo hace así.

