# Guía: Instalador con Inno Setup (experiencia amigable)

> **Fecha:** 21 de Febrero 2026  
> **Objetivo:** Crear un instalador con asistente completo (Bienvenida, elegir carpeta, Siguiente, accesos directos, Agregar o quitar programas)

---

## ¿Por qué Inno Setup?

El Setup.exe de Velopack es **one-click**: consola negra, instala y abre. Sin opciones.

**Inno Setup** ofrece el asistente clásico de Windows:
- Página de bienvenida
- Elegir carpeta de instalación
- Crear accesos directos (escritorio, menú inicio)
- Opción de abrir la app al terminar
- Entrada en **Agregar o quitar programas**
- Desinstalador completo

Usado por **VS Code**, **Git for Windows** y muchas aplicaciones profesionales.

---

## Requisitos

1. **.NET 9 SDK** (ya lo tienes)
2. **Inno Setup 6** — [Descargar](https://jrsoftware.org/isdl.php) (gratis, ~5 MB)

---

## Build en 2 pasos

### Opción A: Script automático

```powershell
cd Ataena
.\build-installer.ps1
```

El script:
1. Ejecuta `dotnet publish` → genera `publish/`
2. Ejecuta el compilador de Inno Setup → genera `Releases\Ataena-Setup-1.0.0.exe`

### Opción B: Manual

```powershell
cd Ataena

# 1. Publicar
dotnet publish -c Release -r win-x64 --self-contained true -o .\publish

# 2. Compilar con Inno Setup
# Abre Installer\Ataena.iss en Inno Setup y pulsa Compile (Ctrl+F9)
# O desde línea de comandos:
& "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe" .\Installer\Ataena.iss
```

---

## Estructura del instalador

| Archivo | Descripción |
|---------|-------------|
| `Installer\Ataena.iss` | Script de Inno Setup |
| `build-installer.ps1` | Script de build automático |
| `Releases\Ataena-Setup-1.0.0.exe` | Instalador generado |

---

## Personalización

Edita `Installer\Ataena.iss` para cambiar:

| Sección | Qué modificar |
|---------|---------------|
| `[Setup]` | `MyAppVersion`, `MyAppPublisher`, icono |
| `[Tasks]` | Icono en escritorio (por defecto desactivado) |
| `[Languages]` | Español está por defecto |
| `[Run]` | "Abrir al terminar" (activado por defecto) |

---

## Datos del usuario

El instalador copia la app a `C:\Program Files\Ataena CRM\`.

Los **datos** (data.db, ficheros, backups, logs) se guardan en:
```
%LocalAppData%\Ataena\
```

Eso lo gestiona la aplicación. Al desinstalar, **no se borran los datos** del usuario.

---

## Actualizaciones automáticas

A partir de ahora, Ataena **NO usa Velopack**. La comprobación e instalación de nuevas versiones la gestiona un módulo propio que consulta **GitHub Releases** y relanza Inno Setup en modo silencioso.

👉 Ver guía completa: [`11-guia-actualizaciones.md`](11-guia-actualizaciones.md)

| | Inno Setup + GitHub Releases | Velopack (descartado) |
|---|------------------------------|------------------------|
| **Experiencia primera instalación** | Asistente completo | One-click, consola negra |
| **Agregar o quitar programas** | Sí | No |
| **Elegir carpeta** | Sí | No |
| **Actualizaciones en 1 clic** | Sí (banner en la app) | Sí |
| **Dependencias** | Ninguna | Update.exe, estructura de carpetas específica |
| **Control total sobre el instalador** | Sí | No |

---

## Si la app no abre tras instalar

Revisa el archivo de diagnóstico de arranque:
```
%LocalAppData%\Ataena\logs\arranque-diag.txt
```

Ahí verás en qué punto se detiene el arranque. Ejemplo:
- `Main: Base de datos migrada OK` → BD OK
- `App: Splash cerrado` → splash completado
- `App: ERROR - ...` → indica el fallo

---

## Checklist

- [ ] Instalar Inno Setup 6
- [ ] Ejecutar `.\build-installer.ps1`
- [ ] Probar `Ataena-Setup-1.0.0.exe`
- [ ] Verificar: asistente, carpeta, accesos directos, desinstalar
