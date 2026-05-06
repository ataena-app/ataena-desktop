# Plan de Licencias y Actualizaciones - Ataena CRM

> **Fecha de creación:** 21 de Febrero 2026  
> **Estado:** Planificación  
> **Nota:** La primera versión (tester) NO incluye licencias. Este plan se aplicará en futuras actualizaciones.

---

## 📋 Índice

1. [Resumen Ejecutivo](#-resumen-ejecutivo)
2. [Fases del Plan](#-fases-del-plan)
3. [Versión 1 - Tester (sin licencia)](#-versión-1---tester-sin-licencia)
4. [Preparación para Futuras Versiones](#-preparación-para-futuras-versiones)
5. [Sistema de Actualizaciones](#-sistema-de-actualizaciones)
6. [Sistema de Licencias](#-sistema-de-licencias)
7. [Protecciones Antipiratería](#-protecciones-antipiratería)
8. [Web y Comercialización](#-web-y-comercialización)
9. [Checklist General](#-checklist-general)

---

## 🎯 Resumen Ejecutivo

### Objetivo
- **v1 (tester):** Instalador funcional, sin validación de licencia. El estudio tester nunca pagará.
- **v2+:** Sistema de licencias, actualizaciones automáticas, web de registro y pago.

### Principios
- La app debe estar **preparada** para activar licencias cuando llegue el momento.
- Las actualizaciones se publicarán en **GitHub Releases** (gratis).
- Sin internet: período de gracia limitado, luego la app se restringe.

---

## 📅 Fases del Plan

```
FASE 0: v1 Tester                    [AHORA]
├── Instalador (Velopack)
├── Sin licencias
├── Actualizaciones: manuales (descargar nuevo .exe) — hasta Fase 1
└── Entregar al estudio tester

FASE 1: Actualizaciones automáticas  [SIGUIENTE]
├── Integrar Velopack en la app
├── Configurar GitHub Releases
├── App consulta si hay versión nueva
└── Descarga e instala al cerrar

FASE 2: Sistema de licencias         [DESPUÉS]
├── API de validación
├── Pantalla de activación en la app
├── Serial + Hardware ID
└── Período de gracia sin internet

FASE 3: Web comercial                [PARALELO O DESPUÉS]
├── Landing page
├── Registro y pago
├── Área de cliente (descargas, seriales)
└── Integración con pasarela de pago
```

---

## ✅ Versión 1 - Tester (sin licencia)

### Objetivo
Entregar un instalador funcional al estudio que hace de tester. Sin cobrar, sin validación.

### Checklist v1

- [ ] **Instalador**
  - [ ] Elegir herramienta (Velopack o Inno Setup)
  - [ ] Crear script de instalación
  - [ ] Incluir .NET Runtime (self-contained o verificar instalado)
  - [ ] Copiar a %PROGRAMFILES%\Ataena
  - [ ] Crear carpeta %LOCALAPPDATA%\Ataena
  - [ ] Accesos directos (escritorio, menú inicio)
  - [ ] Registrar en "Agregar o quitar programas"
  - [ ] Desinstalador
  - [ ] Probar en máquina limpia

- [ ] **Icono**
  - [ ] Sustituir avalonia-logo.ico por icono de Ataena

- [ ] **Testing manual** (ver nota abajo)
  - [ ] Probar flujo completo de instalación
  - [ ] Probar que la app funciona tras instalar
  - [ ] Probar desinstalación

- [ ] **Entrega al tester**
  - [ ] Entregar instalador al estudio
  - [ ] Documentar que es versión sin licencia (para referencia futura)

### Notas v1
- No hay validación de licencia.
- No hay comprobación de actualizaciones automática.
- El tester recibe el .exe directamente (o descarga desde un enlace).
- **El tester nunca pagará** — identificar cómo distinguirlo cuando se active el sistema de licencias (serial especial, email en lista blanca, etc.).

### ¿Qué es "Testing manual"?
**No son tests automatizados** (no se añade código de pruebas). Es **probar a mano**:
- Abres la app, haces clic en cada pantalla, creas un cliente, una cita, etc.
- Verificas que todo funciona como esperas antes de entregar al tester.
- Es una lista mental o en papel: "¿Funciona crear cliente? ¿Funciona el backup? ¿Se ve bien el splash?"
- El estudio tester también hará testing manual al usarla en su día a día.

---

## 🔧 Preparación para Futuras Versiones

### Arquitectura a tener en cuenta

La app debe estar **preparada** para que, cuando se active el sistema de licencias, no requiera cambios estructurales grandes.

| Aspecto | Preparación recomendada |
|---------|-------------------------|
| **Versión de la app** | Incluir `AssemblyVersion` en el .csproj para que Velopack/updates funcionen |
| **Estructura de carpetas** | Mantener %LOCALAPPDATA%\Ataena como está |
| **Punto de entrada** | Program.cs — lugar natural para añadir comprobación de licencia al inicio |
| **Servicios** | Crear `LicenciaService` (o similar) vacío o con interfaz, implementar después |
| **Configuración** | No guardar datos de licencia aún; la estructura puede preverse |

### Checklist de preparación (opcional para v1)

- [ ] Versión en AssemblyInfo o .csproj (`<Version>1.0.0</Version>`)
- [ ] Documentar dónde se añadirá la comprobación de licencia (Program.cs, antes de BuildAvaloniaApp)
- [ ] Documentar dónde se añadirá la pantalla de activación (antes de MainWindow)

### Lo que NO hace falta en v1
- API de licencias
- Pantalla de activación
- Validación online
- Ofuscación

---

## 🔄 Flujo Instalador + Actualizaciones (checklist)

### Flujo completo (cuando todo esté implementado)

```
1. Tú generas el instalador (Ataena_Setup_v1.0.0.exe)
2. El tester lo instala en su PC
3. Tú modificas algo, compilas v1.0.1, subes a GitHub Releases
4. El tester abre Ataena → la app detecta la nueva versión
5. Le sale "Nueva versión disponible"
6. Acepta → descarga → al cerrar la app se aplica la actualización
7. Al abrir de nuevo → ya tiene v1.0.1
```

### Qué hace falta para que funcione

| Paso | Estado | Checklist |
|------|--------|-----------|
| Instalador | ⏳ Pendiente | Crear con Velopack |
| Detección de actualizaciones | ⏳ Pendiente | Integrar Velopack en la app |
| Publicar en GitHub Releases | ⏳ Pendiente | Subir .exe o .nupkg cuando publiques |
| URL de actualizaciones | ⏳ Pendiente | Configurar en la app (GitHub Releases) |

### Orden de implementación

1. **Instalador** — Sin esto no hay nada que instalar
2. **Actualizaciones** — Velopack las incluye; integrar en la app
3. **Publicar v1** — Subir a GitHub Releases
4. **Probar flujo** — Instalar → modificar → publicar v1.0.1 → ver que el tester recibe la actualización

---

## 🔄 Sistema de Actualizaciones

### Objetivo
Cuando el usuario tenga una versión instalada, la app debe poder actualizarse automáticamente al publicar una nueva versión.

### Herramienta: Velopack
- Integrado con .NET
- Delta updates (solo descarga cambios)
- Aplicación al cerrar la app
- Compatible con GitHub Releases

### Infraestructura: GitHub Releases
- **Coste:** Gratis
- **Límite:** 2 GB por archivo
- **URL base:** `https://github.com/Jvalfdev/desktop-myos-app/releases`

### Checklist Actualizaciones

- [ ] **Configurar Velopack en el proyecto**
  - [ ] Añadir paquete NuGet
  - [ ] Configurar URL del servidor de updates
  - [ ] Configurar canal (stable)

- [ ] **Flujo en la app**
  - [ ] Al iniciar: comprobar si hay versión nueva
  - [ ] Si hay: notificar al usuario
  - [ ] Descargar en background
  - [ ] Al cerrar: aplicar actualización y reiniciar

- [ ] **Publicar releases**
  - [ ] Crear tag (v1.0.0, v1.1.0, etc.)
  - [ ] Subir archivos .nupkg o .exe a GitHub Releases
  - [ ] Documentar proceso para cada nueva versión

### Repo público vs privado
- **Público:** Cualquiera puede descargar. Ideal para clientes que actualizan sin login.
- **Privado:** Requiere token. Más complejo para usuarios finales.

**Recomendación:** Repo público para los releases (o usar otro hosting público para los archivos).

---

## 🔑 Sistema de Licencias

### Modelo (a definir)
| Opción | Descripción |
|--------|-------------|
| Perpetua | Pago único, uso ilimitado |
| Anual | Suscripción renovable |
| Trial | Período de prueba (14-30 días) |

### Tester
- **Nunca paga**
- Identificación: serial especial (ej. `TEST-ERZULIE-XXXX`) o email en lista blanca en la BD

### Flujo de activación (futuro)
1. Usuario compra en la web
2. Recibe serial por email
3. Instala Ataena
4. Primer inicio → pantalla "Introduce tu serial"
5. App envía serial + Hardware ID al servidor
6. Servidor valida y devuelve token
7. App guarda token local (cifrado, firmado)
8. App funcionando

### Validación periódica
- Cada inicio (o cada X días): intentar validar online
- Si hay internet: validar, actualizar token
- Si no hay internet: modo gracia

### Período de gracia sin internet
- **Días sin validación:** Contador de días consecutivos sin validación exitosa
- **Límite:** Ej. 14 días
- **Por qué no usar fecha del PC:** Evitar trucos cambiando la hora del sistema

### Checklist Licencias

- [ ] **Decisiones**
  - [ ] Modelo: perpetua / anual / ambos
  - [ ] Precio(s)
  - [ ] Trial: sí/no, duración
  - [ ] Días de gracia sin internet
  - [ ] Cómo identificar al tester

- [ ] **API**
  - [ ] Endpoint: activar (serial + Hardware ID)
  - [ ] Endpoint: validar (token)
  - [ ] Base de datos: licencias, activaciones
  - [ ] Generación de seriales

- [ ] **App**
  - [ ] Pantalla de activación
  - [ ] LicenciaService
  - [ ] Almacenamiento seguro del token
  - [ ] Comprobación al inicio
  - [ ] Lógica de gracia sin internet

- [ ] **Limitaciones cuando expira**
  - [ ] Definir qué se limita (solo lectura, límite clientes, marca de agua, etc.)
  - [ ] Implementar restricciones

---

## 🛡️ Protecciones Antipiratería

### Contra cambio de hora del PC
- **No depender de la fecha local** para expiración
- Usar **"días sin validación online"** como contador
- Guardar **última fecha de validación** — si la fecha actual es anterior, sospechoso

### Contra token local falsificado
- Token **firmado** por el servidor
- Token **cifrado** en disco
- Token **vinculado a Hardware ID**

### Contra parcheo del .exe
- **Ofuscación** (ConfuserEx u otra herramienta)
- **Verificación de integridad** (hash del ejecutable) — opcional

### Checklist Seguridad

- [ ] Token firmado digitalmente
- [ ] Token cifrado en disco
- [ ] Binding a Hardware ID
- [ ] Contador de días sin validación (no fecha local)
- [ ] Detección de tiempo invertido (fecha actual < última validación)
- [ ] Ofuscación con ConfuserEx (o similar)

---

## 🌐 Web y Comercialización

### Componentes de la web
| Página/Sección | Función |
|----------------|---------|
| Landing | Presentación del producto |
| Precios | Planes y precios |
| Checkout | Registro y pago |
| Área de cliente | Descargas, seriales, facturas |
| Changelog | Novedades por versión |

### Pasarela de pago (a elegir)
| Opción | Pros | Contras |
|--------|------|---------|
| Stripe | Flexible, API potente | Más integración manual |
| Gumroad | Simple, entrega de licencias | Comisión ~10% |
| Paddle | Gestión de impuestos, licencias | Comisión ~5% |

### Checklist Web

- [ ] **Decisión**
  - [ ] Elegir pasarela de pago
  - [ ] Elegir hosting (Vercel, Netlify, etc.)

- [ ] **Desarrollo**
  - [ ] Landing page
  - [ ] Página de precios
  - [ ] Checkout / registro
  - [ ] Área de cliente
  - [ ] Webhook para generar serial tras pago
  - [ ] Email automático con serial

- [ ] **Legal**
  - [ ] Política de privacidad
  - [ ] Términos y condiciones
  - [ ] Política de reembolsos

---

## ☑️ Checklist General

### Fase 0 - v1 Tester (actual)
- [ ] Instalador creado y probado
- [ ] Icono de la aplicación
- [ ] Testing manual completo
- [ ] Entregado al estudio tester
- [ ] Versión documentada como "sin licencia"

### Fase 1 - Actualizaciones
- [ ] Velopack integrado
- [ ] GitHub Releases configurado
- [ ] App comprueba actualizaciones al iniciar
- [ ] Proceso documentado para publicar nuevas versiones

### Fase 2 - Licencias
- [ ] Decisiones tomadas (modelo, precio, gracia)
- [ ] API de licencias implementada
- [ ] Pantalla de activación en la app
- [ ] Tester identificado (serial especial o lista blanca)
- [ ] Limitaciones definidas e implementadas cuando expira

### Fase 3 - Web
- [ ] Web operativa
- [ ] Checkout funcionando
- [ ] Seriales generados y enviados automáticamente

### Seguridad (cuando se active licencias)
- [ ] Token firmado y cifrado
- [ ] Hardware ID
- [ ] Contador días sin validación
- [ ] Ofuscación básica

---

## 📚 Documentos Relacionados

| Documento | Contenido |
|-----------|-----------|
| `04-plan-distribucion.md` | Plan detallado de instalador, licencias, infraestructura |
| `05-estudio-mercado-viabilidad.md` | Análisis de precios y mercado |
| `08-analisis-primer-inicio-y-backups.md` | Menú post-instalación (crear/importar) y mejoras a backups |
| `ESTADO-ACTUAL.md` | Estado actual del proyecto |

---

> **Nota:** Este documento se actualizará conforme se completen tareas y se tomen decisiones.
