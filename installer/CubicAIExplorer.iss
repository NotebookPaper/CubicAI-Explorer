; CubicAI Explorer - Inno Setup Script
; Requires Inno Setup 6.x (https://jrsoftware.org/isinfo.php)
;
; Before compiling this script:
;   dotnet publish ..\src\CubicAIExplorer\CubicAIExplorer.csproj -p:PublishProfile=SingleFile
;
; Then open this .iss in Inno Setup Compiler and hit Build.

#define MyAppName "CubicAI Explorer"
#define MyAppVersion GetStringFileInfo("..\src\CubicAIExplorer\bin\publish\CubicAIExplorer.exe", "ProductVersion")
#define MyAppPublisher "CubicAI Explorer Contributors"
#define MyAppURL "https://github.com/nickvdyck/CubicAI-Explorer"
#define MyAppExeName "CubicAIExplorer.exe"
#define MyAppSourceDir "..\src\CubicAIExplorer\bin\publish"
#define MyAppIconFile "..\src\CubicAIExplorer\Resources\appicon-v2.ico"

[Setup]
AppId={{7C3A8E5D-4F2B-4A1E-9D6C-8B5F3E2A1D0C}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
LicenseFile=..\LICENSE
OutputDir=output
OutputBaseFilename=CubicAIExplorer-{#MyAppVersion}-setup
SetupIconFile={#MyAppIconFile}
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#MyAppSourceDir}\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
