# Guía: Instalador con Velopack (DESCARTADO)

> **Fecha:** 21 de Febrero 2026
> **Estado:** ⚠️ **Obsoleto.** Ataena ya no usa Velopack.
>
> Se mantiene este documento sólo como referencia histórica.

---

## 👉 Guías en vigor

- Instalador amigable (Inno Setup): [`10-guia-instalador-inno-setup.md`](10-guia-instalador-inno-setup.md)
- Sistema de actualizaciones (GitHub Releases): [`11-guia-actualizaciones.md`](11-guia-actualizaciones.md)

Motivos por los que se descartó Velopack:

1. El `Setup.exe` generado es one-click (consola negra) sin asistente.
2. `UpdateManager` exige una estructura de carpetas concreta (`current/`, `Update.exe`) incompatible con una instalación en `Program Files` hecha con Inno Setup.
3. Quedaba un `Update.exe` extra en la carpeta del usuario.

La solución actual es híbrida: Inno Setup para la primera instalación + un `ActualizacionService` propio que consulta GitHub Releases y relanza el instalador en modo silencioso.

---

## 📋 Por dónde empezar

### Resumen en 3 pasos
1. **Publicar** la app (`dotnet publish`)
2. **Empaquetar** con Velopack (`vpk pack`)
3. **Integrar** Velopack en la app (para actualizaciones)

---

## Paso 1: Instalar la herramienta vpk

```powershell
dotnet tool install -g vpk
```

Si ya la tienes y quieres actualizarla:
```powershell
dotnet tool update -g vpk
```

---

## Paso 2: Añadir paquete NuGet a la app

En `Ataena.csproj`, añadir:

```xml
<PackageReference Include="Velopack" Version="0.0.1298" />
```

*(Verificar versión actual en [nuget.org/packages/velopack](https://www.nuget.org/packages/velopack))*

---

## Paso 3: Modificar Program.cs (inicio de Velopack)

Al inicio de `Main()`, antes de todo:

```csharp
static void Main(string[] args)
{
    VelopackApp.Build().Run();
    
    // ... resto del código (LoggingService.Inicializar(), etc.)
}
```

Esto permite que Velopack maneje argumentos especiales durante instalación/actualización.

---

## Paso 4: Publicar la aplicación

```powershell
cd Ataena
dotnet publish -c Release -r win-x64 --self-contained true -o .\publish
```

**Opciones:**
- `--self-contained true` — Incluye .NET Runtime (no requiere que el usuario tenga .NET instalado)
- `-o .\publish` — Carpeta de salida

**Resultado:** Carpeta `publish/` con Ataena.exe, DLLs, wwwroot, Plantillas, etc.

---

## Paso 5: Empaquetar con vpk

```powershell
vpk pack --packId Ataena --packVersion 1.0.0 --packDir .\publish --mainExe Ataena.exe --packTitle "Ataena CRM" --icon .\Assets\avalonia-logo.ico
```

**Argumentos:**
| Argumento | Valor | Descripción |
|-----------|-------|-------------|
| `--packId` | Ataena | ID único (se usa en %LocalAppData%\Ataena) |
| `--packVersion` | 1.0.0 | Versión SemVer |
| `--packDir` | .\publish | Carpeta con los archivos publicados |
| `--mainExe` | Ataena.exe | Ejecutable principal |
| `--packTitle` | Ataena CRM | Nombre visible en accesos directos |
| `--icon` | (ruta) | Icono .ico para el instalador |

**Opcionales:**
- `--splashImage {ruta}` — Imagen de splash durante instalación (PNG, JPG, GIF animado)
- `--outputDir {ruta}` — Dónde guardar el resultado (por defecto `.\Releases`)
**Resultado:** Carpeta `Releases/` con:
- `Setup.exe` — Instalador one-click (consola negra, instala y abre la app sin preguntas)
- `Ataena-1.0.0-full.nupkg` — Paquete para actualizaciones
- `RELEASES` — Manifest para el servidor de updates

---

### Mejorar la experiencia del Setup.exe

El Setup.exe es one-click (consola negra → instala → abre la app). Para darle más presencia de marca durante la instalación, usa `--splashImage`:

```powershell
vpk pack ... --splashImage .\Assets\splash-instalacion.png
```

La imagen puede ser PNG, JPG o GIF animado. Se muestra mientras se instala en lugar de la consola negra.

**Nota:** La opción `--msi` (instalador con asistente clásico) no está disponible en la versión actual de vpk (0.0.1298). Si en el futuro vpk añade soporte MSI, se podría generar un instalador con wizard completo.

---

## Paso 6: Probar la instalación

1. Ejecutar `Setup.exe`
2. Verificar que instala en `%LocalAppData%\Ataena`
3. Verificar accesos directos (escritorio, menú inicio)
4. Abrir la app y comprobar que funciona

---

## Paso 7: Integrar actualizaciones (después del instalador)

En la app, añadir comprobación de actualizaciones. Ejemplo en `App.axaml.cs` o al iniciar `MainWindow`:

```csharp
// Usando GitHub Releases
var mgr = new UpdateManager(new GitHubSource("https://github.com/Jvalfdev/desktop-myos-app"));
var newVersion = await mgr.CheckForUpdatesAsync();
if (newVersion != null)
{
    // Mostrar diálogo "Nueva versión disponible"
    // Si usuario acepta:
    await mgr.DownloadUpdatesAsync(newVersion);
    mgr.ApplyUpdatesAndRestart(newVersion);
}
```

---

## Paso 8: Publicar en GitHub Releases

1. Crear tag: `git tag v1.0.0`
2. Subir a GitHub: `git push origin v1.0.0`
3. En GitHub: Releases → Create new release
4. Seleccionar tag v1.0.0
5. Subir archivos de `Releases/`:
   - `Setup.exe`
   - `Ataena-1.0.0-full.nupkg`
   - `RELEASES`

---

## ⚠️ Importante: Datos del usuario

Velopack instala en `%LocalAppData%\Ataena\current\` y **reemplaza esa carpeta en cada actualización**.

Ataena ya guarda datos en `%LocalAppData%\Ataena\` (data.db, ficheros, logs, backups) — **fuera** de la carpeta `current`. Eso está bien: los datos del usuario no se borran al actualizar.

Estructura actual de Ataena:
```
%LocalAppData%\Ataena\
├── data.db          ← Datos (NO en current)
├── ficheros\        ← Archivos (NO en current)
├── backups\         ← Backups (NO en current)
├── logs\            ← Logs (NO en current)
└── current\         ← Lo que instala Velopack (se reemplaza al actualizar)
    ├── Ataena.exe
    └── ...
```

**Verificar:** Que la app use rutas relativas a `%LocalAppData%\Ataena\` y NO a la carpeta del ejecutable. Actualmente usa `Environment.GetFolderPath(LocalApplicationData)` + "Ataena", así que está correcto.

---

## ⚠️ Si falla el publish con NETSDK1047

Añade en el `.csproj` dentro de `<PropertyGroup>`:
```xml
<RuntimeIdentifiers>win-x64</RuntimeIdentifiers>
```

---

## ✅ Checklist

- [ ] Instalar `vpk` (`dotnet tool install -g vpk`)
- [ ] Añadir paquete Velopack al .csproj
- [ ] Añadir `VelopackApp.Build().Run()` en Program.cs
- [ ] Configurar versión en .csproj (`<Version>1.0.0</Version>`)
- [ ] Publicar: `dotnet publish -c Release -r win-x64 --self-contained true -o .\publish`
- [ ] Empaquetar: `vpk pack ...`
- [ ] Probar Setup.exe en tu PC
- [ ] (Después) Integrar UpdateManager para actualizaciones
- [ ] (Después) Subir a GitHub Releases

---

## 📚 Referencias

- [Velopack Docs](https://docs.velopack.io/)
- [Integrating Overview](https://docs.velopack.io/integrating/overview)
- [Windows Packaging](https://docs.velopack.io/packaging/operating-systems/windows)
