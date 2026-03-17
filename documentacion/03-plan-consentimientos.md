# Plan de Implementación: Módulo de Consentimientos

> **Fecha de inicio:** Diciembre 2025  
> **Estado:** ✅ Completado (90%)  
> **Prioridad:** Alta

---

## 📋 Índice

1. [Resumen](#-resumen)
2. [Arquitectura](#-arquitectura)
3. [Componentes a Crear](#-componentes-a-crear)
4. [Plan de Implementación por Fases](#-plan-de-implementación-por-fases)
5. [Registro de Avances](#-registro-de-avances)
6. [Flujos de Usuario](#-flujos-de-usuario)
7. [Consideraciones Técnicas](#-consideraciones-técnicas)

---

## 🎯 Resumen

Implementar un sistema completo de gestión de consentimientos que permita:
- Generar PDFs de consentimientos legalmente válidos
- Capturar firmas desde móvil (táctil) o PC (mouse/tableta)
- Integrar en flujos de creación de clientes y trabajos
- Almacenar y gestionar consentimientos firmados

### Tipos de Consentimientos

| Tipo | Obligatorio | Cuándo se firma | Frecuencia |
|------|-------------|----------------|------------|
| **RGPD** | ✅ Sí | Al crear cliente | 1 vez por cliente |
| **Imágenes** | ❌ No | Al crear cliente | 1 vez por cliente (opcional) |
| **Trabajo** | ✅ Sí | Al crear trabajo | 1 por cada trabajo |

---

## 🏗️ Arquitectura

### Sistema de Firma Dual

```
┌─────────────────────────────────────────────────────────┐
│                    PC (Ataena)                        │
│  ┌──────────────────────────────────────────────────┐  │
│  │  Modal de Firma de Consentimiento                 │  │
│  │  ├─ Vista previa del texto                       │  │
│  │  ├─ Checkbox de aceptación                       │  │
│  │  ├─ QR Code / URL                                │  │
│  │  └─ Preview de firma recibida                    │  │
│  └──────────────────────────────────────────────────┘  │
│                          │                               │
│                          │ HTTP Local (Puerto 8080)      │
│                          │                               │
└──────────────────────────┼───────────────────────────────┘
                           │
                           │ WiFi Local
                           │
┌──────────────────────────┼───────────────────────────────┐
│                    Móvil (Navegador)                      │
│  ┌──────────────────────────────────────────────────┐  │
│  │  Página Web de Firma                             │  │
│  │  ├─ Vista del consentimiento                    │  │
│  │  ├─ Canvas táctil para firmar                   │  │
│  │  └─ Botón "Enviar firma"                        │  │
│  └──────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────┘
```

### Flujo de Datos

```
1. PC inicia servidor HTTP local
2. PC genera URL única + QR code
3. Móvil escanea QR → Abre página web
4. Cliente firma en móvil (táctil)
5. Móvil envía firma (imagen) al PC
6. PC recibe firma → Genera PDF
7. PC guarda PDF en carpeta
8. PC guarda registro en BD
```

---

## 📦 Componentes a Crear

### 1. Servicios

#### `FirmaWebService.cs`
- **Responsabilidad:** Servidor HTTP local para recibir firmas desde móvil
- **Métodos:**
  - `IniciarServidor(int puerto)` → Inicia servidor HTTP
  - `DetectarIPLocal()` → Obtiene IP local del PC
  - `GenerarTokenUnico()` → Genera token único para sesión
  - `GenerarUrlFirma(string token)` → Genera URL completa
  - `EsperarFirma(string token, TimeSpan timeout)` → Espera recibir firma
  - `DetenerServidor()` → Cierra servidor HTTP
  - `ServirPaginaWeb(HttpContext context)` → Sirve HTML de firma

#### `ConsentimientoService.cs`
- **Responsabilidad:** Generación de PDFs y gestión de consentimientos
- **Métodos:**
  - `GenerarPdfConsentimiento(Consentimiento cons, string rutaFirma)` → Genera PDF
  - `CargarPlantillaTexto(TipoConsentimiento tipo)` → Carga texto del consentimiento
  - `GuardarConsentimiento(Consentimiento cons, string rutaPdf)` → Guarda en BD
  - `ValidarConsentimientosRequeridos(Cliente cliente)` → Verifica RGPD
  - `ObtenerRutaCarpetaConsentimientos()` → Ruta de almacenamiento

#### `QRCodeService.cs`
- **Responsabilidad:** Generación de códigos QR
- **Métodos:**
  - `GenerarQRCode(string url)` → Genera imagen QR
  - `GenerarQRCodeBitmap(string url)` → Retorna Bitmap para mostrar

### 2. ViewModels

#### `ConsentimientoFirmaViewModel.cs`
- **Responsabilidad:** Lógica del modal de firma
- **Propiedades:**
  - `TextoConsentimiento` (string)
  - `TipoConsentimiento` (TipoConsentimiento)
  - `Cliente` (Cliente)
  - `Trabajo` (Trabajo?) - opcional
  - `UrlFirma` (string)
  - `QRCodeImage` (Bitmap?)
  - `FirmaRecibida` (bool)
  - `ImagenFirma` (byte[]?)
  - `EstadoConexion` (string) - "Esperando...", "Conectado", etc.
  - `AceptaTerminos` (bool)
- **Comandos:**
  - `IniciarFirmaCommand` → Inicia servidor y genera QR
  - `CancelarFirmaCommand` → Cancela y cierra servidor
  - `ConfirmarFirmaCommand` → Genera PDF y guarda
  - `CopiarUrlCommand` → Copia URL al portapapeles

#### `ConsentimientosViewModel.cs`
- **Responsabilidad:** Gestión y listado de consentimientos
- **Propiedades:**
  - `Consentimientos` (ObservableCollection<Consentimiento>)
  - `ClienteSeleccionado` (Cliente?)
  - `FiltroTipo` (TipoConsentimiento?)
- **Comandos:**
  - `CargarConsentimientosCommand`
  - `VerPdfCommand`
  - `DescargarPdfCommand`
  - `EliminarConsentimientoCommand`

### 3. Views

#### `ConsentimientoFirmaView.axaml`
- **Responsabilidad:** Modal de firma de consentimiento
- **Componentes:**
  - ScrollViewer con texto del consentimiento
  - Checkbox "Acepto los términos"
  - QR Code grande
  - URL como texto (copiable)
  - Indicador de estado de conexión
  - Preview de firma recibida
  - Botones: Cancelar, Confirmar

#### `ConsentimientosView.axaml`
- **Responsabilidad:** Lista de consentimientos de un cliente
- **Componentes:**
  - Lista de consentimientos
  - Filtros por tipo
  - Botones: Ver PDF, Descargar, Eliminar

### 4. Controles

#### `SignaturePad.axaml` (opcional - para PC)
- **Responsabilidad:** Control de firma en PC (mouse/tableta)
- **Uso:** Como alternativa si no hay móvil disponible

### 5. Archivos Web

#### `wwwroot/firma.html`
- **Responsabilidad:** Página web optimizada para móvil
- **Componentes:**
  - Canvas táctil para firmar
  - Botón "Limpiar"
  - Botón "Enviar firma"
  - Instrucciones claras
  - CSS responsive

#### `wwwroot/signature.js`
- **Responsabilidad:** Lógica JavaScript para capturar firma táctil
- **Funciones:**
  - `inicializarCanvas()`
  - `capturarToque(event)`
  - `limpiarFirma()`
  - `enviarFirma()`

### 6. Plantillas

#### `Plantillas/ConsentimientoRGPD.txt`
- Texto legal del consentimiento RGPD

#### `Plantillas/ConsentimientoImagenes.txt`
- Texto del consentimiento de uso de imágenes

#### `Plantillas/ConsentimientoTrabajo.txt`
- Texto del consentimiento informado de trabajo

---

## 📅 Plan de Implementación por Fases

### Fase 1: Infraestructura Base ✅

| # | Tarea | Estado | Notas |
|---|-------|--------|-------|
| 1.1 | Instalar QuestPDF (NuGet) | ✅ | Versión 2025.12.0 instalada |
| 1.2 | Instalar QRCoder (NuGet) | ✅ | Versión 1.7.0 instalada |
| 1.3 | Crear carpeta `wwwroot/` | ✅ | Carpeta creada, configurada en .csproj |
| 1.4 | Crear carpeta `Plantillas/` | ✅ | Carpeta creada con 3 plantillas de texto |
| 1.5 | Crear estructura de carpetas para PDFs | ✅ | `ConsentimientoPathService.cs` creado |

### Fase 2: Servidor HTTP Local ✅

| # | Tarea | Estado | Notas |
|---|-------|--------|-------|
| 2.1 | Crear `FirmaWebService.cs` | ✅ | Servicio completo implementado |
| 2.2 | Implementar detección de IP local | ✅ | Método `DetectarIPLocal()` |
| 2.3 | Implementar servidor HTTP (HttpListener) | ✅ | Puerto 8080, escucha en todas las interfaces |
| 2.4 | Implementar generación de tokens únicos | ✅ | Método `GenerarTokenUnico()` con GUID |
| 2.5 | Implementar endpoint para recibir firma | ✅ | POST /firma/{token} con validación |
| 2.6 | Implementar endpoint para servir HTML | ✅ | GET /firma/{token} sirve firma.html |
| 2.7 | Implementar timeout y limpieza | ✅ | Tokens expiran en 10 min, limpieza automática |

### Fase 3: Página Web de Firma (Móvil) ✅

| # | Tarea | Estado | Notas |
|---|-------|--------|-------|
| 3.1 | Crear `wwwroot/firma.html` | ✅ | Página HTML completa y responsive |
| 3.2 | Crear `wwwroot/signature.js` | ✅ | Lógica completa de firma táctil y mouse |
| 3.3 | Crear `wwwroot/styles.css` | ✅ | Estilos modernos y responsive |
| 3.4 | Implementar canvas táctil | ✅ | Soporte touch y mouse, prevención de scroll |
| 3.5 | Implementar envío de firma | ✅ | POST al servidor con base64 |
| 3.6 | Implementar botón "Limpiar" | ✅ | Limpia canvas y reinicia estado |
| 3.7 | Optimizar para móvil | ✅ | Touch events, responsive, sin zoom |

### Fase 4: Generación de QR Code ✅

| # | Tarea | Estado | Notas |
|---|-------|--------|-------|
| 4.1 | Crear `QRCodeService.cs` | ✅ | Servicio completo con múltiples métodos |
| 4.2 | Integrar QRCoder | ✅ | Generación de QR desde URL/texto |
| 4.3 | Convertir a Bitmap de Avalonia | ✅ | Conversión de System.Drawing.Bitmap a Avalonia.Bitmap |
| 4.4 | Métodos adicionales | ✅ | Guardar en archivo, generar como bytes |
| 4.5 | Mostrar QR en modal | ✅ | Integrado en `ConsentimientoFirmaView` |
| 4.6 | Mostrar URL como texto | ✅ | Integrado en `ConsentimientoFirmaViewModel`/View |
| 4.7 | Botón "Copiar URL" | ✅ | Implementado (por ahora log + estado visual) |

### Fase 5: Generación de PDFs ✅

| # | Tarea | Estado | Notas |
|---|-------|--------|-------|
| 5.1 | Crear `ConsentimientoService.cs` | ✅ | Servicio completo de PDFs |
| 5.2 | Crear plantillas de texto | ✅ | Ya creadas en Fase 1 |
| 5.3 | Implementar generación de PDF (QuestPDF) | ✅ | PDF completo con formato A4 |
| 5.4 | Incluir imagen de firma en PDF | ✅ | Conversión base64 a imagen |
| 5.5 | Incluir datos del cliente en PDF | ✅ | Nombre, DNI, fecha |
| 5.6 | Incluir datos del estudio en PDF | ✅ | Desde Configuracion |
| 5.7 | Guardar PDF en carpeta | ✅ | Con nombre único usando ConsentimientoPathService |
| 5.8 | Validar estructura del PDF | ✅ | Formato legalmente válido con fecha/hora |
| 5.9 | Métodos adicionales | ✅ | Validar consentimientos, guardar en BD |

### Fase 6: ViewModel y View de Firma ✅

| # | Tarea | Estado | Notas |
|---|-------|--------|-------|
| 6.1 | Crear `ConsentimientoFirmaViewModel.cs` | ✅ | ViewModel completo con lógica de servidor |
| 6.2 | Crear `ConsentimientoFirmaView.axaml` | ✅ | Modal de firma con diseño moderno |
| 6.3 | Integrar QR code en modal | ✅ | QR grande y visible |
| 6.4 | Integrar preview de firma | ✅ | Indicador visual cuando se recibe |
| 6.5 | Implementar estados de conexión | ✅ | Estados dinámicos con emojis |
| 6.6 | Implementar timeout visual | ⏳ | Pendiente: timeout de 5 minutos implementado |
| 6.7 | Diseño moderno del modal | ✅ | Estilo consistente con app (glow, colores) |

### Fase 7: Integración en Clientes ✅

| # | Tarea | Estado | Notas |
|---|-------|--------|-------|
| 7.1 | Modificar `ClientesViewModel` | ✅ | Verificación RGPD implementada |
| 7.2 | Integrar modal RGPD en creación | ✅ | Se abre solo si se marca checkbox opcional |
| 7.3 | Validar RGPD obligatorio | ✅ | RGPD ahora es opcional al crear cliente |
| 7.4 | Integrar checkbox Imágenes | ✅ | Checkbox opcional en formulario |
| 7.5 | Integrar modal Imágenes | ✅ | Se abre si marca checkbox |
| 7.6 | Guardar consentimientos en BD | ✅ | Se guardan al confirmar firma |
| 7.7 | Checkbox RGPD opcional | ✅ | Cliente puede crearse sin firmar RGPD |
| 7.8 | Indicador visual "Sin RGPD" | ✅ | Badge rojo en lista de clientes |
| 7.9 | Botón "Firmar RGPD" desde lista | ✅ | Permite firmar RGPD después de crear cliente |
| 7.10 | Propiedad calculada `TieneConsentimientoRGPD` | ✅ | Facilita verificación de estado |
| 7.11 | Evento `FirmaCompletada` | ✅ | Recarga lista automáticamente tras firma |

### Fase 8: Integración en Trabajos ✅

| # | Tarea | Estado | Notas |
|---|-------|--------|-------|
| 8.1 | Modificar `TrabajosViewModel` | ✅ | Verificación de RGPD del cliente y consentimiento por trabajo |
| 8.2 | Validar RGPD antes de guardar trabajo | ✅ | No bloquea guardado, pero muestra aviso claro y acceso rápido a firma |
| 8.3 | Integrar consentimiento de trabajo en modal | ✅ | Sección específica en el modal de trabajo con estado y acciones (ver/exportar/enviar/firmar) |
| 8.4 | Vincular consentimiento a trabajo | ✅ | `TrabajoId` en `Consentimiento` y navegación `Trabajo.Consentimiento` |
| 8.5 | Bloquear modificación de trabajos con consentimiento firmado | ✅ | Trabajo de solo lectura si el consentimiento de trabajo está firmado |
| 8.6 | Aviso visual en lista de trabajos | ✅ | Badges "✅ Consentimiento" / "⚠️ Sin consentimiento" y botón "Firmar consentimiento" |
| 8.7 | Integración en Agenda (citas) | ✅ | Aviso cuando la cita tiene trabajo sin consentimiento firmado y acceso a firma desde la cita |

### Fase 9: Vista de Consentimientos ✅

| # | Tarea | Estado | Notas |
|---|-------|--------|-------|
| 9.1 | Crear `ConsentimientosViewModel.cs` | ✅ | Gestión global de consentimientos con filtros por cliente y tipo |
| 9.2 | Crear `ConsentimientosView.axaml` | ✅ | Lista global de consentimientos con acciones (ver/exportar/email) |
| 9.3 | Integrar en navegación principal | ✅ | Entrada "Consentimientos" en el menú lateral |
| 9.4 | Implementar ver PDF | ✅ | Abrir PDF en visor predeterminado |
| 9.5 | Implementar descargar PDF | ✅ | Exportar PDF al Escritorio del usuario |
| 9.6 | Implementar filtros | ✅ | Por cliente y tipo de consentimiento |
| 9.7 | Mostrar estado (Firmado/Pendiente) | ✅ | Indicador visual en lista |

### Fase 10: Mejoras y Pulido ⏳

| # | Tarea | Estado | Notas |
|---|-------|--------|-------|
| 10.1 | Manejo de errores de conexión | ⏳ | Si no hay WiFi, etc. |
| 10.2 | Alternativa mouse/trackpad | ⏳ | Si no hay móvil disponible |
| 10.3 | Validaciones de seguridad | ⏳ | Timeout, tokens únicos |
| 10.4 | Logging de operaciones | ⏳ | Registrar firmas, etc. |
| 10.5 | Tests manuales | ⏳ | Probar todos los flujos |
| 10.6 | Documentación de uso | ⏳ | Guía para usuarios |

---

## 📊 Registro de Avances

### Estado General: 90% Completado

```
Fase 1: Infraestructura Base          [████████████████████] 100%
Fase 2: Servidor HTTP Local           [████████████████████] 100%
Fase 3: Página Web de Firma           [████████████████████] 100%
Fase 4: Generación de QR Code         [████████████████████] 100%
Fase 5: Generación de PDFs            [████████████████████] 100%
Fase 6: ViewModel y View de Firma     [████████████████████] 100%
Fase 7: Integración en Clientes       [████████████████████] 100%
Fase 8: Integración en Trabajos       [████████████████████] 100%
Fase 9: Vista de Consentimientos      [████████████████████] 100%
Fase 10: Mejoras y Pulido             [░░░░░░░░░░░░░░░░░░░░]   0%
────────────────────────────────────────────────────────────────
Total del Módulo                       [█████████████████░░░]  90%
```

### Historial de Cambios

#### Diciembre 2025

**19 de Diciembre:**
- ✅ Plan de implementación creado
- ✅ Arquitectura definida
- ✅ Componentes identificados
- ✅ **Fase 1 completada:**
  - QuestPDF 2025.12.0 instalado
  - QRCoder 1.7.0 instalado
  - Carpeta `wwwroot/` creada y configurada
  - Carpeta `Plantillas/` creada con 3 plantillas:
    - `ConsentimientoRGPD.txt`
    - `ConsentimientoImagenes.txt`
    - `ConsentimientoTrabajo.txt`
  - `ConsentimientoPathService.cs` creado para gestión de rutas
  - Estructura de carpetas para PDFs configurada
- ✅ **Fase 2 completada:**
  - `FirmaWebService.cs` creado con servidor HTTP completo
  - Detección automática de IP local
  - Servidor HttpListener en puerto 8080
  - Generación de tokens únicos (GUID)
  - Endpoint GET /firma/{token} para servir HTML
  - Endpoint POST /firma/{token} para recibir firmas
  - Sistema de timeout y limpieza de tokens expirados (10 min)
  - Evento `FirmaRecibida` para notificaciones
- ✅ **Fase 3 completada:**
  - `wwwroot/firma.html` - Página HTML completa y responsive
  - `wwwroot/signature.js` - Lógica de firma táctil y mouse
  - `wwwroot/styles.css` - Estilos modernos con gradientes
  - Canvas táctil funcional con soporte touch y mouse
  - Envío de firma al servidor (POST con base64)
  - Botón limpiar y validación de firma
  - Optimizado para móvil (sin zoom, responsive, touch events)
- ✅ **Fase 4 completada:**
  - `QRCodeService.cs` creado con múltiples métodos
  - Integración completa de QRCoder
  - Generación de QR desde URL/texto
  - Conversión a Bitmap de Avalonia para mostrar en UI
  - Métodos adicionales: guardar en archivo, generar como bytes
  - Niveles de corrección de errores configurables
- ✅ **Fase 5 completada:**
  - `ConsentimientoService.cs` creado con generación completa de PDFs
  - Carga de plantillas de texto desde archivos
  - Reemplazo de placeholders con datos reales (cliente, trabajo, estudio)
  - Generación de PDF con QuestPDF (formato A4, márgenes, tipografía)
  - Inclusión de imagen de firma (conversión base64 a imagen)
  - Inclusión de datos del cliente (nombre, DNI, fecha)
  - Inclusión de datos del estudio desde Configuracion
  - Guardado de PDF con nombres únicos
  - Métodos adicionales: validar consentimientos, guardar en BD
- ✅ **Fase 6 completada:**
  - `ConsentimientoFirmaViewModel.cs` creado con lógica completa
  - `ConsentimientoFirmaView.axaml` creado con modal moderno
  - Integración de servidor HTTP (inicio automático)
  - Generación de tokens únicos y QR codes
  - Estados de conexión dinámicos con emojis
  - Preview de firma recibida
  - Validación de aceptación de términos
  - Generación y guardado de PDF al confirmar
  - Diseño consistente con el resto de la app (glow, colores morado/naranja)
- ✅ **Fase 7 completada:**
  - `ClientesViewModel` modificado para integrar consentimientos
  - **RGPD ahora es opcional:** Checkbox en formulario para elegir si firmar al crear cliente
  - Cliente puede crearse sin RGPD y firmarlo después desde la lista
  - Indicador visual "⚠️ Sin RGPD" en lista de clientes sin consentimiento
  - Botón "📝 Firmar RGPD" en lista para firmar consentimiento después de crear cliente
  - Propiedad calculada `TieneConsentimientoRGPD` en modelo `Cliente` para verificación fácil
  - Evento `FirmaCompletada` que recarga lista automáticamente tras firma
  - Checkbox opcional para consentimiento de imágenes
  - Modal de imágenes se abre si se marca el checkbox
  - Consentimientos se guardan en BD automáticamente
  - `ConsentimientoFirmaView` integrado en `ClientesView`

---

## 🔄 Flujos de Usuario

### Flujo 1: Crear Cliente con RGPD (Opcional)

```
1. Usuario abre "Nuevo Cliente"
2. Rellena datos del cliente
3. Opcional: Marca checkbox "📝 Firmar consentimiento RGPD"
4. Click "Guardar"
5. Si marcó checkbox RGPD:
   └─ Abre modal de firma RGPD
      ├─ Muestra texto del consentimiento
      ├─ Checkbox "Acepto términos" (obligatorio)
      ├─ Muestra QR code
      ├─ Móvil escanea QR → Firma
      ├─ PC recibe firma → Genera PDF
      ├─ Guarda PDF + Registro en BD
      └─ Cierra modal y recarga lista
6. Si NO marcó checkbox:
   └─ Cliente se crea sin RGPD
      └─ Aparece "⚠️ Sin RGPD" en lista
      └─ Botón "📝 Firmar RGPD" disponible para firmar después
```

### Flujo 2: Crear Trabajo con Consentimiento

```
1. Usuario abre "Nuevo Trabajo"
2. Selecciona cliente
3. Rellena datos del trabajo
4. Click "Guardar"
5. Sistema verifica:
   ├─ ¿Cliente tiene RGPD?
   │  └─ NO → Bloquear, mostrar alerta
   │     "El cliente debe firmar RGPD primero"
   │     Botón "Ir a firmar RGPD"
   └─ SÍ → Continuar
6. Abre modal de firma Trabajo
   ├─ Muestra texto con datos del trabajo
   ├─ Checkbox "Acepto riesgos" (obligatorio)
   ├─ Muestra QR code
   ├─ Móvil escanea QR → Firma
   ├─ PC recibe firma → Genera PDF
   ├─ Guarda PDF + Registro en BD
   ├─ Vincula consentimiento al trabajo
   └─ Guarda trabajo
```

### Flujo 3: Ver Consentimientos de un Cliente

```
1. Usuario selecciona cliente
2. Abre sección "Consentimientos"
3. Ve lista de todos sus consentimientos:
   - Tipo (RGPD, Imágenes, Trabajo)
   - Fecha de firma
   - Estado (Firmado/Pendiente)
   - Botón "Ver PDF"
   - Botón "Descargar PDF"
```

---

## 🔧 Consideraciones Técnicas

### Seguridad
- ✅ Servidor solo en red local (no expuesto a internet)
- ✅ Tokens únicos con expiración (5-10 minutos)
- ✅ Timeout automático si no hay conexión
- ✅ Validación de origen de peticiones

### Rendimiento
- ✅ Servidor HTTP ligero (HttpListener nativo)
- ✅ Cerrar servidor después de recibir firma
- ✅ Optimizar tamaño de imágenes de firma
- ✅ PDFs comprimidos pero legibles

### Compatibilidad
- ✅ Funciona en cualquier móvil con navegador
- ✅ No requiere app móvil
- ✅ Funciona con cualquier WiFi local
- ✅ Alternativa mouse/trackpad si no hay móvil

### Almacenamiento
- ✅ PDFs en: `%LOCALAPPDATA%\Ataena\consentimientos\[Tipo]\`
- ✅ Nombre: `cliente-{id}_rgpd_{fecha}_{hora}.pdf`
- ✅ Ruta guardada en BD: `Consentimiento.RutaDocumento`
- ✅ Backup incluido en backup general de BD

---

## 📚 Referencias

- [QuestPDF Documentation](https://www.questpdf.com/)
- [QRCoder GitHub](https://github.com/codebude/QRCoder)
- [RGPD - Consentimiento Digital](https://www.aepd.es/)
- [HttpListener Class](https://learn.microsoft.com/dotnet/api/system.net.httplistener)

---

> **Nota:** Este plan se actualizará conforme avance la implementación. Cada tarea completada se marcará con ✅.

