# Sistema de Logging

> **Objetivo:** Documentar el sistema de logging para diagnóstico de errores

---

## 📋 Índice

1. [Resumen](#-resumen)
2. [Ubicación de los Logs](#-ubicación-de-los-logs)
3. [Cómo Ver los Logs](#-cómo-ver-los-logs)
4. [Cómo Exportar Logs](#-cómo-exportar-logs)
5. [Niveles de Log](#-niveles-de-log)
6. [Para el Desarrollador](#-para-el-desarrollador)
7. [Solución de Problemas](#-solución-de-problemas)

---

## 🎯 Resumen

Ataena CRM utiliza **Serilog** para registrar todos los eventos y errores de la aplicación. Los logs se guardan automáticamente en archivos de texto que pueden ser consultados para diagnosticar problemas.

### Características

- ✅ **Logging automático** de errores y eventos importantes
- ✅ **Archivos diarios** (un archivo por día)
- ✅ **Retención de 30 días** (se eliminan automáticamente)
- ✅ **Vista integrada** en la aplicación para ver logs
- ✅ **Exportación** de logs para enviar al soporte

---

## 📁 Ubicación de los Logs

Los logs se guardan en:

```
%LOCALAPPDATA%\Ataena\logs\
```

**Ejemplo en Windows:**
```
C:\Users\[TuUsuario]\AppData\Local\Ataena\logs\
```

### Formato de archivos

Los archivos siguen el formato:
```
ataena-YYYYMMDD.log
```

**Ejemplo:**
- `ataena-20251205.log` (log del 5 de diciembre de 2025)
- `ataena-20251206.log` (log del 6 de diciembre de 2025)

---

## 👀 Cómo Ver los Logs

### Opción 1: Desde la aplicación (Recomendado)

1. Abre Ataena CRM
2. En el menú lateral, haz clic en **"📋 Logs"**
3. Selecciona el archivo de log que quieres ver (por defecto se muestra el más reciente)
4. Haz clic en **"🔄 Cargar"** si no se carga automáticamente

### Opción 2: Desde el explorador de archivos

1. Abre Ataena CRM
2. En el menú lateral, haz clic en **"📋 Logs"**
3. Haz clic en **"📁 Abrir Carpeta"**
4. Se abrirá la carpeta de logs en el explorador
5. Abre el archivo `.log` que necesites con un editor de texto

---

## 💾 Cómo Exportar Logs

### Para enviar al soporte técnico

1. Abre la vista de Logs en la aplicación
2. Selecciona el archivo de log del día en que ocurrió el problema
3. Haz clic en **"💾 Exportar"**
4. El log se guardará en tu carpeta de Documentos con el nombre:
   ```
   ataena-log-export-YYYYMMDDHHmmss.txt
   ```
5. Envía ese archivo al soporte técnico

### Ubicación de exportación

Los logs exportados se guardan en:
```
%USERPROFILE%\Documents\
```

---

## 📊 Niveles de Log

El sistema registra diferentes niveles de eventos:

| Nivel | Descripción | Cuándo se usa |
|-------|-------------|---------------|
| **Debug** | Información detallada para desarrollo | Operaciones internas, flujo de datos |
| **Information** | Eventos normales de la aplicación | Inicio de app, operaciones exitosas |
| **Warning** | Situaciones anómalas pero no críticas | Validaciones fallidas, datos faltantes |
| **Error** | Errores que impiden una operación | Excepciones capturadas, fallos de BD |
| **Fatal** | Errores críticos que cierran la app | Errores no manejados al iniciar |

### Ejemplo de log

```
2025-12-05 14:30:15.123 +01:00 [INF] Ataena CRM iniciado - Sistema de logging activado
2025-12-05 14:30:15.456 +01:00 [INF] Carpeta de logs: C:\Users\Usuario\AppData\Local\Ataena\logs
2025-12-05 14:30:20.789 +01:00 [DBG] Cargando datos del Dashboard
2025-12-05 14:30:21.012 +01:00 [DBG] Citas de hoy cargadas: 3 citas
2025-12-05 14:30:21.234 +01:00 [INF] Clientes cargados: 25 clientes activos
2025-12-05 14:35:10.567 +01:00 [ERR] Error al cargar clientes desde la base de datos
System.Exception: SQLite error: database is locked
   at Ataena.ViewModels.ClientesViewModel.CargarClientes()
```

---

## 👨‍💻 Para el Desarrollador

### Estructura del sistema

```
Ataena/
├── Services/
│   └── LoggingService.cs      ← Configuración de Serilog
├── ViewModels/
│   └── LogsViewModel.cs        ← Lógica de visualización
└── Views/
    └── LogsView.axaml          ← UI para ver logs
```

### Cómo usar logging en el código

```csharp
using Serilog;

// Log de información
Log.Information("Cliente guardado: {ClienteId}", cliente.Id);

// Log de error con excepción
try
{
    // código...
}
catch (Exception ex)
{
    Log.Error(ex, "Error al guardar cliente");
}

// Log de debug
Log.Debug("Buscando clientes con término: {Termino}", textoBusqueda);

// Log de warning
if (algoAnomalo)
{
    Log.Warning("Situación anómala detectada: {Detalle}", detalle);
}
```

### Configuración en LoggingService

```csharp
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()                    // Nivel mínimo: Debug
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)  // Reducir ruido de EF Core
    .WriteTo.Console()                       // También a consola
    .WriteTo.File(
        path: logFile,
        rollingInterval: RollingInterval.Day,  // Un archivo por día
        retainedFileCountLimit: 30)           // Mantener 30 días
    .CreateLogger();
```

---

## 🔧 Solución de Problemas

### El log está vacío o no se crea

1. Verifica que la aplicación tenga permisos de escritura en:
   ```
   %LOCALAPPDATA%\Ataena\logs\
   ```
2. Revisa si hay errores en el log de la aplicación (consola)
3. Verifica que Serilog esté correctamente inicializado en `Program.cs`

### No puedo abrir la carpeta de logs

1. Navega manualmente a:
   ```
   %LOCALAPPDATA%\Ataena\logs\
   ```
2. O usa la tecla Windows + R y escribe:
   ```
   %LOCALAPPDATA%\Ataena\logs
   ```

### El log es muy grande

- Los logs muestran solo las **últimas 1000 líneas** por defecto
- Para ver el log completo, abre el archivo directamente con un editor de texto
- Los logs antiguos (más de 30 días) se eliminan automáticamente

### ¿Qué información incluir al reportar un error?

Cuando reportes un error, incluye:

1. **Fecha y hora** del error
2. **Archivo de log** del día (exportado)
3. **Descripción** de lo que estabas haciendo
4. **Pasos para reproducir** el error (si es posible)

---

## 📝 Notas Importantes

- ⚠️ Los logs pueden contener información sensible (nombres, teléfonos, etc.)
- 🔒 No compartas logs públicamente sin revisar su contenido
- 🗑️ Los logs se eliminan automáticamente después de 30 días
- 💾 Exporta los logs importantes antes de que se eliminen

---

> **Tip:** Si la aplicación crashea, el log se guarda antes de cerrarse, así que siempre podrás ver qué pasó.

