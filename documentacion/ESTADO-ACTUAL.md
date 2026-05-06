# Estado Actual del Proyecto - Ataena CRM

> **Última actualización:** 21 de Febrero 2026  
> **Documento de referencia rápida** para ver qué está hecho y qué queda por hacer.

---

## 📊 Resumen en una línea

**El proyecto está ~90% completado.** Las funcionalidades core están implementadas. Para distribuir a usuarios falta: instalador, icono y testing manual.

---

## ✅ Lo que está hecho

### Funcionalidades completas
| Módulo | Estado | Notas |
|--------|--------|-------|
| Dashboard | ✅ | Estadísticas, citas del día, alertas |
| Clientes | ✅ | CRUD, búsqueda, soft delete, foto DNI |
| Agenda | ✅ | Vista día/semana/mes, drag & drop, festivos |
| Trabajos | ✅ | CRUD, fotos antes/después, galería |
| Consentimientos | ✅ | RGPD, Imágenes, Trabajo, Menores, firma móvil/PC |
| Configuración | ✅ | Datos estudio, SMTP, backup, escáner, impresora |
| Backup/Restauración | ✅ | Local + copia a nube (OneDrive, GDrive, Dropbox) |
| Logs | ✅ | Visualización, exportación |
| Emails | ✅ | Recordatorio de citas con plantilla HTML |
| Escáner DNI | ✅ | WIA, opcional desde Configuración |
| Impresión consentimientos | ✅ | PDF → impresora predeterminada |

### Infraestructura
- Base de datos SQLite en `%LOCALAPPDATA%\Ataena\`
- Servidor HTTP local para firma móvil (QR)
- Sistema de logging con Serilog
- Migraciones EF Core actualizadas

### UI/UX
- Diseño moderno (degradados, modales con glow)
- Splash screen con logo configurable
- Navegación lateral
- Responsive en modales

---

## ⏳ Lo que queda por hacer

### Para poder distribuir (prioridad alta)
| # | Tarea | Esfuerzo | Descripción |
|---|-------|----------|-------------|
| 1 | **Instalador** | Alto | Velopack. Ver `04-plan-distribucion.md` y `07-plan-licencias-actualizaciones.md` |
| 2 | **Actualizaciones automáticas** | Medio | Integrar Velopack en la app + GitHub Releases |
| 3 | **Icono de app** | Bajo | Sustituir `avalonia-logo.ico` por icono profesional |
| 4 | **Testing manual** | Medio | Probar a mano (sin código). Ver nota en `07-plan-licencias-actualizaciones.md` |

### Pulido (prioridad media)
| # | Tarea | Esfuerzo |
|---|-------|----------|
| 4 | Tema claro/oscuro | Medio |
| 5 | Animaciones y transiciones | Bajo |
| 6 | Auto-actualización (opcional) | Alto |

### Futuro (prioridad baja)
| # | Tarea |
|---|-------|
| 7 | Tests unitarios |
| 8 | Tests de integración |
| 9 | Login de usuario |
| 10 | Cifrado de BD (SQLCipher) |

---

## 📁 Estructura del proyecto

```
MYO_DESK/
├── Ataena/                 # Proyecto principal (.NET 9, Avalonia 11.3.9)
│   ├── Data/               # AtaenaDbContext, migraciones
│   ├── Models/             # Cliente, Cita, Trabajo, Consentimiento, Configuracion
│   ├── ViewModels/         # Lógica de cada pantalla
│   ├── Views/              # XAML de cada pantalla
│   ├── Services/           # Backup, Email, Logging, Escanner, Impresor, etc.
│   └── Ataena.csproj
└── documentacion/          # Documentación del proyecto
```

---

## 📚 Documentos relacionados

| Documento | Contenido |
|-----------|-----------|
| `02-roadmap.md` | Roadmap detallado con historial |
| `04-plan-distribucion.md` | Plan de instalador y distribución |
| `05-estudio-mercado-viabilidad.md` | Análisis de mercado y precios |
| `06-diario-desarrollo.md` | Registro cronológico de cambios |
| `07-plan-licencias-actualizaciones.md` | Plan de licencias, actualizaciones y checklist |
| `09-guia-instalador-velopack.md` | Guía paso a paso para crear el instalador con Velopack |

---

## 🚀 Siguiente paso recomendado

**Crear el instalador con Velopack.** Es el primer bloque: sin instalador no hay nada que entregar al tester. Velopack incluye soporte para actualizaciones, así que al integrarlo tendrás también el flujo de actualizaciones automáticas.

**Orden sugerido:** Instalador → Integrar actualizaciones en la app → Icono → Testing manual → Entregar al tester.
