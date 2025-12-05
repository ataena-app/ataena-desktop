# Roadmap del Proyecto

> **Última actualización:** Diciembre 2024  
> **Estado general:** En desarrollo

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
Fase 2: Módulos Principales  [████████░░░░░░░░░░░░]  40%
Fase 3: Funcionalidades      [░░░░░░░░░░░░░░░░░░░░]   0%
Fase 4: Distribución         [░░░░░░░░░░░░░░░░░░░░]   0%
─────────────────────────────────────────────────────────
Total del proyecto           [███████░░░░░░░░░░░░░]  35%
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

## 🔨 Fase 2: Módulos Principales (EN PROGRESO)

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
| ClienteDetalleView (ficha) | ✅ Completado | Dic 2024 |
| CRUD de clientes | ✅ Completado | Dic 2024 |
| Búsqueda de clientes | ✅ Completado | Dic 2024 |
| Filtros (VIP, activos, etc.) | ⏳ Pendiente | |

### Agenda / Citas

| Tarea | Estado | Notas |
|-------|--------|-------|
| AgendaViewModel | ⏳ Pendiente | |
| AgendaView (calendario) | ⏳ Pendiente | |
| Vista día/semana/mes | ⏳ Pendiente | |
| Crear/editar citas | ⏳ Pendiente | |
| Cambiar estado de citas | ⏳ Pendiente | |
| Asociar cita a cliente | ⏳ Pendiente | |

### Trabajos / Galería

| Tarea | Estado | Notas |
|-------|--------|-------|
| TrabajosViewModel | ⏳ Pendiente | |
| TrabajosView (lista/galería) | ⏳ Pendiente | |
| Registrar nuevo trabajo | ⏳ Pendiente | |
| Subir fotos del trabajo | ⏳ Pendiente | |
| Asociar trabajo a cliente | ⏳ Pendiente | |
| Galería de fotos | ⏳ Pendiente | |

### Configuración

| Tarea | Estado | Notas |
|-------|--------|-------|
| ConfiguracionViewModel | ⏳ Pendiente | |
| ConfiguracionView | ⏳ Pendiente | |
| Datos del estudio | ⏳ Pendiente | |
| Configuración SMTP | ⏳ Pendiente | |
| Tema claro/oscuro | ⏳ Pendiente | |

---

## 📌 Fase 3: Funcionalidades Adicionales (PENDIENTE)

### Consentimientos

| Tarea | Estado | Notas |
|-------|--------|-------|
| Gestión de consentimientos | ⏳ Pendiente | |
| Generación de PDF | ⏳ Pendiente | |
| Firma digital (opcional) | ⏳ Pendiente | |
| Almacenamiento de documentos | ⏳ Pendiente | |

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

| Tarea | Estado | Notas |
|-------|--------|-------|
| Pulir diseño visual | ⏳ Pendiente | |
| Animaciones y transiciones | ⏳ Pendiente | |
| Responsive/adaptativo | ⏳ Pendiente | |
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
  - Marcado de clientes VIP
  - Soft delete (desactivación)
  - XML Documentation Comments (estilo Javadoc)

**Bugs corregidos:**
- ✅ SQLite no soporta OrderBy con TimeSpan → Ordenamiento en memoria
- ✅ ProgressRing no disponible en Avalonia estándar → Reemplazado por emoji

---

## 🎯 Próximos Pasos Inmediatos

1. [ ] Implementar módulo de Clientes (CRUD completo)
2. [ ] Implementar módulo de Agenda (calendario y citas)
3. [ ] Conectar botones de acciones rápidas del Dashboard
4. [ ] Implementar navegación real entre vistas

---

> **Nota:** Este documento se actualizará conforme avance el desarrollo del proyecto.

