# Comandos Útiles

> **Nivel:** Todos  
> **Objetivo:** Referencia rápida de comandos de terminal

---

## 📋 Índice

1. [Ejecutar la Aplicación](#-ejecutar-la-aplicación)
2. [Debuguear y Ver Errores](#-debuguear-y-ver-errores)
3. [Comandos .NET Básicos](#-comandos-net-básicos)
4. [Entity Framework (Migraciones)](#-entity-framework-migraciones)
5. [Gestión de Paquetes](#-gestión-de-paquetes)
6. [Publicación](#-publicación)
7. [Git](#-git)
8. [Solución de Problemas](#-solución-de-problemas)

---

## 🚀 Ejecutar la Aplicación

### Desde la carpeta del proyecto

```powershell
# 1. Navegar a la carpeta del proyecto
cd "C:\Users\Jose Vallejo\Documents\MYO_DESK\Ataena"

# 2. Ejecutar
dotnet run
```

### Desde la raíz del workspace

```powershell
# Usando --project (desde MYO_DESK)
dotnet run --project "Ataena\Ataena.csproj"
```

### Ejecutar en modo Release

```powershell
# Más rápido, sin información de debug
dotnet run -c Release
```

---

## 🐛 Debuguear y Ver Errores

### ⚠️ IMPORTANTE: Ver errores en terminal

Por defecto, si la app crashea puede que no veas el error. Usa este comando para **capturar todos los errores**:

```powershell
# Redirigir stderr a stdout para ver todos los errores
dotnet run 2>&1
```

### ¿Por qué `2>&1`?

```
stdout (1) = Salida normal
stderr (2) = Errores

2>&1 = Redirigir errores a la salida normal
```

### Ejemplo de flujo de debug

```powershell
# 1. Navegar al proyecto
cd Ataena

# 2. Compilar primero (ver errores de compilación)
dotnet build

# 3. Si compila, ejecutar viendo errores de runtime
dotnet run 2>&1
```

### Errores comunes y cómo verlos

| Tipo de error | Cómo verlo |
|---------------|------------|
| Error de compilación | `dotnet build` muestra el error |
| Error de runtime (crash) | `dotnet run 2>&1` muestra el stack trace |
| Error de XAML | La app crashea → usar `2>&1` |
| Error de BD | La app crashea → usar `2>&1` |

### Ejemplo real de error capturado

```powershell
PS> dotnet run 2>&1

Unhandled exception. System.NotSupportedException: SQLite does not 
support expressions of type 'TimeSpan' in ORDER BY clauses...
   at Ataena.ViewModels.DashboardViewModel.CargarCitasHoy()
   ...
```

### Debug con más información

```powershell
# Compilar con información detallada
dotnet build -v detailed

# Ver qué paquetes se usan
dotnet build -v diagnostic | Select-String "PackageReference"
```

### Hot Reload (cambios en vivo)

```powershell
# Ejecutar con hot reload activado
dotnet watch run
```

> **Nota:** Hot Reload permite ver cambios en el código sin reiniciar la app.

---

## 🔧 Comandos .NET Básicos

### Información del sistema

```powershell
# Ver versión de .NET instalada
dotnet --version

# Ver todas las versiones instaladas
dotnet --list-sdks

# Ver información completa
dotnet --info
```

### Compilar y ejecutar

```powershell
# Navegar al proyecto
cd Ataena

# Compilar
dotnet build

# Compilar en modo Release (optimizado)
dotnet build -c Release

# Ejecutar
dotnet run

# Ejecutar desde la raíz (sin navegar)
dotnet run --project Ataena

# Compilar y ejecutar (limpia antes)
dotnet build
dotnet run

# Limpiar archivos compilados
dotnet clean
```

### Restaurar paquetes

```powershell
# Restaurar paquetes NuGet
dotnet restore
```

---

## 🗄️ Entity Framework (Migraciones)

### Instalación de herramienta EF

```powershell
# Instalar herramienta global (solo una vez)
dotnet tool install --global dotnet-ef

# Actualizar herramienta
dotnet tool update --global dotnet-ef

# Verificar instalación
dotnet ef --version
```

### Migraciones

```powershell
# IMPORTANTE: Ejecutar desde la carpeta del proyecto
cd Ataena

# Crear nueva migración
dotnet ef migrations add NombreDescriptivo

# Ejemplos de nombres descriptivos:
dotnet ef migrations add Inicial
dotnet ef migrations add AgregarTablaClientes
dotnet ef migrations add AgregarCampoEmailCliente
dotnet ef migrations add CrearRelacionClienteCita

# Aplicar migraciones pendientes
dotnet ef database update

# Ver lista de migraciones
dotnet ef migrations list

# Eliminar última migración (si NO se ha aplicado)
dotnet ef migrations remove

# Revertir a una migración específica
dotnet ef database update NombreMigracion

# Revertir todas las migraciones (vaciar BD)
dotnet ef database update 0
```

### Scripts SQL

```powershell
# Generar script SQL de todas las migraciones
dotnet ef migrations script

# Generar script desde una migración específica
dotnet ef migrations script MigracionInicial

# Generar script y guardarlo en archivo
dotnet ef migrations script -o script.sql
```

### Base de datos

```powershell
# Eliminar la base de datos
dotnet ef database drop

# Eliminar y confirmar sin preguntar
dotnet ef database drop --force
```

---

## 📦 Gestión de Paquetes

### Añadir paquetes

```powershell
# Añadir paquete
dotnet add package NombrePaquete

# Añadir versión específica
dotnet add package NombrePaquete --version 1.2.3

# Ejemplos:
dotnet add package CommunityToolkit.Mvvm
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
dotnet add package FluentAvaloniaUI
```

### Listar y actualizar

```powershell
# Ver paquetes instalados
dotnet list package

# Ver paquetes desactualizados
dotnet list package --outdated

# Actualizar paquete
dotnet add package NombrePaquete
```

### Eliminar paquetes

```powershell
dotnet remove package NombrePaquete
```

---

## 📤 Publicación

### Publicar para Windows

```powershell
# Publicar versión Release
dotnet publish -c Release

# Publicar como archivo único (self-contained)
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

# Publicar con recorte (más pequeño)
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true
```

### Opciones de publicación

| Opción | Descripción |
|--------|-------------|
| `-c Release` | Modo Release (optimizado) |
| `-r win-x64` | Para Windows 64-bit |
| `--self-contained true` | Incluye .NET (no requiere instalación) |
| `-p:PublishSingleFile=true` | Un solo archivo .exe |
| `-p:PublishTrimmed=true` | Elimina código no usado |

### Ubicación del resultado

```
Ataena/bin/Release/net9.0/win-x64/publish/
    └── Ataena.exe    ← Tu aplicación
```

---

## 🔀 Git

### Comandos básicos

```powershell
# Inicializar repositorio
git init

# Ver estado
git status

# Añadir todos los cambios
git add .

# Añadir archivo específico
git add archivo.cs

# Commit
git commit -m "Mensaje descriptivo"

# Ver historial
git log --oneline
```

### Ramas

```powershell
# Ver ramas
git branch

# Crear rama
git branch nueva-funcionalidad

# Cambiar de rama
git checkout nueva-funcionalidad

# Crear y cambiar en un paso
git checkout -b nueva-funcionalidad

# Fusionar rama
git checkout main
git merge nueva-funcionalidad

# Eliminar rama
git branch -d rama-a-eliminar
```

### Remoto (GitHub, etc.)

```powershell
# Añadir remoto
git remote add origin https://github.com/usuario/repo.git

# Subir cambios
git push -u origin main

# Bajar cambios
git pull
```

### .gitignore recomendado

```gitignore
# Archivos compilados
bin/
obj/

# Configuración local
*.user
.vs/

# Base de datos local
*.db
*.db-wal
*.db-shm

# Carpeta de datos de usuario
AppData/
```

---

## 🔧 Solución de Problemas

### Limpiar y reconstruir

```powershell
# Limpiar todo y reconstruir
dotnet clean
dotnet restore
dotnet build
```

### Problemas con EF

```powershell
# Si "dotnet ef" no funciona
dotnet tool install --global dotnet-ef

# Si las migraciones fallan, eliminar y recrear
Remove-Item -Recurse -Force .\Migrations
dotnet ef migrations add Inicial
dotnet ef database update
```

### Problemas con paquetes

```powershell
# Limpiar caché de NuGet
dotnet nuget locals all --clear

# Restaurar de nuevo
dotnet restore
```

### Eliminar base de datos

```powershell
# Ruta de la BD en Windows
$dbPath = "$env:LOCALAPPDATA\Ataena\data.db"

# Eliminar
Remove-Item $dbPath -Force

# Eliminar carpeta completa
Remove-Item "$env:LOCALAPPDATA\Ataena" -Recurse -Force
```

### Ver errores detallados

```powershell
# Compilar con más detalle
dotnet build -v detailed

# Ejecutar con logs
dotnet run --verbosity detailed
```

---

## 📝 Cheatsheet Rápido

```powershell
# ════════════════════════════════════════════
# DESARROLLO DIARIO
# ════════════════════════════════════════════

# Compilar y ejecutar
dotnet run --project Ataena

# Añadir nueva entidad → crear migración
dotnet ef migrations add NombreDescriptivo
dotnet ef database update

# ════════════════════════════════════════════
# CUANDO ALGO FALLA
# ════════════════════════════════════════════

# Reset completo
dotnet clean
dotnet restore
dotnet build

# ════════════════════════════════════════════
# PUBLICAR
# ════════════════════════════════════════════

# Crear ejecutable
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

---

## 📚 Recursos adicionales

- [Documentación .NET CLI](https://docs.microsoft.com/dotnet/core/tools/)
- [Documentación EF Core](https://docs.microsoft.com/ef/core/cli/dotnet)
- [Documentación Avalonia](https://docs.avaloniaui.net/)

---

> **Tip:** Guarda este documento como referencia rápida. Los comandos más usados son `dotnet run`, `dotnet ef migrations add` y `dotnet ef database update`.

