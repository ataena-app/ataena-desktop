# Análisis: Primer Inicio y Sistema de Backups

> **Fecha:** 21 de Febrero 2026  
> **Estado:** Análisis y propuestas  
> **Objetivo:** Menú post-instalación (crear nuevo / importar) y mejoras al sistema de backups

---

## 📋 Índice

1. [Requisito: Menú Post-Instalación](#-requisito-menú-post-instalación)
2. [Estado Actual del Sistema de Backups](#-estado-actual-del-sistema-de-backups)
3. [Problemas Detectados](#-problemas-detectados)
4. [Propuestas de Mejora](#-propuestas-de-mejora)
5. [Plan de Implementación](#-plan-de-implementación)

---

## 🎯 Requisito: Menú Post-Instalación

### Lo que el usuario quiere
Tras instalar el programa, debe aparecer un **menú inicial** con dos opciones:

| Opción | Descripción |
|--------|-------------|
| **Crear nuevo estudio** | Empezar con datos vacíos (o con datos de ejemplo mínimos) |
| **Importar datos existentes** | Seleccionar un archivo .zip de backup y restaurar |

### Flujo propuesto

```
┌─────────────────────────────────────────────────────────────┐
│  PRIMER INICIO (o instalación nueva)                        │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ¿Existe data.db con datos?                                 │
│                                                             │
│  NO (instalación nueva)                                     │
│       ↓                                                     │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  🖋️ Bienvenido a Ataena                             │   │
│  │                                                       │   │
│  │  ¿Cómo quieres empezar?                               │   │
│  │                                                       │   │
│  │  [📁 Crear nuevo estudio]                             │   │
│  │     Empezar con datos vacíos                          │   │
│  │                                                       │   │
│  │  [📥 Importar datos existentes]                       │   │
│  │     Restaurar desde un backup (.zip)                  │   │
│  │                                                       │   │
│  └─────────────────────────────────────────────────────┘   │
│       ↓                                                     │
│  Si "Crear nuevo" → Crear data.db vacía con seed mínimo    │
│  Si "Importar" → Abrir selector de archivo .zip → Restaurar │
│                                                             │
│  SÍ (ya tiene datos)                                        │
│       ↓                                                     │
│  Ir directo a MainWindow (comportamiento actual)             │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### Cómo detectar "primer inicio"
- **Opción A:** Archivo flag `%LOCALAPPDATA%\Ataena\.setup_completado` — no existe = primer inicio
- **Opción B:** Comprobar si `data.db` existe (las migraciones la crean al arrancar, así que siempre existirá tras el primer run)
- **Opción C:** Si data.db no existe O está vacía

**Recomendación:** Opción A. Archivo `.setup_completado` que se crea cuando el usuario completa el wizard (elige "Crear nuevo" o "Importar"). Simple y fiable.

**Nota:** Las migraciones se ejecutan en `Program.Main` antes de mostrar cualquier UI, así que `data.db` ya existirá cuando mostremos el menú. "Crear nuevo" = usar la BD actual (con seed). "Importar" = restaurar backup y sobrescribir data.db.

---

## 📦 Estado Actual del Sistema de Backups

### Lo que funciona bien ✅

| Aspecto | Estado |
|---------|--------|
| **Crear backup** | ZIP con data.db, ficheros/, metadata.json, README.txt |
| **Estructura del ZIP** | Correcta, incluye todo lo necesario |
| **VACUUM INTO** | Copia segura de la BD sin bloqueos |
| **Fallback FileShare.Read** | Si VACUUM falla, copia directa |
| **Detección de nube** | OneDrive, Google Drive, Dropbox |
| **Copia a nube** | Automática tras crear backup |
| **Rotación** | Elimina backups antiguos según configuración |
| **Restaurar desde lista** | Funciona si el backup está en la carpeta de nube |
| **Validación** | Valida que el ZIP tenga data.db y metadata.json |
| **Backup antes de restaurar** | Crea backup de datos actuales antes de sobrescribir |

### Contenido del backup (BackupService)
- `data.db` — Base de datos SQLite
- `ficheros/` — Carpeta con consentimientos, fotos DNI, fotos trabajos, logos
- `metadata.json` — Fecha, versión, estadísticas (clientes, citas, etc.)
- `README.txt` — Instrucciones de restauración

---

## ❌ Problemas Detectados

### 1. **RestaurarDesdeArchivo no implementado**
- **Ubicación:** `BackupViewModel.RestaurarDesdeArchivoCommand`
- **Problema:** Tiene un TODO. No abre el selector de archivos. Solo usa `BackupSeleccionado` si existe.
- **Impacto:** Un usuario con un backup en USB, disco externo o descargas **no puede restaurar** a menos que lo copie a OneDrive/GDrive/Dropbox y lo seleccione ahí.

### 2. **Lista de backups solo muestra nube**
- **Ubicación:** `BackupViewModel.ActualizarListaBackups`
- **Problema:** Solo carga backups de `ServicioNubeSeleccionado.RutaCarpeta`. Si no hay nube seleccionada o la carpeta no existe, la lista está **vacía**.
- **Impacto:** Los backups locales (`%LOCALAPPDATA%\Ataena\backups\`) **nunca se muestran**. El usuario puede tener backups locales y no verlos.

### 3. **Flujo de "PC nuevo" poco claro**
- **Problema:** El README dice "Ir a Configuración > Backup" pero Backup es un ítem del menú lateral, no está dentro de Configuración.
- **Problema:** Un usuario en PC nuevo no tiene backups en la nube aún (primera vez que abre). Necesita "Seleccionar archivo ZIP" que **no funciona**.

### 4. **No hay pantalla de primer inicio**
- **Problema:** La app siempre arranca igual. No distingue entre "primera vez" y "uso normal".
- **Impacto:** No se ofrece la opción explícita de "Importar" al usuario que acaba de instalar.

### 5. **Selector de carpeta personalizada no implementado**
- **Ubicación:** `BackupViewModel.SeleccionarCarpetaNubeCommand`
- **Problema:** TODO. Solo muestra mensaje "pendiente de implementar".
- **Impacto:** Usuario no puede elegir carpeta personalizada para backups (ej. disco externo, otra ruta).

### 6. **Restauración requiere reinicio manual**
- **Problema:** Tras restaurar, el mensaje dice "cierra y vuelve a abrir la aplicación".
- **Mejora posible:** Cerrar y reiniciar automáticamente la app tras restauración exitosa.

---

## 💡 Propuestas de Mejora

### Prioridad Alta

| # | Mejora | Descripción |
|---|--------|-------------|
| 1 | **Pantalla de primer inicio** | Mostrar "Crear nuevo" / "Importar" cuando no hay datos |
| 2 | **RestaurarDesdeArchivo funcional** | Implementar OpenFilePicker para seleccionar .zip desde cualquier ruta |
| 3 | **Mostrar backups locales** | Incluir en la lista los backups de `%LOCALAPPDATA%\Ataena\backups\` |

### Prioridad Media

| # | Mejora | Descripción |
|---|--------|-------------|
| 4 | **Unificar fuentes de backups** | Mostrar backups locales + nube en una sola lista (con indicador de origen) |
| 5 | **Reinicio automático tras restaurar** | Cerrar app y relanzar para aplicar cambios sin que el usuario lo haga manualmente |
| 6 | **Selector de carpeta** | Implementar selección de carpeta personalizada para backups |

### Prioridad Baja

| # | Mejora | Descripción |
|---|--------|-------------|
| 7 | **Checksum en metadata** | Añadir checksum del ZIP para detectar corrupción |
| 8 | **Validación de versión** | Advertir si el backup es de una versión muy antigua |
| 9 | **README actualizado** | Corregir instrucciones (Backup está en menú lateral, no en Configuración) |

---

## 📐 Plan de Implementación

### Fase 1: Primer inicio (menú crear/importar)

```
1. Crear SetupInicialView + SetupInicialViewModel
   - Vista con dos botones grandes: "Crear nuevo estudio" / "Importar datos"
   - Lógica: detectar si es primer inicio

2. Modificar Program.cs o App.axaml.cs
   - Antes de mostrar MainWindow: comprobar si hay datos
   - Si no hay datos (o data.db no existe): mostrar SetupInicialView
   - Si "Crear nuevo": crear BD con seed, ir a MainWindow
   - Si "Importar": abrir file picker .zip, restaurar, ir a MainWindow

3. Detección de primer inicio
   - data.db no existe, O
   - data.db existe pero Configuracion está vacía o no tiene datos de estudio
   - Cuidado: las migraciones crean Configuracion con seed. Revisar lógica.
```

**Nota sobre migraciones:** El seed de Configuracion se aplica en la migración inicial. Habrá que definir: ¿"primer inicio" = primera vez que el usuario elige, o = data.db no existe? Si data.db se crea al aplicar migraciones, siempre existirá. Opción: usar un flag en Configuracion (ej. `SetupCompletado`) o un archivo `.primer_inicio` que se elimina tras completar el wizard.

### Fase 2: Restaurar desde archivo

```
1. BackupViewModel.RestaurarDesdeArchivo
   - Obtener TopLevel (MainWindow)
   - OpenFilePickerAsync con filtro *.zip
   - Si usuario selecciona archivo: llamar a RestaurarBackupAsync con esa ruta
   - Mostrar confirmación con resumen del backup antes de restaurar
```

### Fase 3: Mostrar backups locales

```
1. ActualizarListaBackups
   - Siempre incluir backups de carpeta local (BackupService.ObtenerRutaCarpetaBackups())
   - Combinar con backups de nube si hay servicio seleccionado
   - Añadir propiedad Origen (Local / OneDrive / etc.) a InfoBackup para mostrar en UI
```

### Fase 4: Mejoras adicionales

- Selector de carpeta con `StorageProvider.OpenFolderPickerAsync`
- Reinicio automático tras restauración: `Environment.Exit(0)` + relanzar con `Process.Start`

---

## 🔄 Flujo Completo Propuesto

### Usuario nuevo (instala por primera vez)

```
Instala Ataena
    ↓
Ejecuta por primera vez
    ↓
¿data.db existe? NO (o flag primer_inicio)
    ↓
Pantalla: "Crear nuevo" | "Importar"
    ↓
[Crear nuevo] → BD con seed → MainWindow
[Importar] → File picker .zip → Validar → Confirmar → Restaurar → MainWindow
```

### Usuario que migra a PC nuevo

```
Tiene backup en USB / descargas / OneDrive
    ↓
Instala Ataena en PC nuevo
    ↓
Primer inicio → "Importar datos existentes"
    ↓
Selecciona .zip (desde USB, etc.)
    ↓
Restauración → MainWindow con todos sus datos
```

### Usuario que ya usa la app

```
Abre Ataena (data.db existe, tiene datos)
    ↓
MainWindow directamente (sin pantalla de setup)
```

---

## ✅ Checklist de Implementación

### Primer inicio
- [ ] Crear SetupInicialView
- [ ] Crear SetupInicialViewModel
- [ ] Lógica de detección "primer inicio"
- [ ] Botón "Crear nuevo estudio"
- [ ] Botón "Importar datos" → file picker → restaurar
- [ ] Integrar en flujo de arranque (App.axaml.cs)

### Backups
- [ ] Implementar RestaurarDesdeArchivo con OpenFilePicker
- [ ] Incluir backups locales en la lista
- [ ] Indicador de origen (Local / Nube) en cada backup
- [ ] Selector de carpeta personalizada (opcional)
- [ ] Reinicio automático tras restaurar (opcional)

### Documentación
- [ ] Actualizar README.txt dentro del backup
- [ ] Actualizar 07-plan-licencias-actualizaciones.md si aplica

---

## 📚 Referencias

- `BackupService.cs` — Creación de backups
- `RestauracionService.cs` — Restauración
- `BackupViewModel.cs` — UI y comandos
- `ConfiguracionViewModel.SeleccionarLogoAsync` — Ejemplo de OpenFilePicker

---

> **Nota:** Este documento es de análisis. La implementación se hará cuando se decida proceder.
