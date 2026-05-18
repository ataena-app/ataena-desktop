# Ataena CRM

> CRM de escritorio para estudios de tatuaje y piercing. Gestiona clientes, agenda, trabajos y consentimientos RGPD desde una sola aplicación nativa para Windows.

![Version](https://img.shields.io/badge/version-0.5.5--beta-a855f7)
![Platform](https://img.shields.io/badge/platform-Windows-0078d4)
![Framework](https://img.shields.io/badge/.NET-9.0-512bd4)
![UI](https://img.shields.io/badge/UI-Avalonia%2011-purple)
![License](https://img.shields.io/badge/license-MIT-green)

---

## ¿Qué es Ataena?

Ataena es una aplicación de escritorio pensada para pequeños estudios de tatuaje y piercing que necesitan llevar un control ordenado de su negocio sin depender de herramientas online ni suscripciones. Toda la información se guarda localmente en el equipo.

---

## Características

- **Dashboard personalizable** — Estadísticas del día, citas programadas, alertas de pendientes y acciones rápidas. Cada sección es configurable desde los ajustes.
- **Gestión de clientes** — Ficha completa por cliente: datos de contacto, historial de citas y trabajos, estado RGPD.
- **Agenda** — Vista de citas por día con control de estado (pendiente, confirmada, completada, cancelada).
- **Trabajos** — Registro de trabajos por cliente con descripción, zona corporal y seguimiento.
- **Consentimientos RGPD** — Generación de consentimientos informados en PDF con código QR, firma y archivo digital.
- **Actualizaciones automáticas** — La app consulta GitHub Releases al arrancar y ofrece actualizar con un clic, sin necesidad de descargar nada manualmente.
- **Backup y restauración** — Copia de seguridad de la base de datos desde la pantalla de configuración.

---

## Stack tecnológico

| Capa | Tecnología |
|------|-----------|
| UI | [Avalonia UI 11](https://avaloniaui.net/) |
| Patrón | MVVM con [CommunityToolkit.Mvvm](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/) |
| Framework | .NET 9 |
| Base de datos | SQLite + [Entity Framework Core 9](https://learn.microsoft.com/en-us/ef/core/) |
| PDF | [QuestPDF](https://www.questpdf.com/) |
| QR | [QRCoder](https://github.com/codebude/QRCoder) |
| Logs | [Serilog](https://serilog.net/) |
| Instalador | [Inno Setup](https://jrsoftware.org/isinfo.php) |

---

## Instalación

1. Ve a la sección de [Releases](https://github.com/Jvalfdev/desktop-myos-app/releases) y descarga el instalador de la última versión (`Ataena-Setup-X.Y.Z.exe`).
2. Ejecuta el instalador y sigue el asistente.
3. Ataena se instala en `Program Files` y crea accesos directos en el escritorio y el menú inicio.

**Requisitos:** Windows 10 / 11 (x64).

---

## Actualizaciones

Ataena comprueba automáticamente si hay una nueva versión disponible cada vez que se abre. Si la hay, muestra un banner en la parte superior con la opción de actualizar. El proceso es silencioso: descarga el instalador, lo ejecuta en segundo plano y relanza la aplicación automáticamente al terminar.

---

## Estado del proyecto

Actualmente en fase **beta** (`v0.5.x`). La aplicación es funcional y estable para uso diario, pero algunas características secundarias están aún en desarrollo.

Consulta el [CHANGELOG](./CHANGELOG.md) para ver el detalle de cambios por versión.

---

## Desarrollo local

```bash
# Clonar el repositorio
git clone https://github.com/Jvalfdev/desktop-myos-app.git
cd desktop-myos-app/Ataena

# Restaurar dependencias y compilar
dotnet build

# Aplicar migraciones de base de datos
dotnet ef database update

# Ejecutar
dotnet run
```

Para compilar el instalador se necesita tener [Inno Setup 6](https://jrsoftware.org/isinfo.php) instalado y ejecutar:

```powershell
.\build-installer.ps1
```

---

## Licencia

MIT — ver [LICENSE](./LICENSE) para más detalles.
