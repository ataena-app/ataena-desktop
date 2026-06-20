# CLAUDE.md — Ataena Desktop

CRM para estudios de tatuaje y piercing en España. Desktop Windows offline-first, con migración futura a Ataena Cloud.

## Stack
- .NET 9 / C# / Avalonia UI 11
- SQLite + EF Core 9 (migraciones en `Migrations/`)
- MVVM con CommunityToolkit.Mvvm
- Inyección de dependencias vía `Microsoft.Extensions.DependencyInjection`

## Estructura del proyecto
```
Ataena/
  Models/       → Entidades de dominio (Cliente, Cita, Trabajo, Consentimiento…)
  Data/         → AtaenaDbContext
  Migrations/   → Migraciones EF Core (nunca editar a mano)
  Services/     → Lógica de negocio (única capa que toca la BD)
  ViewModels/   → MVVM: coordinan UI con servicios, sin lógica de negocio
  Views/        → XAML + code-behind mínimo
  Converters/   → Value converters de Avalonia
  Plantillas/   → Plantillas .txt de consentimiento por CCAA
```

## Principios irrenunciables

### Seguridad (prioridad máxima)
- El DNI/NIE se almacena solo en texto, nunca como imagen ni fichero
- Nunca loguear datos personales: DNI, nombre, teléfono, email, fecha de nacimiento
- Los PDFs de consentimiento se guardan en `%LOCALAPPDATA%\Ataena\ficheros\` con acceso solo local
- Validar siempre que un cliente pertenece al estudio antes de operar sobre él
- No confiar en datos que vengan de la UI sin validar en el servicio

### Deuda técnica (cero tolerancia)
- Nunca dejar `// TODO` sin issue asociado
- Nunca saltarse una migración con `EnsureCreated()` o `DDL auto: create`
- Nunca poner lógica de negocio en un ViewModel ni en code-behind de una View
- Nunca acceder a `AtaenaDbContext` directamente desde un ViewModel — siempre a través de un servicio
- Si algo "funciona pero está feo", no se deja así: se crea un issue y se refactoriza

## Reglas de arquitectura
- **MVVM estricto:** View → ViewModel → Service → Repository (DbContext)
- Los servicios son la única capa que conoce EF Core
- Los ViewModels no instancian servicios directamente — se inyectan
- Las operaciones async no bloquean el hilo UI: siempre `await`, nunca `.Result` ni `.Wait()`
- Los errores se comunican al usuario mediante el sistema de notificaciones existente, nunca con excepciones sin capturar

## Base de datos
- Toda modificación de esquema = nueva migración EF (`dotnet ef migrations add`)
- Nunca modificar migraciones ya aplicadas
- Probar siempre backup/restauración ZIP tras cambios de esquema

## Preparación para Cloud (no implementar aún, tener en mente)
- Las entidades deben poder recibir `EstudioId`, `UpdatedAt`, `SyncVersion` sin romper nada
- Los servicios deben poder trabajar detrás de `IDataStore` cuando llegue el momento
- Las rutas de fichero deben poder convertirse a `StorageKey` opaco

## Modelo de datos
Tablas SQLite principales y sus relaciones:

| Tabla | Relaciones clave |
|-------|-----------------|
| `Clientes` | 1:N → Citas, Trabajos, Consentimientos |
| `Citas` | FK `ClienteId`, FK opcional `TrabajoId` |
| `Trabajos` | FK `ClienteId`; 1:1 opcional con Consentimiento |
| `Consentimientos` | FK `ClienteId`, FK opcional `TrabajoId`; campo `RutaDocumento` (ruta PDF local) |
| `Configuracion` | Singleton (Id=1): nombre estudio, SMTP, preferencias |
| `DiasFestivos` | Independiente |

## Flujos críticos — no romper nunca
- **Backup ZIP** (`BackupService`): contiene `data.db` + `ficheros/` + `metadata.json`. Es el mecanismo de migración a Cloud. Cualquier cambio de esquema debe mantener compatibilidad con el ZIP.
- **Firma QR en LAN** (`FirmaWebService`): servidor HTTP local que sirve la página de firma al cliente en el móvil. No depende de internet.
- **Actualizaciones automáticas** (`ActualizacionService`): consulta GitHub Releases en `ataena-app/ataena-desktop`. No cambiar owner ni repo sin actualizar estas constantes.

## RGPD — obligaciones en el desktop
- Los consentimientos PDF firmados son documentos legales — nunca borrar sin confirmación explícita del usuario
- El backup ZIP es el mecanismo de portabilidad (art. 20 RGPD) — siempre debe funcionar
- Los datos del estudio en `Configuracion` (CIF, dirección) aparecen en los PDFs — validar que estén completos antes de generar cualquier documento
- Las plantillas de consentimiento en `Plantillas/` son por CCAA; no mezclar plantillas entre comunidades

## Versioning
La versión se define en **dos sitios** — ambos deben estar sincronizados:
- `Ataena/Ataena.csproj` → `<Version>` y `<InformationalVersion>`
- `Ataena/Installer/Ataena.iss` → `#define MyAppVersion`

Cuando se sube una release: compilar en Release → generar instalador → crear GitHub Release con tag `v{version}` y subir el `.exe`.

## Cómo compilar y ejecutar
```bash
dotnet build
dotnet run --project Ataena
```

## Cómo generar instalador
Compilar en Release → abrir `Ataena/Installer/Ataena.iss` con Inno Setup → Build.

## Lo que NO existe aún (no implementar sin acuerdo previo)
- `IDataStore` / `CloudApiStore` — está planificado, no implementado
- Login o cuenta de usuario
- Sync con ningún servicio externo
- Cualquier llamada a internet salvo `ActualizacionService`
