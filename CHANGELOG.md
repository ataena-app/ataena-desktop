# Changelog

Todos los cambios notables de Ataena CRM se documentan en este archivo.  
El formato sigue [Keep a Changelog](https://keepachangelog.com/es/1.0.0/).

---

## [0.6.5] - 2026-06-20

### Técnico
- **Migración a sistema cloud:** actualización interna del sistema de distribución y actualizaciones automáticas en preparación para Ataena Cloud.

---

## [0.6.4] - 2026-06-11

### Cambiado
- **Agenda — selector de fecha en citas:** calendario en popup flotante (ya no se recorta dentro del modal), con márgenes, cierre al elegir día y estilo visual alineado con la app (degradado morado/naranja, días redondeados, fines de semana resaltados).

---

## [0.6.3] - 2026-06-11

### Añadido
- **Agenda:** mini-calendario al elegir fecha de cita; aviso de solapamiento con detalle de la cita conflictiva antes de crear o mover; bloqueo de citas en el pasado; crear trabajo desde la cita y volver al modal con el trabajo vinculado; filtro por estado; click en día del mes para ir a vista día.
- **Configuración:** menú lateral por secciones (estudio, correo, impresora, dashboard, preferencias).
- **Lista de trabajos:** mismo estilo visual que clientes (tarjetas, barra de estado, chips); búsqueda automática insensible a acentos.
- **Consentimientos:** barra de búsqueda por nombre, apellidos, DNI o teléfono (insensible a acentos); lista rediseñada.
- **Clientes:** aviso modal centrado en rojo al guardar con campos obligatorios faltantes o incorrectos.

### Cambiado
- **DNI:** solo texto obligatorio (número de documento); eliminado flujo de foto/escáner/OCR del DNI y sección de normativa en configuración.
- **Trabajos:** al crear desde la ficha del cliente, el cliente queda pre-rellenado y bloqueado; sin estado «Diseño» en la lista (solo fecha).
- **Filtro «Sin RGPD»:** incluye correctamente menores (cuenta RGPD y RGPD de menor).

### Arreglado
- Etiquetas de hora en el calendario semanal al arrastrar citas.
- Búsqueda de trabajos y consentimientos con normalización de tildes.

---

## [0.6.2] - 2026-06-11

### Añadido
- **Foto de DNI (fase 1):** captura con móvil en página dedicada (`/foto-dni/`), subida desde PC y escáner; recorte automático del documento y mejora de legibilidad antes de guardar.

### Arreglado
- **Crash tras guardar la foto del DNI:** al terminar de guardar, la app recargaba toda la lista de clientes en un hilo de fondo y Avalonia lanzaba `Call from invalid thread` al actualizar la paginación. En la práctica parecía fallar al pulsar «Ver DNI», pero el cierre ocurría por esa recarga en segundo plano. Ahora la actualización va al hilo de la interfaz y solo sincroniza el cliente afectado.
- **Ver foto del DNI:** apertura más fiable con el visor predeterminado de Windows; el JPEG se escribe permitiendo lectura simultánea para la miniatura y el visor externo.

---

## [0.6.1] - 2026-06-11

### Añadido
- **Lista de clientes:** desplegable para ordenar por más reciente, más antiguo, nombre (A-Z) o edad; por defecto **más reciente** (último cliente añadido primero).
- **Búsqueda por DNI** e insensible a tildes (p. ej. `fernandez` encuentra «Fernández»); misma lógica en el selector de cliente de Trabajos.
- **Barra de estado** en cada tarjeta: verde (DNI + consentimientos de ficha al día), naranja (falta algo), rojo (sin RGPD).
- Comando de desarrollo `--seed-demo-clientes` para generar clientes de prueba en la BD local (no se distribuye con el instalador).

### Cambiado
- **Rediseño de la lista de clientes:** tarjetas con scroll, chips compactos y datos de contacto más legibles.
- **Búsqueda automática** al escribir (eliminado el botón «Buscar»); filtrado en memoria con retardo breve.

---

## [0.6.0] - 2026-05-29

### Añadido
- **Consentimientos:** acciones reducidas a **Ver**, **Renovar** y **Borrar** (ficha de cliente, trabajos y vista global). Al borrar, aviso en pantalla de que el cliente deja de tener ese consentimiento firmado.
- **PDF de consentimiento:** numeración «Página X de Y» al pie de cada hoja.
- **Fotos de trabajo:** botón **Subir desde PC** además de la cámara del móvil.
- **Validación de cliente:** indicadores en el propio campo (borde rojo y mensaje) para DNI, teléfono, email, fecha de nacimiento y datos del tutor.

### Cambiado
- **Firma en móvil:** hay que leer hasta el final (pasos 1 y 2) antes de marcar aceptación o firmar; sin checkbox de consentimiento en el modal del PC.
- Tras **generar el PDF**, el modal de firma se cierra y aparece aviso global «PDF guardado»; preview de firma recibida visible en el modal del PC.
- **Trabajos con consentimiento firmado:** solo las **notas internas** son editables; botón **Cerrar** en el editor; se puede **eliminar el trabajo** (con confirmación reforzada) borrando también consentimiento y PDF.
- **Renovar** consentimiento sustituye al anterior (firma nueva en modal); disponible siempre en consentimientos firmados, no solo cuando «necesita renovación».
- Campos obligatorios del cliente: **nombre, apellidos, DNI y fecha de nacimiento** (+ tutor si es menor); teléfono y email opcionales con validación de formato si se rellenan.
- Fecha de nacimiento: no se permiten fechas futuras (texto, calendario y al guardar).

### Arreglado
- **Dashboard:** ya no avisa de «falta RGPD» si el cliente tiene RGPD o RGPD de menor firmado.
- Tras añadir una foto en el modal de trabajo, la vista se actualiza sin cerrar el editor.
- Scroll del modal de trabajo y selector de cliente sin interferir con la rueda del ratón.

---

## [0.5.6] - 2026-05-19

### Arreglado
- Tras firmar el **RGPD** en la ficha del cliente, Trabajos ya no muestra por error que «debe tener RGPD» al abrir o firmar un trabajo (consulta fresca a BD en lugar de caché desactualizada).

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

[0.6.4]: https://github.com/Jvalfdev/desktop-myos-app/releases/tag/v0.6.4
[0.6.3]: https://github.com/Jvalfdev/desktop-myos-app/releases/tag/v0.6.3
[0.6.2]: https://github.com/Jvalfdev/desktop-myos-app/releases/tag/v0.6.2
[0.6.1]: https://github.com/Jvalfdev/desktop-myos-app/releases/tag/v0.6.1
[0.6.0]: https://github.com/Jvalfdev/desktop-myos-app/releases/tag/v0.6.0
[0.5.6]: https://github.com/Jvalfdev/desktop-myos-app/releases/tag/v0.5.6
[0.5.5]: https://github.com/Jvalfdev/desktop-myos-app/releases/tag/v0.5.5
[0.5.4]: https://github.com/Jvalfdev/desktop-myos-app/releases/tag/v0.5.4
[0.5.3]: https://github.com/Jvalfdev/desktop-myos-app/releases/tag/v0.5.3
[0.5.2]: https://github.com/Jvalfdev/desktop-myos-app/releases/tag/v0.5.2
[0.5.1]: https://github.com/Jvalfdev/desktop-myos-app/releases/tag/v0.5.1
[0.5.0]: https://github.com/Jvalfdev/desktop-myos-app/releases/tag/v0.5.0
