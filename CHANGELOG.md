# Changelog

Todos los cambios notables de Ataena CRM se documentan en este archivo.  
El formato sigue [Keep a Changelog](https://keepachangelog.com/es/1.0.0/).

---

## [0.5.5] - 2026-05-18

### Añadido
- **Consentimiento de imágenes para menores** (`Imagenes_Menor`): doble firma tutor + menor en un solo QR y PDF.
- **Chips en lista de clientes**: RGPD, imágenes y menor; botón **Ver DNI** en ficha y modal de foto.
- **Changelog / Novedades**: se muestra al actualizar (una vez por versión) y se puede consultar desde el menú **Novedades** o pulsando la versión en la barra lateral.

### Cambiado
- Firma en móvil: dos cajas en la misma página, dibujo estable en pantalla táctil.
- Formulario y PDF de consentimiento de **trabajo** sin precio ni duración estimada; firmas más compactas en el PDF.
- Búsqueda de **trabajos** por teléfono del cliente; botones de consentimiento sin texto cortado.

### Arreglado
- Mensajes y validaciones de fotos de trabajo alineados con consentimientos vigentes (RGPD, imágenes, trabajo).
- Cliente puede indicar que no quiere fotos de trabajo (`PermiteFotosTrabajo`).

---

## [0.5.4] - 2026-05-13

### Cambiado
- Icono de marca unificado (`Assets/favicon.ico`): ventana principal, ejecutable y asistente de instalación (Inno Setup).

---

## [0.5.3] - 2026-05-13

### Arreglado
- La aplicación ahora se relanza automáticamente al terminar una actualización silenciosa. Antes era necesario abrirla manualmente tras actualizar.

---

## [0.5.2] - 2026-05-13

### Añadido
- **Rework completo del dashboard**: nuevo diseño moderno con tarjetas de bordes degradados (morado/índigo para clientes, verde/cian para citas, naranja/rojo para pendientes, dorado para economía).
- Layout de **timeline** en la sección de citas del día: hora destacada, punto de timeline con degradado y tarjeta de cita con fondo oscuro.
- Acciones rápidas rediseñadas con icono en burbuja coloreada y borde degradado por acción.
- Línea de acento morado→naranja en la cabecera del dashboard.
- Pill de fecha con borde degradado.
- El título de la ventana principal ahora muestra dinámicamente el nombre del estudio configurado (en lugar de estar hardcodeado).

### Arreglado
- El comprobador de actualizaciones ahora usa el endpoint `/releases` de GitHub en lugar de `/releases/latest`, lo que permite detectar correctamente versiones marcadas como prerelease.

---

## [0.5.1] - 2026-05-13

### Añadido
- **Personalización del dashboard**: nuevas opciones en Configuración para mostrar u ocultar cada sección del dashboard (estadísticas, economía, alertas, acciones rápidas). Los cambios se persisten en base de datos.
- La tarjeta de ingresos económicos solo se puede activar si las estadísticas generales están habilitadas.

### Arreglado
- La versión mostrada en la barra lateral ahora incluye el número de parche completo en versiones beta (p. ej. `v0.5.1 Beta` en lugar de `v0.5 Beta`), evitando confusión al actualizar.

---

## [0.5.0] - 2026-05-13

### Primera versión beta pública

#### Añadido
- **Dashboard** con saludo personalizado, fecha actual, estadísticas rápidas (clientes, citas del día, citas por confirmar), lista de citas de hoy y panel de alertas/pendientes.
- **Gestión de clientes**: alta, edición, búsqueda y archivo de clientes.
- **Agenda**: calendario de citas con vista diaria y semanal, creación y edición de citas.
- **Trabajos**: registro y seguimiento de trabajos por cliente.
- **Consentimientos RGPD**: generación, firma y archivo de consentimientos informados en PDF.
- **Configuración**: nombre del estudio, dirección, preferencias de dashboard.
- **Actualizaciones automáticas**: la app consulta GitHub Releases al arrancar y ofrece actualizar con un solo clic.
- **Backup y restauración** de la base de datos desde la pantalla de configuración.
- Tema oscuro moderno con paleta morado/naranja.
- Instalador de Windows generado con Inno Setup (`Ataena-Setup-X.Y.Z.exe`).

#### Técnico
- Avalonia UI 11 con MVVM (CommunityToolkit.Mvvm).
- SQLite + Entity Framework Core 9.
- Serilog para diagnóstico y logs de arranque.
- Output type `WinExe` (sin ventana de consola al arrancar).

---

[0.5.5]: https://github.com/Jvalfdev/desktop-myos-app/releases/tag/v0.5.5
[0.5.4]: https://github.com/Jvalfdev/desktop-myos-app/releases/tag/v0.5.4
[0.5.3]: https://github.com/Jvalfdev/desktop-myos-app/releases/tag/v0.5.3
[0.5.2]: https://github.com/Jvalfdev/desktop-myos-app/releases/tag/v0.5.2
[0.5.1]: https://github.com/Jvalfdev/desktop-myos-app/releases/tag/v0.5.1
[0.5.0]: https://github.com/Jvalfdev/desktop-myos-app/releases/tag/v0.5.0
