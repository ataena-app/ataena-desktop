# Roadmap del Proyecto

> **Última actualización:** 30 de Diciembre 2024  
> **Estado general:** En desarrollo - Fase 3 Completada, Fase 4 en progreso (75% del proyecto)

---

## 📋 Índice

1. [Resumen de Progreso](#-resumen-de-progreso)
2. [Fase 1: Fundamentos](#-fase-1-fundamentos-completada)
3. [Fase 2: Módulos Principales](#-fase-2-módulos-principales-en-progreso)
4. [Fase 3: Funcionalidades Adicionales](#-fase-3-funcionalidades-adicionales-pendiente)
5. [Fase 4: Pulido y Distribución](#-fase-4-pulido-y-distribución-pendiente)
6. [Historial de Cambios](#-historial-de-cambios)

---

## 📊 Resumen de Progreso

```
Fase 1: Fundamentos          [████████████████████] 100%
Fase 2: Módulos Principales  [████████████████████] 100%
Fase 3: Funcionalidades      [████████████████████] 100%
Fase 4: Distribución         [░░░░░░░░░░░░░░░░░░░░]   0%
─────────────────────────────────────────────────────────
Total del proyecto           [███████████████░░░░░]  75%
```

---

## ✅ Fase 1: Fundamentos (COMPLETADA)

### Configuración del proyecto

| Tarea | Estado | Fecha |
|-------|--------|-------|
| Crear proyecto Avalonia | ✅ Completado | Dic 2024 |
| Configurar .NET 9 | ✅ Completado | Dic 2024 |
| Añadir paquetes NuGet (FluentAvalonia, EF Core, etc.) | ✅ Completado | Dic 2024 |
| Configurar estructura de carpetas | ✅ Completado | Dic 2024 |

### Modelos de datos

| Tarea | Estado | Fecha |
|-------|--------|-------|
| Modelo Cliente | ✅ Completado | Dic 2024 |
| Modelo Cita | ✅ Completado | Dic 2024 |
| Modelo Trabajo | ✅ Completado | Dic 2024 |
| Modelo Consentimiento | ✅ Completado | Dic 2024 |
| Modelo Configuracion | ✅ Completado | Dic 2024 |
| Enumeraciones (EstadoCita, TipoCita, etc.) | ✅ Completado | Dic 2024 |

### Base de datos

| Tarea | Estado | Fecha |
|-------|--------|-------|
| Crear DbContext | ✅ Completado | Dic 2024 |
| Configurar relaciones entre tablas | ✅ Completado | Dic 2024 |
| Crear migración inicial | ✅ Completado | Dic 2024 |
| Aplicar migración | ✅ Completado | Dic 2024 |

### Documentación

| Tarea | Estado | Fecha |
|-------|--------|-------|
| Documento de idea general | ✅ Completado | Dic 2024 |
| Documentación de base de datos | ✅ Completado | Dic 2024 |
| Guías de desarrollo | ✅ Completado | Dic 2024 |

---

## ✅ Fase 2: Módulos Principales (COMPLETADA)

### Dashboard

| Tarea | Estado | Fecha |
|-------|--------|-------|
| DashboardViewModel con lógica | ✅ Completado | Dic 2024 |
| DashboardView con diseño | ✅ Completado | Dic 2024 |
| Tarjetas de estadísticas | ✅ Completado | Dic 2024 |
| Lista de citas del día | ✅ Completado | Dic 2024 |
| Sección de alertas | ✅ Completado | Dic 2024 |
| Botones de acciones rápidas | ✅ Completado | Dic 2024 |
| Navegación lateral | ✅ Completado | Dic 2024 |

### Gestión de Clientes

| Tarea | Estado | Fecha |
|-------|--------|-------|
| ClientesViewModel | ✅ Completado | Dic 2024 |
| ClientesView (lista) | ✅ Completado | Dic 2024 |
| CRUD de clientes | ✅ Completado | Dic 2024 |
| Búsqueda de clientes | ✅ Completado | Dic 2024 |
| Modal de edición/creación | ✅ Completado | Dic 2024 |
| Soft delete (desactivación) | ✅ Completado | Dic 2024 |
| Diseño moderno con modales | ✅ Completado | Dic 2024 |

### Agenda / Citas

| Tarea | Estado | Fecha |
|-------|--------|-------|
| AgendaViewModel | ✅ Completado | Dic 2024 |
| AgendaView (calendario) | ✅ Completado | Dic 2024 |
| Vista semana | ✅ Completado | Dic 2024 |
| Crear/editar citas | ✅ Completado | Dic 2024 |
| Cambiar estado de citas | ✅ Completado | Dic 2024 |
| Asociar cita a cliente | ✅ Completado | Dic 2024 |
| Asociar trabajo a cita | ✅ Completado | Dic 2024 |
| Modal de creación/edición | ✅ Completado | Dic 2024 |
| Crear trabajo desde cita | ✅ Completado | Dic 2024 |
| Diseño moderno con modales | ✅ Completado | Dic 2024 |
| Calendario semanal personalizado | ✅ Completado | Dic 2024 |
| Drag and drop de citas | ✅ Completado | Dic 2024 |
| Redimensionado de citas | ✅ Completado | Dic 2024 |
| Detección de superposiciones | ✅ Completado | Dic 2024 |
| Visualización lado a lado (Teams-style) | ✅ Completado | Dic 2024 |
| Preview mejorada durante arrastre | ✅ Completado | Dic 2024 |
| Menú contextual para citas superpuestas | ✅ Completado | Dic 2024 |

### Trabajos / Galería

| Tarea | Estado | Fecha |
|-------|--------|-------|
| TrabajosViewModel | ✅ Completado | Dic 2024 |
| TrabajosView (lista) | ✅ Completado | Dic 2024 |
| CRUD de trabajos | ✅ Completado | Dic 2024 |
| Búsqueda de trabajos | ✅ Completado | Dic 2024 |
| Filtro por cliente | ✅ Completado | Dic 2024 |
| Asociar trabajo a cliente | ✅ Completado | Dic 2024 |
| Modal de creación/edición | ✅ Completado | Dic 2024 |
| Pre-selección de cliente desde cita | ✅ Completado | Dic 2024 |
| Diseño moderno con modales | ✅ Completado | Dic 2024 |
| Subir fotos del trabajo | ✅ Completado | Dic 2024 | Fotos "antes" y "después" con captura desde móvil |
| Galería de fotos | ✅ Completado | Dic 2024 | Visualización de fotos en modal de trabajo |

### Sistema de Logs

| Tarea | Estado | Fecha |
|-------|--------|-------|
| LogsViewModel | ✅ Completado | Dic 2024 |
| LogsView | ✅ Completado | Dic 2024 |
| Visualización de logs | ✅ Completado | Dic 2024 |
| Exportar logs | ✅ Completado | Dic 2024 |
| Cargar logs desde archivo | ✅ Completado | Dic 2024 |
| Integración con Serilog | ✅ Completado | Dic 2024 |

### Configuración

| Tarea | Estado | Fecha | Notas |
|-------|--------|-------|-------|
| ConfiguracionViewModel | ✅ Completado | Dic 2024 | |
| ConfiguracionView | ✅ Completado | Dic 2024 | |
| Datos del estudio | ✅ Completado | Dic 2024 | Configuración inicial hardcodeada (Estudio Erzulie) |
| Configuración SMTP | ⏳ Pendiente | | Diferido para más adelante |
| Tema claro/oscuro | ⏳ Pendiente | | Diferido para más adelante |

---

## ✅ Fase 3: Funcionalidades Adicionales (COMPLETADA)

### Consentimientos

| Tarea | Estado | Fecha | Notas |
|-------|--------|-------|-------|
| Plan de implementación | ✅ Completado | Dic 2024 | Plan detallado creado |
| Arquitectura definida | ✅ Completado | Dic 2024 | Sistema de firma dual (móvil/PC) |
| Infraestructura base | ✅ Completado | Dic 2024 | Librerías instaladas, carpetas creadas |
| Servidor HTTP local | ✅ Completado | Dic 2024 | Servidor HTTP con firewall automático |
| Página web de firma (móvil) | ✅ Completado | Dic 2024 | HTML/JS/CSS responsive para móvil |
| Generación de QR codes | ✅ Completado | Dic 2024 | QR codes generados automáticamente |
| Generación de PDFs | ✅ Completado | Dic 2024 | PDFs legalmente válidos con QuestPDF |
| Integración en Clientes | ✅ Completado | Dic 2024 | RGPD opcional, Imágenes opcional, firma desde lista |
| Integración en Trabajos | ✅ Completado | Dic 2024 | Consentimiento por trabajo, avisos no bloqueantes y bloqueo de edición tras firma |
| Vista de consentimientos | ✅ Completado | Dic 2024 | Lista global con filtros por cliente y tipo |
| Gestión de consentimientos | ✅ Completado | Dic 2024 | Ver, exportar y enviar por email |
| Almacenamiento de documentos | ✅ Completado | Dic 2024 | PDFs en %LOCALAPPDATA%\InkStudio\ficheros\clientes\{id}\consentimientos\ |

**Ver plan detallado:** `documentacion/03-plan-consentimientos.md`

### Emails

| Tarea | Estado | Notas |
|-------|--------|-------|
| Servicio de envío de emails | ⏳ Pendiente | |
| Plantillas de email | ⏳ Pendiente | |
| Email de confirmación de cita | ⏳ Pendiente | |
| Configuración SMTP en UI | ⏳ Pendiente | |

### Backup y Restauración ✅

| Tarea | Estado | Fecha | Notas |
|-------|--------|-------|-------|
| BackupService.cs | ✅ Completado | Dic 2024 | Servicio completo de gestión de backups |
| RestauracionService.cs | ✅ Completado | Dic 2024 | Restauración con reintentos y validación |
| BackupViewModel y BackupView | ✅ Completado | Dic 2024 | Vista moderna con lista de backups |
| Detección servicios de nube | ✅ Completado | Dic 2024 | OneDrive, Google Drive, Dropbox |
| Copia automática a nube | ✅ Completado | Dic 2024 | Sincronización al crear backup |
| Rotación de backups | ✅ Completado | Dic 2024 | Eliminación de antiguos según configuración |
| Configuración de backup en BD | ✅ Completado | Dic 2024 | Frecuencia, hora, retención, servicio nube |
| Modelos InfoBackup, BackupMetadata | ✅ Completado | Dic 2024 | Modelos para UI y metadatos en ZIP |

### Seguridad (fase posterior)

| Tarea | Estado | Notas |
|-------|--------|-------|
| Login de usuario | ⏳ Pendiente | |
| Cifrado de BD (SQLCipher) | ⏳ Pendiente | |
| Cifrado de campos sensibles | ⏳ Pendiente | |

---

## 📦 Fase 4: Pulido y Distribución (PENDIENTE)

### UX/UI

| Tarea | Estado | Fecha |
|-------|--------|-------|
| Diseño moderno con degradados | ✅ Completado | Dic 2024 |
| Estilos globales de botones | ✅ Completado | Dic 2024 |
| Modales con efecto glow | ✅ Completado | Dic 2024 |
| Paleta de colores morado/naranja | ✅ Completado | Dic 2024 |
| Botones con degradados verdes | ✅ Completado | Dic 2024 |
| Scrollbars personalizados | ✅ Completado | Dic 2024 |
| ComboBox mejorados | ✅ Completado | Dic 2024 |
| Animaciones y transiciones | ⏳ Pendiente | |
| Iconos personalizados | ⏳ Pendiente | |

### Testing

| Tarea | Estado | Notas |
|-------|--------|-------|
| Tests unitarios | ⏳ Pendiente | |
| Tests de integración | ⏳ Pendiente | |
| Testing manual | ⏳ Pendiente | |

### Distribución

| Tarea | Estado | Notas |
|-------|--------|-------|
| Crear instalador | ⏳ Pendiente | |
| Icono de aplicación | ⏳ Pendiente | |
| Splash screen | ⏳ Pendiente | |
| Auto-actualización (opcional) | ⏳ Pendiente | |

---

## 📝 Historial de Cambios

### Diciembre 2024

**Semana 1:**
- ✅ Proyecto inicial creado
- ✅ Configuración de .NET 9 y Avalonia 11.3.9
- ✅ Modelos de datos creados (Cliente, Cita, Trabajo, Consentimiento, Configuracion)
- ✅ DbContext configurado con relaciones
- ✅ Migración inicial aplicada
- ✅ Dashboard implementado con:
  - Tarjetas de estadísticas
  - Lista de citas del día
  - Alertas y pendientes
  - Navegación lateral
- ✅ Documentación completa creada
- ✅ Repositorio Git inicializado y subido a GitHub
- ✅ **Módulo de Clientes implementado:**
  - ClientesViewModel con CRUD completo
  - ClientesView con lista y formulario de edición
  - Búsqueda por nombre, teléfono, email
  - Soft delete (desactivación)
  - XML Documentation Comments (estilo Javadoc)

**Semana 2-3:**
- ✅ **Módulo de Agenda implementado:**
  - AgendaViewModel con CRUD completo de citas
  - AgendaView con calendario semanal
  - Modal de creación/edición de citas
  - Asociación de citas a clientes y trabajos
  - Cambio de estado de citas
  - Crear trabajo desde modal de cita
- ✅ **Módulo de Trabajos implementado:**
  - TrabajosViewModel con CRUD completo
  - TrabajosView con lista y búsqueda
  - Modal de creación/edición de trabajos
  - Filtro por cliente
  - Pre-selección de cliente desde cita
- ✅ **Sistema de Logs implementado:**
  - LogsViewModel y LogsView
  - Visualización de logs del sistema
  - Exportación de logs
  - Integración con Serilog
- ✅ **Mejoras visuales:**
  - Diseño moderno con degradados morado/naranja
  - Modales con efecto glow y sombras
  - Botones con degradados verdes modernos
  - Estilos globales mejorados
  - Scrollbars y ComboBox personalizados

**Semana 4:**
- ✅ **Plan de Consentimientos creado:**
  - Plan detallado de implementación
  - Arquitectura de sistema de firma dual (móvil/PC)
  - Definición de componentes y fases
  - Documentación completa del módulo
- ✅ **Módulo de Consentimientos (Fases 1-8 completadas):**
  - Fases 1-7 completadas (Infraestructura, Servidor HTTP, Página Web, QR Codes, PDFs, ViewModel/View, Integración en Clientes)
  - Fase 8 completada: Integración en Trabajos (consentimiento por trabajo, avisos no bloqueantes, bloqueo de edición tras firma, integración en Agenda)
  - RGPD e Imágenes opcionales al crear cliente (pueden firmarse después)
  - Firma desde ficha de cliente, lista de trabajos y agenda
  - Indicadores visuales de estado en clientes, trabajos y agenda
  - Vista global de consentimientos añadida al menú lateral
- ✅ **Mejoras en Agenda/Calendario:**
  - Calendario semanal personalizado con grid moderno
  - Drag and drop de citas (arrastrar para mover)
  - Redimensionado de citas (arrastrar borde inferior)
  - Detección automática de superposiciones (completas y parciales)
  - Visualización lado a lado estilo Teams cuando hay superposiciones
  - Preview mejorada que muestra posición final dividida durante arrastre
  - Menú contextual (click derecho) para seleccionar citas superpuestas
  - Creación de citas arrastrando en huecos del calendario
  - Snapping de horas a intervalos de 30 minutos al crear y redimensionar citas
  - Header con mes y año visible en la agenda
  - Fines de semana destacados con color más claro (sábado y domingo)
- ✅ **Mejoras en Gestión de Clientes:**
  - Calendario de fecha de nacimiento mejorado con header de mes/año
  - Fines de semana destacados en el calendario
  - Días de otros meses con opacidad reducida
  - Validación de DNI único y obligatorio
  - Teléfono opcional (no único)
  - Navegación a trabajos desde ficha de cliente (overlay)
- ✅ **Módulo de Configuración:**
  - ConfiguracionViewModel y ConfiguracionView implementados
  - Datos del estudio hardcodeados inicialmente (Estudio Erzulie)
  - Configuración SMTP diferida para más adelante
- ✅ **Fotos de Trabajos:**
  - Captura de fotos "antes" y "después" desde móvil
  - Almacenamiento en `ficheros/clientes/{id}/trabajos/{trabajoId}/`
  - Visualización en modal de trabajo
  - Validación de consentimientos antes de capturar

**Bugs corregidos:**
- ✅ SQLite no soporta OrderBy con TimeSpan → Ordenamiento en memoria
- ✅ ProgressRing no disponible en Avalonia estándar → Reemplazado por emoji
- ✅ BoxShadow no disponible en Button → Eliminado de estilos de botones
- ✅ Archivo de log bloqueado por Serilog → Implementada lectura desde final del archivo

**Semana 5 (29-30 Diciembre):**
- ✅ **Sistema de Backup y Restauración completado:**
  - `BackupService.cs` - Creación de backups completos (BD + ficheros)
  - `RestauracionService.cs` - Restauración con reintentos y validación
  - `BackupViewModel` y `BackupView` - Vista completa de gestión
  - Detección automática de servicios de nube (OneDrive, Google Drive, Dropbox)
  - Copia automática a nube al crear backup
  - Rotación automática de backups antiguos
  - Configuración guardada en BD (frecuencia, hora, retención)
  - Modelos `InfoBackup`, `BackupMetadata`, `InfoServicioNube`
  - ZIP con base de datos, ficheros, metadata y README
  - Uso de VACUUM INTO para evitar bloqueos de SQLite
  - Campos de backup añadidos a modelo `Configuracion`
- ✅ **Migración AddBackupFieldsToConfiguracion** aplicada

**Bugs corregidos (Semana 5):**
- ✅ Bloqueo de BD durante backup → VACUUM INTO para copia limpia
- ✅ Archivos WAL/SHM bloqueados → Reintentos con fallback

---

## 🎯 Próximos Pasos Inmediatos

1. [ ] Implementar configuración SMTP (diferido)
2. [ ] Implementar tema claro/oscuro (diferido)
3. [x] ~~Implementar backup y restauración de base de datos~~ ✅ Completado
4. [ ] Crear instalador de la aplicación
5. [ ] Implementar sistema de emails (confirmación de citas, etc.)
6. [ ] Mejoras y pulido final (Fase 10 de Consentimientos)
7. [ ] Implementar seguridad (login, cifrado de BD)

---

> **Nota:** Este documento se actualizará conforme avance el desarrollo del proyecto.

