; ============================================================================
;  EHMR — Inno Setup скрипта за 64-битен инсталер
;
;  Се гради вака (од коренот на проектот):
;
;    dotnet publish EHMR.csproj -f net9.0-windows10.0.19041.0 -c Release ^
;        -r win10-x64 --self-contained true -p:WindowsPackageType=None
;
;    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" Installer\EHMR.iss
;
;  Резултат: Installer\Output\EHMR-Setup-1.0.0.exe
; ============================================================================

#define MyAppName "EHMR"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "EHMR"
#define MyAppExeName "EHMR.exe"

; Папката што ја произведува `dotnet publish`.
#define MyAppSrc "..\bin\Release\net9.0-windows10.0.19041.0\win10-x64\publish"

[Setup]
AppId={{8F3C1D42-6E2A-4B7F-9C15-EHMR00000001}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
UninstallDisplayIcon={app}\{#MyAppExeName}
OutputDir=Output
OutputBaseFilename=EHMR-Setup-{#MyAppVersion}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern

; ── 64-битно ──────────────────────────────────────────────────────────────
; x64compatible опфаќа и ARM64 со емулација. Инсталацијата оди во
; „Program Files", не во „Program Files (x86)".
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

; Апликацијата се инсталира за сите корисници, па бара администратор.
PrivilegesRequired=admin

[Languages]
; Inno 6 не носи македонски јазик во стандардната инсталација.
; За македонски: земи Macedonian.isl од Inno Setup Unofficial Translations
; и стави го во „Inno Setup 6\Languages", па додај го тука.
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Кратенка на работната површина"; GroupDescription: "Дополнително:"; Flags: unchecked

[Files]
; Целата објавена папка. therapy.db е намерно исклучена — базата се создава
; при првото подигање во %LOCALAPPDATA%\EHMR, не во папката на програмата.
Source: "{#MyAppSrc}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "*.pdb,therapy.db,*.db-shm,*.db-wal"

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Деинсталирај {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Пушти ја {#MyAppName}"; Flags: nowait postinstall skipifsilent

; НАМЕРНО нема [UninstallDelete] за %LOCALAPPDATA%\EHMR.
; Таму седи therapy.db со сите пациентски податоци — деинсталација не смее
; да ги брише. Ако е потребно целосно чистење, тоа се прави рачно.
