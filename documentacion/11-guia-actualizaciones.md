# Guía: Sistema de actualizaciones (Inno Setup + GitHub Releases)

> **Fecha:** 21 Feb 2026
> **Objetivo:** Publicar nuevas versiones de Ataena CRM y que los clientes las actualicen desde dentro de la app con un clic, sin Velopack.

---

## Resumen del flujo híbrido

| Momento                  | Herramienta        | Qué hace                                                                   |
| ------------------------ | ------------------ | -------------------------------------------------------------------------- |
| Primera instalación      | **Inno Setup**     | Asistente amigable (bienvenida, carpeta, accesos directos, UAC).           |
| Comprobación de versión  | `ActualizacionService` | Consulta `GitHub Releases` y compara con la versión del ensamblado.     |
| Descarga e instalación   | `ActualizacionService` | Descarga el `Ataena-Setup-X.Y.Z.exe` del release y lo ejecuta silencioso. |
| Reinstalación silenciosa | **Inno Setup**     | `/VERYSILENT /CLOSEAPPLICATIONS /RESTARTAPPLICATIONS` ⇒ cierra y relanza Ataena. |

No hay dependencia de Velopack. Los datos del usuario (`%LOCALAPPDATA%\Ataena\`) no se tocan nunca.

---

## 1. Cómo detecta la app una nueva versión

`Ataena/Services/ActualizacionService.cs`:

- Lee la versión actual del ensamblado (`<Version>` del `.csproj`).
- Llama a `https://api.github.com/repos/Jvalfdev/desktop-myos-app/releases/latest`.
- Parsea el `tag_name` (soporta `1.1.0` o `v1.1.0`).
- Si el tag es mayor y el release incluye un asset `Ataena-Setup-*.exe`, marca `HayActualizacion = true`.

`MainWindowViewModel` llama a `ComprobarAsync()` al arrancar, en un `Task.Run` que no bloquea la UI. Si hay versión nueva, muestra un banner arriba a la derecha con:

- **Más tarde** → oculta el banner hasta el próximo arranque.
- **Instalar ahora** → descarga el `.exe` a `%TEMP%\Ataena\updates\`, muestra barra de progreso, lo lanza en silencio y cierra la app. Inno Setup reinstala, cierra el proceso y vuelve a abrir Ataena.

> Si la API de GitHub falla o no hay internet, el servicio no lanza excepciones: simplemente no se muestra el banner.

---

## 2. Requisitos previos (una sola vez)

### 2.1. Instalar GitHub CLI

```powershell
winget install --id GitHub.cli --silent --accept-source-agreements --accept-package-agreements
```

Cierra y reabre PowerShell para que `gh` quede en el PATH. Verifica con:

```powershell
gh --version
```

### 2.2. Autenticar `gh`

```powershell
gh auth login --hostname github.com --web --git-protocol https --skip-ssh-key
```

Te dará un código `XXXX-XXXX` y te abrirá `https://github.com/login/device`. Pega el código → Continue → autoriza con la cuenta `Jvalfdev`. El token queda guardado en el keyring de Windows.

Para confirmar:

```powershell
gh auth status
```

### 2.3. El repositorio debe ser público

El `ActualizacionService` consulta `https://api.github.com/repos/Jvalfdev/desktop-myos-app/releases/latest` **sin autenticación** desde el PC del cliente. Si el repo es privado, GitHub responde 404 y el banner nunca aparece.

Para hacerlo público (ya está hecho a partir de v1.0.2):

```powershell
gh repo edit Jvalfdev/desktop-myos-app --visibility public --accept-visibility-change-consequences
```

Para verificarlo:

```powershell
gh repo view Jvalfdev/desktop-myos-app --json visibility
# {"visibility":"PUBLIC"}
```

---

## 3. Publicar una nueva versión (paso a paso)

### 3.1. Subir la versión

Edita `Ataena/Ataena.csproj`:

```xml
<Version>1.1.0</Version>
```

Y `Ataena/Installer/Ataena.iss`:

```
#define MyAppVersion "1.1.0"
```

> Las dos versiones **deben coincidir**. El comparador usa la del ensamblado; el nombre del archivo usa la del `.iss`.

(Opcional) actualiza también la línea final de `Ataena/build-installer.ps1` para que el mensaje al final del build muestre la versión correcta.

### 3.2. Generar el instalador

```powershell
cd Ataena
./build-installer.ps1
```

Esto produce `Releases/Ataena-Setup-1.1.0.exe`.

### 3.3. Commit + push de los cambios

```powershell
cd ..
git add .
git commit -m "release: v1.1.0 - <resumen breve>"
git push origin main
```

### 3.4. Crear el tag y la release con `gh`

Comando en bloque (PowerShell):

```powershell
$version = "1.1.0"
$exe     = "Ataena/Releases/Ataena-Setup-$version.exe"
$notes = @"
## Cambios

- Bullet 1
- Bullet 2
"@

git tag "v$version"
git push origin "v$version"

gh release create "v$version" $exe `
  --title "Ataena CRM $version" `
  --notes $notes `
  --latest
```

Lo que hace `gh release create`:

1. Crea la release asociada al tag `v1.1.0`.
2. Sube el `.exe` como asset (el nombre se mantiene → será `Ataena-Setup-1.1.0.exe`).
3. La marca como **Latest** (`--latest`), que es lo que mira `/releases/latest`.

**Requisitos del release** (los cumple el comando de arriba pero conviene tenerlos presentes):

1. Tag `vX.Y.Z` o `X.Y.Z` (ambos válidos para el comparador).
2. Un asset cuyo nombre empiece por `Ataena-Setup` y termine en `.exe`.
3. Que **no** sea `draft` ni `prerelease` (el endpoint `/releases/latest` los ignora).

### 3.5. Verificar que la API pública responde

Desde cualquier PowerShell (no necesita estar autenticado):

```powershell
$resp = Invoke-RestMethod -Uri "https://api.github.com/repos/Jvalfdev/desktop-myos-app/releases/latest" -Headers @{ "User-Agent" = "Ataena-test" }
$resp | Select-Object tag_name, name, draft, prerelease
$resp.assets | Select-Object name, size
```

Debe devolver el `tag_name` correcto y al menos un asset `Ataena-Setup-X.Y.Z.exe`.

### 3.6. Verificar desde el cliente

- Abre cualquier instalación anterior de Ataena.
- A los pocos segundos debe aparecer el banner "Nueva versión 1.1.0 disponible".
- Pulsa **Instalar ahora** → descarga, cierra, reinstala y relanza.
- Los datos del usuario siguen intactos en `%LOCALAPPDATA%\Ataena\`.

---

## 3. Ficheros implicados

| Archivo                                       | Función                                                         |
| --------------------------------------------- | --------------------------------------------------------------- |
| `Ataena/Services/ActualizacionService.cs`     | Comprobación, descarga y ejecución del instalador.              |
| `Ataena/ViewModels/MainWindowViewModel.cs`    | Arranca la comprobación y expone propiedades/commands al banner. |
| `Ataena/Views/MainWindow.axaml`               | Banner UI en la esquina superior derecha.                       |
| `Ataena/Installer/Ataena.iss`                 | Script Inno Setup (versión + metadatos del .exe).               |
| `Ataena/build-installer.ps1`                  | Publica + compila el instalador.                                |

---

## 4. Parámetros silenciosos usados

El instalador se invoca con:

```
/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /CLOSEAPPLICATIONS /RESTARTAPPLICATIONS
```

- `/VERYSILENT`: sin ventanas ni barra de progreso.
- `/SUPPRESSMSGBOXES`: responde OK automáticamente a cualquier mensaje.
- `/NORESTART`: no reinicia Windows.
- `/CLOSEAPPLICATIONS`: cierra Ataena automáticamente si sigue abierto.
- `/RESTARTAPPLICATIONS`: lo vuelve a abrir al terminar.

`UseShellExecute = true` en el `ProcessStartInfo` permite que Windows muestre el UAC del instalador si el usuario no es admin.

---

## 5. Preguntas frecuentes

**¿Qué pasa si el usuario cancela el UAC?**
El instalador no se ejecuta y la app sigue corriendo como si nada. El banner se volverá a mostrar en el siguiente arranque.

**¿Qué pasa con los datos del estudio al actualizar?**
Nada. La base de datos (`%LOCALAPPDATA%\Ataena\data.db`), los ficheros subidos y la configuración viven **fuera** de `Program Files`, así que la reinstalación no los toca.

**¿Puedo forzar una comprobación manual?**
Por ahora se ejecuta solo al abrir la app. Si lo necesitas, basta con añadir un botón en Configuración que llame a `ComprobarActualizacionesAsync()` del `MainWindowViewModel`.

**¿Y si quiero hacer una prerelease para pruebas?**
El endpoint `/releases/latest` ignora prereleases, así que puedes subir `v1.1.0-beta` como prerelease y no dispararás avisos en producción. Solo se anunciará al marcarla como release normal.

---

## 6. Checklist rápido para publicar

- [ ] `<Version>X.Y.Z</Version>` en `Ataena/Ataena.csproj`.
- [ ] `#define MyAppVersion "X.Y.Z"` en `Ataena/Installer/Ataena.iss`.
- [ ] `cd Ataena; ./build-installer.ps1`.
- [ ] `git add . ; git commit -m "release: vX.Y.Z - ..." ; git push origin main`.
- [ ] `git tag vX.Y.Z ; git push origin vX.Y.Z`.
- [ ] `gh release create vX.Y.Z Ataena/Releases/Ataena-Setup-X.Y.Z.exe --title "Ataena CRM X.Y.Z" --notes "..." --latest`.
- [ ] `Invoke-RestMethod https://api.github.com/repos/Jvalfdev/desktop-myos-app/releases/latest -Headers @{"User-Agent"="t"} | Select tag_name, name` devuelve el tag correcto.
- [ ] El banner aparece en una instalación previa al abrir la app.

---

## 7. Historial verificado

| Versión | Tag    | Fecha        | Notas                                                                                  |
| ------- | ------ | ------------ | -------------------------------------------------------------------------------------- |
| 1.0.0   | v1.0.0 | 17 abr 2026  | Primera versión "tester" con instalador Inno Setup.                                    |
| 1.0.1   | v1.0.1 | 21 abr 2026  | Fix transición Setup inicial → MainWindow (crashe).                                    |
| 1.0.2   | v1.0.2 | 6 may 2026   | Botones del setup centrados, consentimiento completo en móvil, refresh ficha cliente. Repo pasado a público; primera release verificada con `gh` y `gh repo edit --visibility public`. |
