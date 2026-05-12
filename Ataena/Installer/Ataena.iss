; Script de Inno Setup para Ataena CRM
; Crea un instalador con asistente completo (Bienvenida, elegir carpeta, accesos directos, etc.)

#define MyAppName "Ataena CRM"
#define MyAppVersion "0.5.0"
#define MyAppPublisher "Ataena"
#define MyAppURL "https://github.com/Jvalfdev/desktop-myos-app"
#define MyAppExeName "Ataena.exe"

[Setup]
AppId={{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
; Instalación estándar en Program Files (requiere admin la primera vez)
PrivilegesRequired=admin
OutputDir=..\Releases
OutputBaseFilename=Ataena-Setup-{#MyAppVersion}
SetupIconFile=..\Assets\avalonia-logo.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
WizardSizePercent=120
WizardResizable=yes
; Diseño moderno (compatible con Windows 10/11)
DisableWelcomePage=no
DisableProgramGroupPage=yes
; Crear desinstalador
UninstallDisplayName={#MyAppName}

; Restart Manager: cuando se reinstala sobre una version anterior con la app
; abierta, Inno Setup detecta Ataena.exe y lo cierra de forma limpia, y al
; terminar lo relanza automaticamente. Esto es lo que permite que el flujo
; de auto-update (lanzado con /VERYSILENT /CLOSEAPPLICATIONS /RESTARTAPPLICATIONS)
; vuelva a abrir la app por si solo.
CloseApplications=yes
RestartApplications=yes

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Crear un icono en el escritorio"; GroupDescription: "Iconos adicionales:"; Flags: checkedonce
Name: "quicklaunchicon"; Description: "Crear un icono en la barra de tareas"; GroupDescription: "Iconos adicionales:"; Flags: unchecked

[Files]
; Copiar todo el contenido de publish (generado por dotnet publish)
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Desinstalar {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: quicklaunchicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Abrir {#MyAppName}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; No borrar datos del usuario (%LocalAppData%\Ataena\ contiene data.db, ficheros, etc.)
; Solo desinstalamos los archivos de la aplicación
Type: filesandordirs; Name: "{app}"
