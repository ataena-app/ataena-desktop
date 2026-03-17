# Crear Pantallas con XAML

> **Nivel:** Principiante a Intermedio  
> **Objetivo:** Aprender a diseñar interfaces con Avalonia y XAML

---

## 📋 Índice

1. [¿Qué es XAML?](#-qué-es-xaml)
2. [Estructura de una View](#-estructura-de-una-view)
3. [Layouts (Contenedores)](#-layouts-contenedores)
4. [Controles Básicos](#-controles-básicos)
5. [Data Binding](#-data-binding)
6. [Comandos](#-comandos)
7. [Listas y Templates](#-listas-y-templates)
8. [Estilos](#-estilos)
9. [Ejemplo Completo](#-ejemplo-completo)

---

## 📝 ¿Qué es XAML?

**XAML** (eXtensible Application Markup Language) es un lenguaje basado en XML para definir interfaces de usuario.

### Comparación con HTML

```xml
<!-- HTML -->
<div class="container">
    <h1>Título</h1>
    <button onclick="guardar()">Guardar</button>
</div>

<!-- XAML (Avalonia) -->
<StackPanel>
    <TextBlock Text="Título" FontSize="24"/>
    <Button Content="Guardar" Command="{Binding GuardarCommand}"/>
</StackPanel>
```

### Sintaxis básica

```xml
<!-- Elemento con atributos -->
<Button Content="Click" Width="100" Height="40"/>

<!-- Elemento con contenido -->
<Button>
    <TextBlock Text="Click aquí"/>
</Button>

<!-- Elemento con propiedades complejas -->
<Button Content="Click">
    <Button.Background>
        <SolidColorBrush Color="Blue"/>
    </Button.Background>
</Button>
```

---

## 🏗️ Estructura de una View

### Archivo .axaml (diseño)

```xml
<!-- Views/ClientesView.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:Ataena.ViewModels"
             x:Class="Ataena.Views.ClientesView"
             x:DataType="vm:ClientesViewModel">
    
    <!-- Contenido de la vista -->
    <StackPanel>
        <TextBlock Text="Hola"/>
    </StackPanel>
    
</UserControl>
```

### Explicación de los namespaces

```xml
xmlns="https://github.com/avaloniaui"     <!-- Controles de Avalonia -->
xmlns:x="http://schemas.microsoft.com/..." <!-- Funciones XAML -->
xmlns:vm="using:Ataena.ViewModels"      <!-- Tus ViewModels -->
x:Class="Ataena.Views.ClientesView"     <!-- Clase C# asociada -->
x:DataType="vm:ClientesViewModel"          <!-- Tipo del DataContext -->
```

### Archivo .axaml.cs (código behind)

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

> **Nota:** En MVVM, el código behind debe ser mínimo. Toda la lógica va en el ViewModel.

---

## 📦 Layouts (Contenedores)

### StackPanel

Apila elementos vertical u horizontalmente.

```xml
<!-- Vertical (por defecto) -->
<StackPanel>
    <TextBlock Text="Uno"/>
    <TextBlock Text="Dos"/>
    <TextBlock Text="Tres"/>
</StackPanel>

<!-- Horizontal -->
<StackPanel Orientation="Horizontal" Spacing="10">
    <Button Content="A"/>
    <Button Content="B"/>
    <Button Content="C"/>
</StackPanel>
```

```
Vertical:        Horizontal:
┌─────────┐      ┌───┬───┬───┐
│  Uno    │      │ A │ B │ C │
├─────────┤      └───┴───┴───┘
│  Dos    │
├─────────┤
│  Tres   │
└─────────┘
```

### Grid

Organiza en filas y columnas.

```xml
<Grid RowDefinitions="Auto,*,Auto" ColumnDefinitions="*,*">
    
    <!-- Fila 0, ocupa 2 columnas -->
    <TextBlock Grid.Row="0" Grid.ColumnSpan="2" Text="Título"/>
    
    <!-- Fila 1, Columna 0 -->
    <ListBox Grid.Row="1" Grid.Column="0"/>
    
    <!-- Fila 1, Columna 1 -->
    <StackPanel Grid.Row="1" Grid.Column="1"/>
    
    <!-- Fila 2, ocupa 2 columnas -->
    <Button Grid.Row="2" Grid.ColumnSpan="2" Content="Guardar"/>
    
</Grid>
```

```
Definiciones:
- Auto = tamaño del contenido
- *    = espacio restante
- 200  = 200 píxeles fijos
- 2*   = doble de espacio que *

Ejemplo: RowDefinitions="Auto,*,Auto"
┌────────────────────────────┐
│ Fila 0 (Auto) - Título     │  ← Alto según contenido
├──────────────┬─────────────┤
│              │             │
│ Fila 1 (*)   │             │  ← Ocupa espacio restante
│              │             │
├──────────────┴─────────────┤
│ Fila 2 (Auto) - Botón      │  ← Alto según contenido
└────────────────────────────┘
```

### DockPanel

Ancla elementos a los bordes.

```xml
<DockPanel>
    <Menu DockPanel.Dock="Top"/>
    <StatusBar DockPanel.Dock="Bottom"/>
    <TreeView DockPanel.Dock="Left" Width="200"/>
    
    <!-- El último elemento ocupa el resto -->
    <ContentControl/>
</DockPanel>
```

```
┌────────────────────────────┐
│         Menu (Top)         │
├────────┬───────────────────┤
│        │                   │
│ Tree   │    Contenido      │
│ (Left) │    (resto)        │
│        │                   │
├────────┴───────────────────┤
│      StatusBar (Bottom)    │
└────────────────────────────┘
```

### WrapPanel

Elementos que saltan de línea.

```xml
<WrapPanel>
    <Button Content="Uno"/>
    <Button Content="Dos"/>
    <Button Content="Tres"/>
    <Button Content="Cuatro"/>
    <!-- Si no caben, saltan a la siguiente línea -->
</WrapPanel>
```

---

## 🎛️ Controles Básicos

### TextBlock (texto estático)

```xml
<TextBlock Text="Hola mundo"/>
<TextBlock Text="Título" FontSize="24" FontWeight="Bold"/>
<TextBlock Text="Gris" Foreground="Gray" Opacity="0.7"/>
<TextBlock Text="Texto largo que puede ocupar
                 varias líneas" TextWrapping="Wrap"/>
```

### TextBox (entrada de texto)

```xml
<TextBox Text="{Binding Nombre}"/>
<TextBox Watermark="Escribe aquí..." />
<TextBox AcceptsReturn="True" Height="100"/>  <!-- Multilínea -->
<TextBox IsReadOnly="True"/>
<TextBox PasswordChar="*"/>  <!-- Para contraseñas -->
```

### Button

```xml
<Button Content="Click"/>
<Button Content="Guardar" Command="{Binding GuardarCommand}"/>
<Button Content="Eliminar" IsEnabled="{Binding PuedeEliminar}"/>

<!-- Botón con icono y texto -->
<Button>
    <StackPanel Orientation="Horizontal" Spacing="5">
        <TextBlock Text="💾"/>
        <TextBlock Text="Guardar"/>
    </StackPanel>
</Button>
```

### CheckBox

```xml
<CheckBox Content="Acepto los términos" IsChecked="{Binding Acepta}"/>
<CheckBox Content="VIP" IsChecked="{Binding EsVip}"/>
```

### ComboBox (desplegable)

```xml
<!-- Opciones fijas -->
<ComboBox SelectedIndex="0">
    <ComboBoxItem Content="Opción 1"/>
    <ComboBoxItem Content="Opción 2"/>
    <ComboBoxItem Content="Opción 3"/>
</ComboBox>

<!-- Opciones desde datos -->
<ComboBox ItemsSource="{Binding Opciones}"
          SelectedItem="{Binding OpcionSeleccionada}"/>
```

### DatePicker

```xml
<DatePicker SelectedDate="{Binding FechaCita}"/>
```

### ListBox

```xml
<ListBox ItemsSource="{Binding Clientes}"
         SelectedItem="{Binding ClienteSeleccionado}">
    <ListBox.ItemTemplate>
        <DataTemplate>
            <TextBlock Text="{Binding NombreCompleto}"/>
        </DataTemplate>
    </ListBox.ItemTemplate>
</ListBox>
```

### Image

```xml
<Image Source="/Assets/logo.png" Width="100"/>
```

---

## 🔗 Data Binding

### ¿Qué es?

Data Binding conecta la View con el ViewModel. Cuando cambia un dato, la UI se actualiza automáticamente.

```
ViewModel                          View (XAML)
┌─────────────────┐               ┌─────────────────┐
│ Nombre = "Ana"  │ ◄──────────── │ Text="{Binding  │
│                 │    Binding     │       Nombre}"  │
└─────────────────┘               └─────────────────┘
```

### Sintaxis básica

```xml
<!-- Binding simple -->
<TextBlock Text="{Binding Nombre}"/>

<!-- Binding con formato -->
<TextBlock Text="{Binding Precio, StringFormat='€{0:N2}'}"/>

<!-- Binding bidireccional (por defecto en TextBox) -->
<TextBox Text="{Binding Nombre}"/>

<!-- Binding solo lectura -->
<TextBlock Text="{Binding Nombre, Mode=OneWay}"/>
```

### Ejemplo completo

```csharp
// ViewModel
public partial class ClienteViewModel : ObservableObject
{
    [ObservableProperty]
    private string _nombre = string.Empty;
    
    [ObservableProperty]
    private bool _esVip;
    
    [ObservableProperty]
    private decimal _deuda;
}
```

```xml
<!-- View -->
<StackPanel>
    <TextBox Text="{Binding Nombre}"/>
    <CheckBox Content="VIP" IsChecked="{Binding EsVip}"/>
    <TextBlock Text="{Binding Deuda, StringFormat='Debe: €{0:N2}'}"/>
</StackPanel>
```

### Binding a propiedades de objetos

```xml
<!-- Si ClienteSeleccionado es un objeto Cliente -->
<TextBlock Text="{Binding ClienteSeleccionado.Nombre}"/>
<TextBlock Text="{Binding ClienteSeleccionado.Telefono}"/>
```

---

## ⚡ Comandos

### ¿Qué son?

Los comandos conectan acciones de la UI (click, etc.) con métodos del ViewModel.

### En el ViewModel

```csharp
public partial class ClientesViewModel : ViewModelBase
{
    [RelayCommand]
    private async Task Guardar()
    {
        // Este método se ejecuta al hacer click
        await _db.SaveChangesAsync();
    }
    
    [RelayCommand]
    private void Cancelar()
    {
        // Lógica de cancelar
    }
}
```

### En la View

```xml
<!-- El atributo [RelayCommand] crea GuardarCommand automáticamente -->
<Button Content="Guardar" Command="{Binding GuardarCommand}"/>
<Button Content="Cancelar" Command="{Binding CancelarCommand}"/>
```

### Comandos con parámetro

```csharp
// ViewModel
[RelayCommand]
private void Eliminar(Cliente cliente)
{
    _db.Clientes.Remove(cliente);
}
```

```xml
<!-- View -->
<Button Content="Eliminar" 
        Command="{Binding EliminarCommand}"
        CommandParameter="{Binding ClienteSeleccionado}"/>
```

### Comando habilitado/deshabilitado

```csharp
// El botón se deshabilita si no hay cliente seleccionado
[RelayCommand(CanExecute = nameof(PuedeEliminar))]
private void Eliminar()
{
    // ...
}

private bool PuedeEliminar => ClienteSeleccionado != null;
```

---

## 📋 Listas y Templates

### ItemTemplate básico

```xml
<ListBox ItemsSource="{Binding Clientes}">
    <ListBox.ItemTemplate>
        <DataTemplate>
            <!-- Cada item de la lista usa este template -->
            <StackPanel>
                <TextBlock Text="{Binding NombreCompleto}" FontWeight="Bold"/>
                <TextBlock Text="{Binding Telefono}" Opacity="0.7"/>
            </StackPanel>
        </DataTemplate>
    </ListBox.ItemTemplate>
</ListBox>
```

### Template más elaborado

```xml
<ListBox ItemsSource="{Binding Clientes}"
         SelectedItem="{Binding ClienteSeleccionado}">
    <ListBox.ItemTemplate>
        <DataTemplate>
            <Border Padding="10" Margin="5" CornerRadius="8"
                    Background="#2d2d2d">
                <Grid ColumnDefinitions="Auto,*,Auto">
                    
                    <!-- Avatar -->
                    <Border Grid.Column="0" 
                            Width="50" Height="50" 
                            CornerRadius="25"
                            Background="#4a4a4a">
                        <TextBlock Text="👤" 
                                   HorizontalAlignment="Center"
                                   VerticalAlignment="Center"
                                   FontSize="24"/>
                    </Border>
                    
                    <!-- Info -->
                    <StackPanel Grid.Column="1" Margin="10,0">
                        <TextBlock Text="{Binding NombreCompleto}" 
                                   FontWeight="Bold" FontSize="16"/>
                        <TextBlock Text="{Binding Telefono}" 
                                   Opacity="0.7"/>
                    </StackPanel>
                    
                    <!-- Badge VIP -->
                    <Border Grid.Column="2"
                            IsVisible="{Binding EsVip}"
                            Background="#ffd700" 
                            Padding="8,4" 
                            CornerRadius="4">
                        <TextBlock Text="VIP" FontSize="12"/>
                    </Border>
                    
                </Grid>
            </Border>
        </DataTemplate>
    </ListBox.ItemTemplate>
</ListBox>
```

### ItemsControl (sin selección)

```xml
<!-- Para mostrar items sin poder seleccionarlos -->
<ItemsControl ItemsSource="{Binding Etiquetas}">
    <ItemsControl.ItemsPanel>
        <ItemsPanelTemplate>
            <WrapPanel/>
        </ItemsPanelTemplate>
    </ItemsControl.ItemsPanel>
    <ItemsControl.ItemTemplate>
        <DataTemplate>
            <Border Background="Blue" Padding="5" Margin="2" CornerRadius="3">
                <TextBlock Text="{Binding}" Foreground="White"/>
            </Border>
        </DataTemplate>
    </ItemsControl.ItemTemplate>
</ItemsControl>
```

---

## 🎨 Estilos

### Estilo inline

```xml
<Button Content="Guardar"
        Background="#4CAF50"
        Foreground="White"
        Padding="20,10"
        CornerRadius="5"/>
```

### Estilo con clase

```xml
<!-- En App.axaml -->
<Application.Styles>
    <FluentTheme/>
    
    <Style Selector="Button.primary">
        <Setter Property="Background" Value="#4CAF50"/>
        <Setter Property="Foreground" Value="White"/>
        <Setter Property="Padding" Value="20,10"/>
    </Style>
</Application.Styles>

<!-- En la View -->
<Button Content="Guardar" Classes="primary"/>
```

### Colores y temas

```xml
<!-- Colores directos -->
<TextBlock Foreground="Red"/>
<TextBlock Foreground="#FF5722"/>
<Border Background="#2d2d2d"/>

<!-- Transparencia -->
<Border Background="#80000000"/>  <!-- 50% transparente -->
```

---

## 📱 Ejemplo Completo

### Vista de lista de clientes

```xml
<!-- Views/ClientesView.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:Ataena.ViewModels"
             x:Class="Ataena.Views.ClientesView"
             x:DataType="vm:ClientesViewModel">

    <Grid RowDefinitions="Auto,*,Auto" Margin="20">
        
        <!-- ═══════════════════════════════════════════ -->
        <!-- FILA 0: BARRA DE HERRAMIENTAS               -->
        <!-- ═══════════════════════════════════════════ -->
        <Grid Grid.Row="0" ColumnDefinitions="*,Auto" Margin="0,0,0,15">
            
            <!-- Búsqueda -->
            <TextBox Grid.Column="0"
                     Text="{Binding Busqueda}"
                     Watermark="🔍 Buscar cliente..."
                     Margin="0,0,10,0"/>
            
            <!-- Botones -->
            <StackPanel Grid.Column="1" Orientation="Horizontal" Spacing="5">
                <Button Content="🔍 Buscar" Command="{Binding BuscarCommand}"/>
                <Button Content="➕ Nuevo" Command="{Binding NuevoCommand}"/>
            </StackPanel>
            
        </Grid>

        <!-- ═══════════════════════════════════════════ -->
        <!-- FILA 1: LISTA DE CLIENTES                   -->
        <!-- ═══════════════════════════════════════════ -->
        <ListBox Grid.Row="1"
                 ItemsSource="{Binding Clientes}"
                 SelectedItem="{Binding ClienteSeleccionado}">
            <ListBox.ItemTemplate>
                <DataTemplate>
                    <Border Padding="15" Margin="0,5" CornerRadius="8"
                            Background="#1e1e1e">
                        <Grid ColumnDefinitions="Auto,*,Auto">
                            
                            <!-- Avatar -->
                            <Border Grid.Column="0" 
                                    Width="50" Height="50" 
                                    CornerRadius="25"
                                    Background="#333">
                                <TextBlock Text="👤" 
                                           FontSize="24"
                                           HorizontalAlignment="Center"
                                           VerticalAlignment="Center"/>
                            </Border>
                            
                            <!-- Info -->
                            <StackPanel Grid.Column="1" 
                                        Margin="15,0" 
                                        VerticalAlignment="Center">
                                <TextBlock Text="{Binding NombreCompleto}" 
                                           FontWeight="SemiBold" 
                                           FontSize="16"/>
                                <StackPanel Orientation="Horizontal" 
                                            Spacing="15" 
                                            Opacity="0.7">
                                    <TextBlock Text="{Binding Telefono}"/>
                                    <TextBlock Text="{Binding Email}"/>
                                </StackPanel>
                            </StackPanel>
                            
                            <!-- Acciones -->
                            <StackPanel Grid.Column="2" 
                                        Orientation="Horizontal" 
                                        Spacing="5">
                                <Button Content="✏️" ToolTip.Tip="Editar"/>
                                <Button Content="🗑️" ToolTip.Tip="Eliminar"/>
                            </StackPanel>
                            
                        </Grid>
                    </Border>
                </DataTemplate>
            </ListBox.ItemTemplate>
        </ListBox>

        <!-- ═══════════════════════════════════════════ -->
        <!-- FILA 2: BARRA DE ESTADO                     -->
        <!-- ═══════════════════════════════════════════ -->
        <Border Grid.Row="2" Padding="10" Margin="0,15,0,0"
                Background="#1e1e1e" CornerRadius="5">
            <TextBlock Opacity="0.7">
                <Run Text="Total: "/>
                <Run Text="{Binding Clientes.Count}"/>
                <Run Text=" clientes"/>
            </TextBlock>
        </Border>
        
    </Grid>

</UserControl>
```

---

## 🔗 Siguiente documento

→ `06-comandos-utiles.md` - Referencia rápida de comandos

