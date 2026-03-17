# Plan de Distribución: Ataena CRM

> **Fecha de creación:** 30 de Diciembre 2025  
> **Estado:** Planificación  
> **Prioridad:** Alta  
> **Autor:** Jose Vallejo

---

## 📋 Índice

1. [Resumen Ejecutivo](#-resumen-ejecutivo)
2. [Instalador](#-instalador)
3. [Sistema de Actualizaciones](#-sistema-de-actualizaciones)
4. [Sistema de Licencias (Seriales)](#-sistema-de-licencias-seriales)
5. [Medidas Antipiratería](#-medidas-antipiratería)
6. [Infraestructura Necesaria](#-infraestructura-necesaria)
7. [Decisiones Pendientes](#-decisiones-pendientes)
8. [Plan de Implementación](#-plan-de-implementación)
9. [Presupuesto Estimado](#-presupuesto-estimado)

> 📊 **Ver también:** `05-estudio-mercado-viabilidad.md` para análisis de competencia, precios y proyección financiera.

---

## 🎯 Resumen Ejecutivo

### Objetivo
Crear un sistema de distribución profesional para Ataena CRM que permita:
- Instalar la aplicación de forma sencilla
- Actualizar automáticamente
- Controlar el uso mediante licencias
- Proteger contra piratería

### Público Objetivo
- Estudios de tatuajes pequeños y medianos (1-5 artistas)
- Tatuadores independientes
- Usuarios con conocimientos informáticos básicos

### Modelo de Negocio Propuesto

| Opción | Descripción | Precio Sugerido |
|--------|-------------|-----------------|
| **Licencia Perpetua** | Pago único, actualizaciones de por vida | 149€ - 199€ |
| **Licencia Anual** | Pago anual, renovación automática | 49€ - 79€/año |
| **Trial** | Versión de prueba 14-30 días | Gratis |

---

## 📦 Instalador

### Opciones Evaluadas

| Herramienta | Tipo | Pros | Contras | Coste |
|-------------|------|------|---------|-------|
| **Velopack** | Open Source | Moderno, incluye updates, .NET nativo | Relativamente nuevo | Gratis |
| **Inno Setup** | Open Source | Maduro, muy usado, personalizable | Sin updates integrados | Gratis |
| **NSIS** | Open Source | Flexible, scripts potentes | Curva de aprendizaje | Gratis |
| **MSIX** | Microsoft | Formato oficial Windows 10/11 | Más complejo | Gratis |
| **Advanced Installer** | Comercial | GUI, todo integrado | Costoso para indie | 499€+ |
| **InstallShield** | Comercial | Enterprise, muy completo | Muy costoso | 1000€+ |

### Recomendación: Velopack + Inno Setup

**Velopack** es la evolución de Squirrel.Windows, diseñado específicamente para .NET moderno:
- Creación de instaladores
- Actualizaciones automáticas integradas
- Delta updates (solo cambios)
- Soporte nativo para .NET 8/9
- Multiplataforma (Windows, macOS, Linux)

**Inno Setup** como alternativa/backup:
- Muy probado y estable
- Gran comunidad
- Personalización total del instalador

### Estructura del Instalador

```
Ataena_Setup_v1.0.0.exe
│
├── 📦 Contenido
│   ├── Ataena.exe (aplicación principal)
│   ├── Ataena.dll (librerías)
│   ├── wwwroot/ (archivos web para firma móvil)
│   ├── Plantillas/ (plantillas de consentimientos)
│   └── Recursos adicionales
│
├── 🔧 Acciones de Instalación
│   ├── Verificar .NET Runtime instalado
│   ├── Instalar .NET Runtime si es necesario
│   ├── Copiar archivos a %PROGRAMFILES%\Ataena
│   ├── Crear carpeta de datos en %LOCALAPPDATA%\Ataena
│   ├── Crear acceso directo en escritorio
│   ├── Crear entrada en menú inicio
│   ├── Registrar programa en "Agregar/Quitar programas"
│   └── Configurar regla de firewall (para firma móvil)
│
└── 📋 Metadatos
    ├── Versión de la aplicación
    ├── Firma digital del ejecutable
    └── Información del publicador
```

### Flujo de Instalación del Usuario

```
┌─────────────────────────────────────────────────────────────┐
│  1. Descarga Ataena_Setup.exe desde web oficial          │
│                          ↓                                   │
│  2. Ejecuta instalador (UAC pide permisos admin)            │
│                          ↓                                   │
│  3. Pantalla de bienvenida con logo                         │
│                          ↓                                   │
│  4. Acepta términos y condiciones                           │
│                          ↓                                   │
│  5. Selecciona carpeta de instalación (opcional)            │
│                          ↓                                   │
│  6. Instalación (barra de progreso)                         │
│     - Verifica/instala .NET Runtime                         │
│     - Copia archivos                                        │
│     - Configura firewall                                    │
│                          ↓                                   │
│  7. Pantalla de finalización                                │
│     - Checkbox: "Ejecutar Ataena"                        │
│     - Checkbox: "Crear acceso directo"                      │
│                          ↓                                   │
│  8. Primer inicio → Pantalla de activación                  │
└─────────────────────────────────────────────────────────────┘
```

### Requisitos Técnicos del Instalador

| Requisito | Especificación |
|-----------|----------------|
| SO Mínimo | Windows 10 (1903) / Windows 11 |
| Arquitectura | x64 (opcional: ARM64) |
| .NET Runtime | .NET 9.0 (incluido o descarga automática) |
| Espacio en disco | ~150 MB (instalación) + datos |
| RAM mínima | 4 GB |
| Permisos | Administrador (solo instalación) |

### Decisiones Pendientes - Instalador

- [ ] ¿Self-contained o framework-dependent?
  - **Self-contained**: Mayor tamaño (~150MB), no requiere .NET instalado
  - **Framework-dependent**: Menor tamaño (~20MB), requiere .NET Runtime

- [ ] ¿Firma digital del ejecutable?
  - Requiere certificado de firma de código (~200-400€/año)
  - Evita advertencias de "Aplicación no reconocida"

- [ ] ¿Installer silencioso para empresas?
  - Permitir `Ataena_Setup.exe /silent /key=XXXX-XXXX-XXXX`

---

## 🔄 Sistema de Actualizaciones

### Opciones Evaluadas

| Herramienta | Tipo | Características | Complejidad |
|-------------|------|-----------------|-------------|
| **Velopack** | Integrado | Delta updates, auto-apply, rollback | Baja |
| **Squirrel.Windows** | Abandonado | Era popular, sin mantenimiento | - |
| **AutoUpdater.NET** | Librería | Simple, XML/JSON manifest | Media |
| **WinSparkle** | Librería | Basado en Sparkle de macOS | Media |
| **Custom** | Propio | Control total | Alta |

### Recomendación: Velopack

Velopack incluye todo lo necesario:
- Verificación de actualizaciones en background
- Descarga delta (solo cambios)
- Aplicación automática al cerrar app
- Rollback si falla
- Canales de actualización (stable, beta)

### Flujo de Actualización

```
┌─────────────────────────────────────────────────────────────┐
│                    FLUJO DE ACTUALIZACIÓN                    │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌─────────────┐         ┌─────────────┐                   │
│  │  Ataena  │ ──────→ │  Servidor   │                   │
│  │  (Cliente)  │  HTTP   │  Updates    │                   │
│  └─────────────┘         └─────────────┘                   │
│        │                        │                           │
│        │ 1. GET /updates/check  │                           │
│        │    version=1.0.0       │                           │
│        │    channel=stable      │                           │
│        │ ←────────────────────→ │                           │
│        │                        │                           │
│        │ 2. Response:           │                           │
│        │    {                   │                           │
│        │      "version": "1.1.0",                          │
│        │      "url": "...",     │                           │
│        │      "size": 5242880,  │                           │
│        │      "checksum": "..." │                           │
│        │    }                   │                           │
│        │                        │                           │
│        ↓                        │                           │
│  ┌─────────────────────────────────────────────────────┐   │
│  │           DECISIÓN DEL USUARIO                       │   │
│  │                                                       │   │
│  │  ┌─────────────────────────────────────────────────┐ │   │
│  │  │  🔔 Nueva versión disponible: v1.1.0            │ │   │
│  │  │                                                   │ │   │
│  │  │  Novedades:                                      │ │   │
│  │  │  • Mejoras en el calendario                      │ │   │
│  │  │  • Corrección de errores                         │ │   │
│  │  │                                                   │ │   │
│  │  │  [Actualizar ahora]  [Recordar más tarde]        │ │   │
│  │  └─────────────────────────────────────────────────┘ │   │
│  └─────────────────────────────────────────────────────────┘   │
│        │                                                │
│        │ 3. Descarga en background                      │
│        │                                                │
│        ↓                                                │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  Actualización descargada. Se instalará al cerrar.  │   │
│  └─────────────────────────────────────────────────────┘   │
│        │                                                │
│        │ 4. Usuario cierra la app                       │
│        │                                                │
│        ↓                                                │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  Aplicando actualización...                          │   │
│  │  [████████████████████░░░░] 80%                      │   │
│  └─────────────────────────────────────────────────────┘   │
│        │                                                │
│        │ 5. Reinicio automático con v1.1.0              │
│        ↓                                                │
│  ┌─────────────┐                                        │
│  │  Ataena  │  ← Versión 1.1.0                       │
│  │  v1.1.0 ✓   │                                        │
│  └─────────────┘                                        │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### Canales de Actualización

| Canal | Descripción | Público |
|-------|-------------|---------|
| **Stable** | Versiones probadas y estables | Todos los usuarios |
| **Beta** | Funcionalidades nuevas en pruebas | Usuarios que opten |
| **Dev** | Builds de desarrollo | Solo desarrolladores |

### Infraestructura de Updates

```
Servidor de Actualizaciones (CDN/S3/Azure Blob)
│
├── /releases/
│   ├── stable/
│   │   ├── RELEASES (manifest)
│   │   ├── Ataena-1.0.0-full.nupkg (instalación completa)
│   │   ├── Ataena-1.0.0-1.1.0-delta.nupkg (solo cambios)
│   │   └── Ataena-1.1.0-full.nupkg
│   │
│   └── beta/
│       ├── RELEASES
│       └── Ataena-1.2.0-beta1-full.nupkg
│
└── /api/
    └── check-update (endpoint JSON alternativo)
```

### Decisiones Pendientes - Actualizaciones

- [ ] ¿Actualizaciones obligatorias o opcionales?
  - Obligatorias para correcciones de seguridad
  - Opcionales para nuevas funcionalidades

- [ ] ¿Servidor propio o CDN?
  - GitHub Releases (gratis, simple)
  - Azure Blob Storage (~5€/mes)
  - Amazon S3 (~5€/mes)
  - Servidor propio (más control)

- [ ] ¿Verificar licencia antes de actualizar?
  - Si la licencia expiró, ¿permitir actualizaciones?

---

## 🔑 Sistema de Licencias (Seriales)

### Opciones Evaluadas

| Solución | Tipo | Características | Coste |
|----------|------|-----------------|-------|
| **Cryptlex** | SaaS | Completo, API robusta, offline | $49-249/mes |
| **Keygen** | SaaS | Moderno, REST API | $29-99/mes |
| **LicenseSpring** | SaaS | Integraciones, analytics | Custom |
| **Standard.Licensing** | Open Source | Básico, local | Gratis |
| **Portable.Licensing** | Open Source | XML-based, firmas | Gratis |
| **Custom** | Propio | Control total | Desarrollo |

### Recomendación: Sistema Híbrido

Para un proyecto indie, recomiendo un **sistema propio con validación online**:

1. **Generación de seriales** propia (algoritmo seguro)
2. **Validación online** contra API propia
3. **Gracia offline** de 7-14 días sin internet
4. **Binding a hardware** para evitar compartir

### Formato de Serial Propuesto

```
Formato: XXXX-XXXX-XXXX-XXXX
Ejemplo: INK1-A3B7-C9D2-E5F8

Estructura:
┌────┬────────────────────────────────────────────────────────┐
│INK1│ Prefijo del producto (Ataena v1)                   │
├────┼────────────────────────────────────────────────────────┤
│A3B7│ ID único del cliente (codificado)                     │
├────┼────────────────────────────────────────────────────────┤
│C9D2│ Tipo de licencia + fecha de expiración (codificado)   │
├────┼────────────────────────────────────────────────────────┤
│E5F8│ Checksum de validación                                │
└────┴────────────────────────────────────────────────────────┘
```

### Tipos de Licencia

| Tipo | Código | Activaciones | Descripción |
|------|--------|--------------|-------------|
| **Trial** | `TRI` | 1 | 14-30 días, limitaciones |
| **Personal** | `PER` | 1 | 1 PC, tatuador individual |
| **Estudio** | `EST` | 3 | Hasta 3 PCs |
| **Enterprise** | `ENT` | Ilimitado | Sin límites |

### Flujo de Activación

```
┌─────────────────────────────────────────────────────────────┐
│                    FLUJO DE ACTIVACIÓN                       │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  1. Usuario compra licencia en web                          │
│     ↓                                                       │
│  2. Recibe email con serial: INK1-XXXX-XXXX-XXXX            │
│     ↓                                                       │
│  3. Instala Ataena                                       │
│     ↓                                                       │
│  4. Primer inicio → Pantalla de activación                  │
│     ┌─────────────────────────────────────────────────┐     │
│     │  🔑 Activar Ataena                           │     │
│     │                                                   │     │
│     │  Introduce tu clave de licencia:                 │     │
│     │  ┌─────────────────────────────────────────────┐ │     │
│     │  │ INK1-XXXX-XXXX-XXXX                         │ │     │
│     │  └─────────────────────────────────────────────┘ │     │
│     │                                                   │     │
│     │  [Activar]   [Iniciar prueba gratuita]          │     │
│     └─────────────────────────────────────────────────┘     │
│     ↓                                                       │
│  5. Cliente envía a servidor:                               │
│     - Serial                                                │
│     - Hardware ID (hash de CPU, disco, MAC)                 │
│     - Versión de la app                                     │
│     ↓                                                       │
│  6. Servidor valida:                                        │
│     - Serial existe y no está revocado                      │
│     - No excede límite de activaciones                      │
│     - Hardware ID no bloqueado                              │
│     ↓                                                       │
│  7. Servidor responde con token de licencia                 │
│     - Token firmado digitalmente                            │
│     - Fecha de expiración                                   │
│     - Funcionalidades habilitadas                           │
│     ↓                                                       │
│  8. Cliente guarda token localmente (cifrado)               │
│     ↓                                                       │
│  9. ¡Aplicación activada! ✅                                │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### Validación Periódica

```
Cada inicio de la aplicación:
│
├── 1. Leer token de licencia local
│
├── 2. Verificar firma digital del token
│
├── 3. ¿Token expirado?
│   ├── SÍ → Intentar renovar online
│   └── NO → Continuar
│
├── 4. ¿Conexión a internet disponible?
│   ├── SÍ → Validar con servidor (background)
│   │        └── Actualizar token si hay cambios
│   │
│   └── NO → ¿Días sin validar < 14?
│            ├── SÍ → Permitir uso (modo gracia)
│            └── NO → Bloquear hasta tener conexión
│
└── 5. Continuar con la aplicación
```

### Base de Datos de Licencias (Servidor)

```sql
-- Tabla: Licencias
CREATE TABLE Licencias (
    Id INT PRIMARY KEY,
    Serial VARCHAR(19) UNIQUE,
    TipoLicencia VARCHAR(10),
    EmailCliente VARCHAR(255),
    NombreCliente VARCHAR(255),
    FechaCompra DATETIME,
    FechaExpiracion DATETIME NULL,  -- NULL = perpetua
    MaxActivaciones INT DEFAULT 1,
    Activa BIT DEFAULT 1,
    Notas TEXT
);

-- Tabla: Activaciones
CREATE TABLE Activaciones (
    Id INT PRIMARY KEY,
    LicenciaId INT FOREIGN KEY,
    HardwareId VARCHAR(64),
    NombrePC VARCHAR(255),
    FechaActivacion DATETIME,
    UltimaValidacion DATETIME,
    VersionApp VARCHAR(20),
    Activa BIT DEFAULT 1
);

-- Tabla: Logs
CREATE TABLE LogsActivacion (
    Id INT PRIMARY KEY,
    LicenciaId INT,
    Evento VARCHAR(50),  -- ACTIVACION, VALIDACION, DESACTIVACION, BLOQUEO
    IP VARCHAR(45),
    Fecha DATETIME,
    Detalles TEXT
);
```

### Decisiones Pendientes - Licencias

- [ ] ¿Precio de licencia?
  - Perpetua: 149€, 179€, 199€?
  - Anual: 49€, 69€, 79€?

- [ ] ¿Período de prueba?
  - 14 días vs 30 días
  - ¿Con o sin funcionalidades limitadas?

- [ ] ¿Límite de activaciones por licencia?
  - Personal: 1-2 PCs
  - Estudio: 3-5 PCs

- [ ] ¿Permitir transferencia de licencia?
  - ¿Entre PCs del mismo usuario?
  - ¿Entre usuarios diferentes?

- [ ] ¿Gracia offline?
  - 7 días, 14 días, 30 días

---

## 🛡️ Medidas Antipiratería

### Niveles de Protección

```
┌─────────────────────────────────────────────────────────────┐
│               CAPAS DE PROTECCIÓN                            │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌───────────────────────────────────────────────────────┐ │
│  │  CAPA 1: Ofuscación de Código                         │ │
│  │  • Renombrar clases, métodos, variables               │ │
│  │  • Cifrar strings (seriales, URLs, mensajes)          │ │
│  │  • Control flow obfuscation                           │ │
│  │  • Anti-debugging / Anti-tampering                    │ │
│  │  Herramientas: ConfuserEx, Obfuscar, .NET Reactor     │ │
│  └───────────────────────────────────────────────────────┘ │
│                          ↓                                   │
│  ┌───────────────────────────────────────────────────────┐ │
│  │  CAPA 2: Verificación de Integridad                   │ │
│  │  • Hash del ejecutable al inicio                      │ │
│  │  • Verificar firma digital                            │ │
│  │  • Detectar modificaciones en DLLs                    │ │
│  └───────────────────────────────────────────────────────┘ │
│                          ↓                                   │
│  ┌───────────────────────────────────────────────────────┐ │
│  │  CAPA 3: Binding a Hardware                           │ │
│  │  • Hardware ID basado en CPU, disco, MAC              │ │
│  │  • Licencia vinculada a máquina específica            │ │
│  │  • Detectar VMs/emuladores (opcional)                 │ │
│  └───────────────────────────────────────────────────────┘ │
│                          ↓                                   │
│  ┌───────────────────────────────────────────────────────┐ │
│  │  CAPA 4: Validación Online                            │ │
│  │  • Verificación periódica con servidor                │ │
│  │  • Detectar seriales compartidos (mismo serial, != HW)│ │
│  │  • Revocar licencias fraudulentas                     │ │
│  │  • Analytics de uso (geografía, versiones)            │ │
│  └───────────────────────────────────────────────────────┘ │
│                          ↓                                   │
│  ┌───────────────────────────────────────────────────────┐ │
│  │  CAPA 5: Limitaciones de Trial                        │ │
│  │  • Marca de agua en PDFs generados                    │ │
│  │  • Límite de clientes (ej: 10 clientes max)           │ │
│  │  • Funcionalidades deshabilitadas                     │ │
│  │  • Recordatorios de compra                            │ │
│  └───────────────────────────────────────────────────────┘ │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### Herramientas de Ofuscación

| Herramienta | Tipo | Nivel | Coste |
|-------------|------|-------|-------|
| **ConfuserEx** | Open Source | Medio-Alto | Gratis |
| **Obfuscar** | Open Source | Básico | Gratis |
| **.NET Reactor** | Comercial | Alto | 179€ |
| **Eazfuscator.NET** | Comercial | Alto | 399€ |
| **Dotfuscator** | Comercial | Alto | Incluido VS Ent |

### Recomendación: ConfuserEx + Verificaciones Propias

1. **ConfuserEx** para ofuscación básica (gratis)
2. **Verificaciones de integridad** propias en código
3. **Validación online** obligatoria

### Hardware ID - Generación

```csharp
// Ejemplo conceptual de generación de Hardware ID
public static string GenerarHardwareId()
{
    var datos = new StringBuilder();
    
    // ID de CPU
    datos.Append(ObtenerCpuId());
    
    // Serial del disco principal
    datos.Append(ObtenerDiskSerial());
    
    // MAC address de la primera NIC
    datos.Append(ObtenerMacAddress());
    
    // Nombre del equipo (opcional, puede cambiar)
    // datos.Append(Environment.MachineName);
    
    // Generar hash SHA256
    using var sha = SHA256.Create();
    var bytes = Encoding.UTF8.GetBytes(datos.ToString());
    var hash = sha.ComputeHash(bytes);
    
    return Convert.ToBase64String(hash).Substring(0, 32);
}
```

### Detección de Piratería

| Señal | Acción |
|-------|--------|
| Serial usado en >N dispositivos | Bloquear nuevas activaciones |
| Mismo serial desde diferentes países | Marcar para revisión manual |
| Modificación del ejecutable detectada | No iniciar, mostrar error |
| Debugger detectado | Comportamiento degradado |
| Fecha del sistema retrocedida | Marcar como sospechoso |

### Respuesta a Piratería (Soft vs Hard)

**Enfoque SOFT (Recomendado):**
- Mostrar recordatorios amigables
- Degradar funcionalidad gradualmente
- No destruir datos del usuario
- Ofrecer descuentos para regularizar

**Enfoque HARD:**
- Bloquear completamente
- Reportar a servidor
- Deshabilitar trial permanentemente

### Decisiones Pendientes - Antipiratería

- [ ] ¿Nivel de ofuscación?
  - Básico (ConfuserEx gratis)
  - Avanzado (.NET Reactor, 179€)

- [ ] ¿Respuesta a piratería detectada?
  - Soft: degradar funcionalidad
  - Hard: bloquear completamente

- [ ] ¿Detectar VMs/emuladores?
  - Pro: Más difícil de crackear
  - Contra: Usuarios legítimos en VMs

---

## 🏗️ Infraestructura Necesaria

### Componentes Requeridos

```
┌─────────────────────────────────────────────────────────────┐
│                    INFRAESTRUCTURA                          │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌───────────────────────────────────────────────────────┐ │
│  │  1. SITIO WEB                                         │ │
│  │  • Landing page del producto                          │ │
│  │  • Página de compra / checkout                        │ │
│  │  • Portal de cliente (descargas, licencias)           │ │
│  │  • Blog / changelog                                   │ │
│  │  Hosting: Vercel, Netlify, o VPS                      │ │
│  │  Coste: 0-20€/mes                                     │ │
│  └───────────────────────────────────────────────────────┘ │
│                          │                                   │
│  ┌───────────────────────────────────────────────────────┐ │
│  │  2. API DE LICENCIAS                                  │ │
│  │  • Endpoints: activar, validar, desactivar            │ │
│  │  • Autenticación de requests                          │ │
│  │  • Rate limiting                                      │ │
│  │  Hosting: Railway, Render, Azure, AWS                 │ │
│  │  Coste: 5-25€/mes                                     │ │
│  └───────────────────────────────────────────────────────┘ │
│                          │                                   │
│  ┌───────────────────────────────────────────────────────┐ │
│  │  3. BASE DE DATOS                                     │ │
│  │  • Licencias, activaciones, logs                      │ │
│  │  • Clientes y órdenes                                 │ │
│  │  Opciones: PostgreSQL, MySQL, SQLite                  │ │
│  │  Hosting: Railway, PlanetScale, Supabase              │ │
│  │  Coste: 0-10€/mes                                     │ │
│  └───────────────────────────────────────────────────────┘ │
│                          │                                   │
│  ┌───────────────────────────────────────────────────────┐ │
│  │  4. CDN / STORAGE (Actualizaciones)                   │ │
│  │  • Archivos de instalación                            │ │
│  │  • Delta updates                                      │ │
│  │  Opciones: GitHub Releases, S3, Azure Blob, Backblaze │ │
│  │  Coste: 0-10€/mes                                     │ │
│  └───────────────────────────────────────────────────────┘ │
│                          │                                   │
│  ┌───────────────────────────────────────────────────────┐ │
│  │  5. PASARELA DE PAGO                                  │ │
│  │  • Stripe, PayPal, Paddle, Gumroad                    │ │
│  │  • Gestión de impuestos (IVA)                         │ │
│  │  • Facturas automáticas                               │ │
│  │  Coste: 2.9% + 0.30€ por transacción                  │ │
│  └───────────────────────────────────────────────────────┘ │
│                          │                                   │
│  ┌───────────────────────────────────────────────────────┐ │
│  │  6. EMAIL TRANSACCIONAL                               │ │
│  │  • Envío de seriales                                  │ │
│  │  • Confirmaciones de compra                           │ │
│  │  • Notificaciones de actualizaciones                  │ │
│  │  Opciones: Resend, SendGrid, Postmark                 │ │
│  │  Coste: 0-10€/mes                                     │ │
│  └───────────────────────────────────────────────────────┘ │
│                                                             │
│  COSTE TOTAL ESTIMADO: 10-80€/mes                          │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### Opción Simplificada: Gumroad/Paddle

Servicios como **Gumroad** o **Paddle** ofrecen:
- Checkout y pagos
- Entrega de archivos
- Generación de licencias
- Emails automáticos
- Gestión de impuestos

Coste: ~5-10% de cada venta (pero sin infraestructura que mantener)

---

## ❓ Decisiones Pendientes

### Prioridad Alta (Definir antes de implementar)

| # | Decisión | Opciones | Recomendación |
|---|----------|----------|---------------|
| 1 | Modelo de negocio | Perpetua vs Suscripción | **Perpetua** (más simple) |
| 2 | Precio | 99€, 149€, 199€ | **149€** (balance) |
| 3 | Trial | 14 vs 30 días | **14 días** |
| 4 | Self-contained | Sí vs No | **Sí** (menos problemas) |
| 5 | Firma digital | Sí vs No | **Sí** (profesional) |

### Prioridad Media

| # | Decisión | Opciones | Recomendación |
|---|----------|----------|---------------|
| 6 | Ofuscador | ConfuserEx vs .NET Reactor | **ConfuserEx** (gratis) |
| 7 | Hosting updates | GitHub vs S3 | **GitHub Releases** |
| 8 | Pasarela de pago | Stripe vs Gumroad | **Gumroad** (simple) |
| 9 | API de licencias | Propia vs Cryptlex | **Propia** (control) |

### Prioridad Baja (Decidir más tarde)

| # | Decisión | Opciones |
|---|----------|----------|
| 10 | Múltiples idiomas | Español, Inglés, ... |
| 11 | Licencias por suscripción | Añadir opción anual |
| 12 | Programa de afiliados | % para referidos |

---

## 📅 Plan de Implementación

### Fases del Proyecto

```
┌─────────────────────────────────────────────────────────────┐
│                 FASES DE IMPLEMENTACIÓN                      │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  FASE 1: Instalador Básico                    [2-3 días]   │
│  ├── Configurar Velopack o Inno Setup                      │
│  ├── Crear script de instalación                           │
│  ├── Incluir .NET Runtime                                  │
│  ├── Crear accesos directos                                │
│  ├── Probar en máquinas limpias                            │
│  └── Desinstalador                                         │
│                                                             │
│  FASE 2: Sistema de Licencias                 [5-7 días]   │
│  ├── Diseñar formato de serial                             │
│  ├── Crear API de validación                               │
│  ├── Implementar pantalla de activación en app             │
│  ├── Hardware ID                                           │
│  ├── Token local cifrado                                   │
│  ├── Modo gracia offline                                   │
│  └── Testing exhaustivo                                    │
│                                                             │
│  FASE 3: Actualizaciones                      [2-3 días]   │
│  ├── Configurar servidor de updates                        │
│  ├── Integrar Velopack en la app                           │
│  ├── UI de notificación de updates                         │
│  ├── Delta updates                                         │
│  └── Rollback                                              │
│                                                             │
│  FASE 4: Antipiratería                        [2-3 días]   │
│  ├── Configurar ConfuserEx                                 │
│  ├── Verificación de integridad                            │
│  ├── Anti-debugging básico                                 │
│  └── Probar que la app sigue funcionando                   │
│                                                             │
│  FASE 5: Infraestructura                      [3-5 días]   │
│  ├── Crear landing page                                    │
│  ├── Configurar Gumroad/Stripe                             │
│  ├── Integrar webhook para generar seriales                │
│  ├── Email de entrega automática                           │
│  └── Probar flujo completo de compra                       │
│                                                             │
│  FASE 6: Documentación y Lanzamiento          [2-3 días]   │
│  ├── Documentación de usuario                              │
│  ├── FAQ                                                   │
│  ├── Política de reembolso                                 │
│  ├── Términos y condiciones                                │
│  └── Lanzamiento beta cerrada                              │
│                                                             │
│  TOTAL ESTIMADO: 16-24 días de desarrollo                  │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

## 💰 Presupuesto Estimado

### Costes de Desarrollo (Únicos)

| Concepto | Coste | Notas |
|----------|-------|-------|
| Certificado firma código | 200-400€/año | Comodo, DigiCert, etc. |
| Dominio | 10-15€/año | ataena.es / .com |
| Logo profesional | 0-100€ | Opcional si ya existe |
| **Total único** | **210-515€** | |

### Costes Mensuales (Operativos)

| Concepto | Coste/mes | Notas |
|----------|-----------|-------|
| Hosting web | 0-10€ | Vercel free tier |
| API hosting | 0-10€ | Railway free tier |
| Base de datos | 0-5€ | Supabase free tier |
| CDN/Storage | 0-5€ | GitHub Releases gratis |
| Email | 0-5€ | Resend free tier |
| **Total mensual** | **0-35€** | |

### Costes por Venta (Variables)

| Pasarela | Comisión | Para venta de 149€ |
|----------|----------|---------------------|
| Stripe | 2.9% + 0.30€ | 4.62€ |
| PayPal | 3.49% + 0.49€ | 5.69€ |
| Gumroad | 10% | 14.90€ |
| Paddle | 5% + fees | ~10€ |

### Punto de Equilibrio

Con costes mensuales de ~35€ y precio de 149€:
- **Mínimo ventas/mes:** 1 licencia = 114€ neto (después de comisiones)
- **Break-even:** 1 venta cada 3-4 meses cubre costes

---

## 📚 Referencias y Recursos

### Herramientas

- [Velopack](https://github.com/velopack/velopack) - Instalador + Updates
- [Inno Setup](https://jrsoftware.org/isinfo.php) - Instalador tradicional
- [ConfuserEx](https://github.com/yck1509/ConfuserEx) - Ofuscación
- [Gumroad](https://gumroad.com/) - Venta de software
- [Stripe](https://stripe.com/) - Pagos

### Documentación

- [MSIX Packaging](https://learn.microsoft.com/en-us/windows/msix/)
- [.NET Publishing](https://learn.microsoft.com/en-us/dotnet/core/deploying/)

---

## ✅ Siguientes Pasos

1. **Revisar este documento** y tomar decisiones pendientes
2. **Definir precio y modelo de licencia**
3. **Elegir herramientas** (Velopack vs Inno, ConfuserEx vs otro)
4. **Crear infraestructura mínima** (dominio, hosting)
5. **Implementar Fase 1** (instalador básico)

---

> **Nota:** Este documento es un plan inicial. Se actualizará conforme se tomen decisiones y avance el desarrollo.

