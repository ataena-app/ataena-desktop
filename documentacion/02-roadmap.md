# Roadmap del Proyecto

> **Última actualización:** 21 de Febrero 2026  
> **Estado general:** En desarrollo - Fase 4 en progreso (~85% del proyecto)

---

## 📋 Índice

1. [Resumen de Progreso](#-resumen-de-progreso)
2. [Fase 1: Fundamentos](#-fase-1-fundamentos-completada)
3. [Fase 2: Módulos Principales](#-fase-2-módulos-principales-en-progreso)
4. [Fase 3: Funcionalidades Adicionales](#-fase-3-funcionalidades-adicionales-pendiente)
5. [Fase 4: Pulido y Distribución](#-fase-4-pulido-y-distribución-pendiente)
6. [Historial de Cambios](#-historial-de-cambios)
7. [Ruta Completa - Lo que queda](#-ruta-completa---lo-que-queda)

> 📔 **Diario de desarrollo:** Ver `06-diario-desarrollo.md` para registro cronológico detallado.

---

## 📊 Resumen de Progreso

```
Fase 1: Fundamentos          [████████████████████] 100%
Fase 2: Módulos Principales   [████████████████████] 100%
Fase 3: Funcionalidades      [████████████████████] 100%
Fase 4: Distribución         [████████░░░░░░░░░░░░]  40%
─────────────────────────────────────────────────────────
Total del proyecto            [█████████████████░░░]  85%
```

---

## ✅ Fase 1: Fundamentos (COMPLETADA)

### Configuración del proyecto

| Tarea | Estado | Fecha |
|-------|--------|-------|
| Crear proyecto Avalonia | ✅ Completado | Dic 2025 |
| Configurar .NET 9 | ✅ Completado | Dic 2025 |
| Añadir paquetes NuGet (FluentAvalonia, EF Core, etc.) | ✅ Completado | Dic 2025 |
| Configurar estructura de carpetas | ✅ Completado | Dic 2025 |

### Modelos de datos

| Tarea | Estado | Fecha |
|-------|--------|-------|
| Modelo Cliente | ✅ Completado | Dic 2025 |
| Modelo Cita | ✅ Completado | Dic 2025 |
| Modelo Trabajo | ✅ Completado | Dic 2025 |
| Modelo Consentimiento | ✅ Completado | Dic 2025 |
| Modelo Configuracion | ✅ Completado | Dic 2025 |
| Enumeraciones (EstadoCita, TipoCita, etc.) | ✅ Completado | Dic 2025 |

### Base de datos

| Tarea | Estado | Fecha |
|-------|--------|-------|
| Crear DbContext | ✅ Completado | Dic 2025 |
| Configurar relaciones entre tablas | ✅ Completado | Dic 2025 |
| Crear migración inicial | ✅ Completado | Dic 2025 |
| Aplicar migración | ✅ Completado | Dic 2025 |

### Documentación

| Tarea | Estado | Fecha |
|-------|--------|-------|
| Documento de idea general | ✅ Completado | Dic 2025 |
| Documentación de base de datos | ✅ Completado | Dic 2025 |
| Guías de desarrollo | ✅ Completado | Dic 2025 |

---

## ✅ Fase 2: Módulos Principales (COMPLETADA)

### Dashboard

| Tarea | Estado | Fecha |
|-------|--------|-------|
| DashboardViewModel con lógica | ✅ Completado | Dic 2025 |
| DashboardView con diseño | ✅ Completado | Dic 2025 |
| Tarjetas de estadísticas | ✅ Completado | Dic 2025 |
| Lista de citas del día | ✅ Completado | Dic 2025 |
| Sección de alertas | ✅ Completado | Dic 2025 |
| Botones de acciones rápidas | ✅ Completado | Dic 2025 |
| Navegación lateral | ✅ Completado | Dic 2025 |

### Gestión de Clientes

| Tarea | Estado | Fecha |
|-------|--------|-------|
| ClientesViewModel | ✅ Completado | Dic 2025 |
| ClientesView (lista) | ✅ Completado | Dic 2025 |
| CRUD de clientes | ✅ Completado | Dic 2025 |
| Búsqueda de clientes | ✅ Completado | Dic 2025 |
| Modal de edición/creación | ✅ Completado | Dic 2025 |
| Soft delete (desactivación) | ✅ Completado | Dic 2025 |
| Diseño moderno con modales | ✅ Completado | Dic 2025 |

### Agenda / Citas

| Tarea | Estado | Fecha |
|-------|--------|-------|
| AgendaViewModel | ✅ Completado | Dic 2025 |
| AgendaView (calendario) | ✅ Completado | Dic 2025 |
| Vista semana | ✅ Completado | Dic 2025 |
| Crear/editar citas | ✅ Completado | Dic 2025 |
| Cambiar estado de citas | ✅ Completado | Dic 2025 |
| Asociar cita a cliente | ✅ Completado | Dic 2025 |
| Asociar trabajo a cita | ✅ Completado | Dic 2025 |
| Modal de creación/edición | ✅ Completado | Dic 2025 |
| Crear trabajo desde cita | ✅ Completado | Dic 2025 |
| Diseño moderno con modales | ✅ Completado | Dic 2025 |
| Calendario semanal personalizado | ✅ Completado | Dic 2025 |
| Drag and drop de citas | ✅ Completado | Dic 2025 |
| Redimensionado de citas | ✅ Completado | Dic 2025 |
| Detección de superposiciones | ✅ Completado | Dic 2025 |
| Visualización lado a lado (Teams-style) | ✅ Completado | Dic 2025 |
| Preview mejorada durante arrastre | ✅ Completado | Dic 2025 |
| Menú contextual para citas superpuestas | ✅ Completado | Dic 2025 |
| Vista de mes (6 semanas) | ✅ Completado | Feb 2026 | Calendario mensual con resumen de citas |
| Días festivos (API Nager.Date) | ✅ Completado | Feb 2026 | Nacionales, autonómicos, locales Guadalajara |
| Aviso consentimiento RGPD en citas | ✅ Completado | Feb 2026 | Icono warning si cliente sin RGPD |

### Trabajos / Galería

| Tarea | Estado | Fecha |
|-------|--------|-------|
| TrabajosViewModel | ✅ Completado | Dic 2025 |
| TrabajosView (lista) | ✅ Completado | Dic 2025 |
| CRUD de trabajos | ✅ Completado | Dic 2025 |
| Búsqueda de trabajos | ✅ Completado | Dic 2025 |
| Filtro por cliente | ✅ Completado | Dic 2025 |
| Asociar trabajo a cliente | ✅ Completado | Dic 2025 |
| Modal de creación/edición | ✅ Completado | Dic 2025 |
| Pre-selección de cliente desde cita | ✅ Completado | Dic 2025 |
| Diseño moderno con modales | ✅ Completado | Dic 2025 |
| Subir fotos del trabajo | ✅ Completado | Dic 2025 | Fotos "antes" y "después" con captura desde móvil |
| Galería de fotos | ✅ Completado | Dic 2025 | Visualización de fotos en modal de trabajo |

### Sistema de Logs

| Tarea | Estado | Fecha |
|-------|--------|-------|
| LogsViewModel | ✅ Completado | Dic 2025 |
| LogsView | ✅ Completado | Dic 2025 |
| Visualización de logs | ✅ Completado | Dic 2025 |
| Exportar logs | ✅ Completado | Dic 2025 |
| Cargar logs desde archivo | ✅ Completado | Dic 2025 |
| Integración con Serilog | ✅ Completado | Dic 2025 |

### Configuración

| Tarea | Estado | Fecha | Notas |
|-------|--------|-------|-------|
| ConfiguracionViewModel | ✅ Completado | Dic 2025 | |
| ConfiguracionView | ✅ Completado | Dic 2025 | |
| Datos del estudio | ✅ Completado | Dic 2025 | Configuración inicial hardcodeada (Estudio Erzulie) |
| Configuración SMTP | ✅ Completado | Feb 2026 | UI completa, contraseña con espacios automáticos |
| Botón probar conexión SMTP | ✅ Completado | Feb 2026 | |
| Tema claro/oscuro | ⏳ Pendiente | | Diferido para más adelante |

---

## ✅ Fase 3: Funcionalidades Adicionales (COMPLETADA)

### Consentimientos

| Tarea | Estado | Fecha | Notas |
|-------|--------|-------|-------|
| Plan de implementación | ✅ Completado | Dic 2025 | Plan detallado creado |
| Arquitectura definida | ✅ Completado | Dic 2025 | Sistema de firma dual (móvil/PC) |
| Infraestructura base | ✅ Completado | Dic 2025 | Librerías instaladas, carpetas creadas |
| Servidor HTTP local | ✅ Completado | Dic 2025 | Servidor HTTP con firewall automático |
| Página web de firma (móvil) | ✅ Completado | Dic 2025 | HTML/JS/CSS responsive para móvil |
| Generación de QR codes | ✅ Completado | Dic 2025 | QR codes generados automáticamente |
| Generación de PDFs | ✅ Completado | Dic 2025 | PDFs legalmente válidos con QuestPDF |
| Integración en Clientes | ✅ Completado | Dic 2025 | RGPD opcional, Imágenes opcional, firma desde lista |
| Integración en Trabajos | ✅ Completado | Dic 2025 | Consentimiento por trabajo, avisos no bloqueantes y bloqueo de edición tras firma |
| Vista de consentimientos | ✅ Completado | Dic 2025 | Lista global con filtros por cliente y tipo |
| Gestión de consentimientos | ✅ Completado | Dic 2025 | Ver, exportar y enviar por email |
| Almacenamiento de documentos | ✅ Completado | Dic 2025 | PDFs en %LOCALAPPDATA%\InkStudio\ficheros\clientes\{id}\consentimientos\ |
| Menores y tutores | ✅ Completado | Feb 2026 | RGPD_Menor, Trabajo_Menor, doble firma, datos tutor |
| Renovación de consentimientos | ✅ Completado | Feb 2026 | Histórico, antigüedad, aviso ≥2 años |
| Plantillas RGPD actualizadas | ✅ Completado | Feb 2026 | lgpd.txt, NOMBRE_EMPRESA vs NOMBRE_ESTUDIO |
| Foto DNI cliente y tutor | ✅ Completado | Feb 2026 | QR + subida desde PC |

**Ver plan detallado:** `documentacion/03-plan-consentimientos.md`

### Emails

| Tarea | Estado | Fecha | Notas |
|-------|--------|-------|-------|
| EmailService.cs | ✅ Completado | Feb 2026 | Envío SMTP con limpieza de espacios en contraseña |
| Plantilla RecordatorioCita.html | ✅ Completado | Feb 2026 | HTML responsive con datos de cita |
| Email de recordatorio de cita | ✅ Completado | Feb 2026 | Botón "Recordar" en tarjetas de cita |
| Registro EmailEnviado en Cita | ✅ Completado | Feb 2026 | Ya existía en modelo |

### Backup y Restauración ✅

| Tarea | Estado | Fecha | Notas |
|-------|--------|-------|-------|
| BackupService.cs | ✅ Completado | Dic 2025 | Servicio completo de gestión de backups |
| RestauracionService.cs | ✅ Completado | Dic 2025 | Restauración con reintentos y validación |
| BackupViewModel y BackupView | ✅ Completado | Dic 2025 | Vista moderna con lista de backups |
| Detección servicios de nube | ✅ Completado | Dic 2025 | OneDrive, Google Drive, Dropbox |
| Copia automática a nube | ✅ Completado | Dic 2025 | Sincronización al crear backup |
| Rotación de backups | ✅ Completado | Dic 2025 | Eliminación de antiguos según configuración |
| Configuración de backup en BD | ✅ Completado | Dic 2025 | Frecuencia, hora, retención, servicio nube |
| Modelos InfoBackup, BackupMetadata | ✅ Completado | Dic 2025 | Modelos para UI y metadatos en ZIP |

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
| Diseño moderno con degradados | ✅ Completado | Dic 2025 |
| Estilos globales de botones | ✅ Completado | Dic 2025 |
| Modales con efecto glow | ✅ Completado | Dic 2025 |
| Paleta de colores morado/naranja | ✅ Completado | Dic 2025 |
| Botones con degradados verdes | ✅ Completado | Dic 2025 |
| Scrollbars personalizados | ✅ Completado | Dic 2025 |
| ComboBox mejorados | ✅ Completado | Dic 2025 |
| Animaciones y transiciones | ⏳ Pendiente | |
| Iconos personalizados | ⏳ Pendiente | |

### Testing

| Tarea | Estado | Notas |
|-------|--------|-------|
| Tests unitarios | ⏳ Pendiente | |
| Tests de integración | ⏳ Pendiente | |
| Testing manual | ⏳ Pendiente | |

### Distribución

| Tarea | Estado | Fecha | Notas |
|-------|--------|-------|-------|
| Crear instalador | ⏳ Pendiente | | |
| Icono de aplicación | ⏳ Pendiente | | |
| Splash screen | ✅ Completado | Feb 2026 | Logo configurable desde Configuración |
| Auto-actualización (opcional) | ⏳ Pendiente | | |

---

## 📝 Historial de Cambios

### Diciembre 2025

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

**Enero - Febrero 2026:**
- ✅ Identificación de menores por fecha nacimiento
- ✅ Datos tutor obligatorios (nombre, DNI, teléfono)
- ✅ Consentimientos RGPD_Menor y Trabajo_Menor con doble firma
- ✅ Indicador menor en clientes, citas y agenda
- ✅ Aviso renovación cuando cliente cumple 18 años
- ✅ Antigüedad de consentimientos (aviso ≥2 años)
- ✅ Histórico y renovación de consentimientos

**Agenda:**
- ✅ Vista de mes (6 semanas) con resumen de citas
- ✅ API festivos (Nager.Date) nacionales, autonómicos, locales
- ✅ Aviso RGPD en tarjetas de cita si cliente sin consentimiento

**Clientes:**
- ✅ Foto DNI cliente y tutor (QR + subida desde PC)
- ✅ NombreEmpresa vs NombreEstudio en Configuración
- ✅ Plantillas RGPD actualizadas (lgpd.txt)

**Emails:**
- ✅ EmailService con SMTP
- ✅ Recordatorio de cita con plantilla HTML
- ✅ Botón "Recordar" en citas, registro de envío
- ✅ Contraseña SMTP: limpieza automática de espacios (Google)

**UI/UX:**
- ✅ Splash screen con logo configurable
- ✅ Botón Guardar configuración (texto centrado, padding)

---

## 🎯 Próximos Pasos Inmediatos

1. [x] ~~Implementar configuración SMTP~~ ✅ Completado
2. [x] ~~Implementar sistema de emails (recordatorio de citas)~~ ✅ Completado
3. [x] ~~Splash screen con logo~~ ✅ Completado
4. [ ] Implementar tema claro/oscuro (diferido)
5. [ ] Crear instalador de la aplicación
6. [ ] Animaciones y transiciones (UX)
7. [ ] Implementar seguridad (login, cifrado de BD)

---

## 🗺️ Ruta Completa - Lo que queda

### Prioridad Alta
| # | Tarea | Fase | Esfuerzo |
|---|-------|------|----------|
| 1 | Crear instalador (Inno Setup o Velopack) | 4 | Alto |
| 2 | Icono de aplicación profesional | 4 | Bajo |
| 3 | Testing manual completo | 4 | Medio |

### Prioridad Media
| # | Tarea | Fase | Esfuerzo |
|---|-------|------|----------|
| 4 | Tema claro/oscuro | 4 | Medio |
| 5 | Animaciones y transiciones | 4 | Bajo |
| 6 | Auto-actualización (opcional) | 4 | Alto |

### Prioridad Baja / Futuro
| # | Tarea | Fase | Esfuerzo |
|---|-------|------|----------|
| 7 | Tests unitarios | 4 | Alto |
| 8 | Tests de integración | 4 | Alto |
| 9 | Login de usuario | Seguridad | Alto |
| 10 | Cifrado de BD (SQLCipher) | Seguridad | Alto |
| 11 | Cifrado de campos sensibles | Seguridad | Medio |

### Resumen de lo pendiente
- **Para MVP/distribución:** Instalador + icono + testing manual
- **Pulido:** Tema claro/oscuro, animaciones
- **Opcional:** Auto-update, tests automatizados, seguridad

---

> **Nota:** Este documento se actualiza conforme avance el desarrollo del proyecto.

