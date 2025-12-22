# Roadmap del Proyecto

> **Última actualización:** Diciembre 2024  
> **Estado general:** En desarrollo - Fase 2 Completada (50% del proyecto)

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
Fase 3: Funcionalidades      [██████░░░░░░░░░░░░░░]  30%
Fase 4: Distribución         [░░░░░░░░░░░░░░░░░░░░]   0%
─────────────────────────────────────────────────────────
Total del proyecto           [████████████░░░░░░░░]  55%
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
| Subir fotos del trabajo | ⏳ Pendiente | Funcionalidad futura |
| Galería de fotos | ⏳ Pendiente | Funcionalidad futura |

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

| Tarea | Estado | Notas |
|-------|--------|-------|
| ConfiguracionViewModel | ⏳ Pendiente | |
| ConfiguracionView | ⏳ Pendiente | |
| Datos del estudio | ⏳ Pendiente | |
| Configuración SMTP | ⏳ Pendiente | |
| Tema claro/oscuro | ⏳ Pendiente | |

---

## 📌 Fase 3: Funcionalidades Adicionales (EN PROGRESO)

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
| Integración en Trabajos | ⏳ Pendiente | | Consentimiento por trabajo (Fase 8) |
| Vista de consentimientos | ⏳ Pendiente | | Lista y gestión (Fase 9) |
| Gestión de consentimientos | ⏳ Pendiente | | CRUD completo (Fase 9) |
| Almacenamiento de documentos | ✅ Completado | Dic 2024 | PDFs en %LOCALAPPDATA%\InkStudio\consentimientos\ |

**Ver plan detallado:** `documentacion/03-plan-consentimientos.md`

### Emails

| Tarea | Estado | Notas |
|-------|--------|-------|
| Servicio de envío de emails | ⏳ Pendiente | |
| Plantillas de email | ⏳ Pendiente | |
| Email de confirmación de cita | ⏳ Pendiente | |
| Configuración SMTP en UI | ⏳ Pendiente | |

### Backup y Restauración

| Tarea | Estado | Notas |
|-------|--------|-------|
| Exportar base de datos | ⏳ Pendiente | |
| Importar base de datos | ⏳ Pendiente | |
| Backup automático | ⏳ Pendiente | |

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
- ✅ **Módulo de Consentimientos (75% completado):**
  - Fases 1-7 completadas (Infraestructura, Servidor HTTP, Página Web, QR Codes, PDFs, ViewModel/View, Integración en Clientes)
  - RGPD opcional al crear cliente
  - Firma desde lista de clientes
  - Indicadores visuales de estado
  - Pendiente: Integración en Trabajos (Fase 8) y Vista de Consentimientos (Fase 9)
- ✅ **Mejoras en Agenda/Calendario:**
  - Calendario semanal personalizado con grid moderno
  - Drag and drop de citas (arrastrar para mover)
  - Redimensionado de citas (arrastrar borde inferior)
  - Detección automática de superposiciones (completas y parciales)
  - Visualización lado a lado estilo Teams cuando hay superposiciones
  - Preview mejorada que muestra posición final dividida durante arrastre
  - Menú contextual (click derecho) para seleccionar citas superpuestas
  - Badge naranja con contador de citas superpuestas

**Bugs corregidos:**
- ✅ SQLite no soporta OrderBy con TimeSpan → Ordenamiento en memoria
- ✅ ProgressRing no disponible en Avalonia estándar → Reemplazado por emoji
- ✅ BoxShadow no disponible en Button → Eliminado de estilos de botones
- ✅ Archivo de log bloqueado por Serilog → Implementada lectura desde final del archivo

---

## 🎯 Próximos Pasos Inmediatos

1. [ ] Implementar módulo de Configuración
2. [ ] Implementar gestión de consentimientos
3. [ ] Implementar subida de fotos para trabajos
4. [ ] Implementar galería de fotos
5. [ ] Crear instalador de la aplicación
6. [ ] Implementar backup y restauración de base de datos

---

> **Nota:** Este documento se actualizará conforme avance el desarrollo del proyecto.

