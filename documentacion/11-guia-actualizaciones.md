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

## 2. Publicar una nueva versión (paso a paso)

### 2.1. Subir la versión

Edita `Ataena/Ataena.csproj`:

```xml
<Version>1.1.0</Version>
```

Y `Ataena/Installer/Ataena.iss`:

```
#define MyAppVersion "1.1.0"
```

> Las dos versiones **deben coincidir**. El comparador usa la del ensamblado; el nombre del archivo usa la del `.iss`.

### 2.2. Generar el instalador

```powershell
cd Ataena
./build-installer.ps1
```

Esto produce `Releases/Ataena-Setup-1.1.0.exe`.

### 2.3. Crear la release en GitHub

```powershell
# Tag y push
git tag v1.1.0
git push origin v1.1.0
```

Luego, en GitHub (web o `gh` CLI):

```powershell
gh release create v1.1.0 `
  "Releases/Ataena-Setup-1.1.0.exe" `
  --title "Ataena CRM 1.1.0" `
  --notes "Cambios de esta versión..."
```

**Requisitos del release:**

1. Tag **exactamente** `v1.1.0` o `1.1.0` (se acepta el prefijo `v`).
2. Un asset cuyo nombre empiece por `Ataena-Setup` y termine en `.exe`.
3. Que **no** sea `draft` ni `prerelease` (si lo es, el endpoint `/releases/latest` lo ignora).

### 2.4. Verificar desde el cliente

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

- [ ] Subir `<Version>` en `Ataena.csproj`.
- [ ] Subir `MyAppVersion` en `Ataena.iss`.
- [ ] `./build-installer.ps1`.
- [ ] `git tag vX.Y.Z && git push origin vX.Y.Z`.
- [ ] `gh release create vX.Y.Z Releases/Ataena-Setup-X.Y.Z.exe --title ... --notes ...`.
- [ ] Comprobar banner en una instalación antigua.
