# Diario de Desarrollo - Ataena CRM

> Registro cronológico de avances, decisiones y cambios del proyecto.

---

## Febrero 2026

### 21 Febrero (actualizado)
- **Emails de recordatorio:** Sistema completo implementado
  - EmailService con SMTP, plantilla HTML RecordatorioCita.html
  - Botón "Recordar" en tarjetas de cita (vista día)
  - Contraseña SMTP: limpieza automática de espacios para contraseñas de Google
  - Botón "Probar conexión SMTP" en Configuración
- **UI:** Botón Guardar configuración con MinWidth y Padding (texto centrado)
- **Documentación:** Roadmap actualizado, ruta completa de lo pendiente

### 21 Febrero (sesión tarde) - Renombrado del proyecto
- **InkStudio → Ataena:** Renombrado completo en todo el proyecto
  - Código: namespaces, AtaenaDbContext, rutas %LOCALAPPDATA%\Ataena\
  - Carpeta del proyecto, Ataena.csproj, AssemblyName
  - Documentación actualizada
  - Commit y push a GitHub

### Actualización documentación (estado del proyecto)
- **Roadmap:** Actualizado con estado ~90%, Escáner/Impresora documentados
- **Pendientes clarificados:** Instalador, icono y testing manual como prioridad para distribución

---

## Enero 2026

*(Resumen de trabajo realizado - ver 02-roadmap.md Historial para detalle)*

- Menores y consentimientos (RGPD_Menor, Trabajo_Menor, doble firma)
- Vista de mes en agenda, API festivos (Nager.Date)
- Foto DNI cliente y tutor
- NombreEmpresa vs NombreEstudio
- Plantillas RGPD actualizadas
- Splash screen con logo configurable

---

## Diciembre 2025

Ver sección **Historial de Cambios** en `02-roadmap.md` para el detalle completo de:
- Semanas 1-4: Proyecto inicial, Clientes, Agenda, Trabajos, Logs, Consentimientos
- Semana 5: Backup y Restauración

---

## Cómo usar este diario

- Añadir entrada al inicio de cada sesión de desarrollo significativa
- Formato: fecha, título breve, lista de cambios
- Enlazar commits de Git cuando proceda
