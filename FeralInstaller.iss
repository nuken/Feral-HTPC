[Setup]
AppName=Feral HTPC
AppVersion=0.1.0-alpha
AppPublisher=Feral HTPC Development
VersionInfoCompany=Feral HTPC Development
VersionInfoProductName=Feral HTPC Setup
VersionInfoProductVersion=0.1.0.0
VersionInfoProductTextVersion=0.1.0-alpha
VersionInfoCopyright=Copyright (C) 2026 Feral HTPC Development
AppPublisherURL=https://github.com/nuken/Feral-HTPC
DefaultDirName={autopf}\FeralHTPC
DefaultGroupName=Feral HTPC
OutputBaseFilename=Feral_HTPC_Alpha_Setup
UninstallDisplayIcon={app}\FeralHTPC.exe
Compression=lzma2
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64compatible
; NEW: Explicitly tell Inno to drop the finished .exe into an Output folder
OutputDir=Output

[Files]
; FIX: Use a relative path from the root of your GitHub repository
Source: "bin\Release\net8.0-windows\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\Feral HTPC"; Filename: "{app}\FeralsHTPC.exe"
Name: "{autodesktop}\Feral HTPC"; Filename: "{app}\FeralHTPC.exe"

[Run]
Filename: "{app}\FeralHTPC.exe"; Description: "{cm:LaunchProgram,Feral HTPC}"; Flags: nowait postinstall skipifsilent
