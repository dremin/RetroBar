#define RetroBarName "RetroBar"
#define RetroBarVersion "BUILD_VERSION"
#define RetroBarPublisher "Sam Johnson"
#define RetroBarURL "https://github.com/dremin/RetroBar"
#define RetroBarExeName "RetroBar.exe"

#define DotNetVersionDownload "6.0.36"
#define DotNetVersionMinimum "6.0.2"
#define DotNetVersionMaximum "6.1.0"
#define DotNetInstallerExe "RetroBar_DotNetRuntimeInstaller.exe"

#define TargetFramework "net6.0-windows"

[Setup]
AppId={{574527FE-00A4-4F85-92AD-B4B8B4077D73}
AppMutex={#RetroBarName}
AppName={#RetroBarName}
AppVersion={#RetroBarVersion}
AppVerName={#RetroBarName} {#RetroBarVersion}
AppPublisher={#RetroBarPublisher}
AppPublisherURL={#RetroBarURL}
AppSupportURL={#RetroBarURL}
AppUpdatesURL={#RetroBarURL}
DefaultDirName={autopf}\{#RetroBarName}
UninstallDisplayIcon={app}\{#RetroBarExeName}
SetupIconFile=RetroBar\Resources\retrobar.ico
ArchitecturesInstallIn64BitMode=x64compatible or arm64
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
OutputBaseFilename=RetroBarSetup
OutputDir=bin
Compression=lzma2
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "armenian"; MessagesFile: "compiler:Languages\Armenian.isl"
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"
Name: "bulgarian"; MessagesFile: "compiler:Languages\Bulgarian.isl"
Name: "catalan"; MessagesFile: "compiler:Languages\Catalan.isl"
Name: "corsican"; MessagesFile: "compiler:Languages\Corsican.isl"
Name: "czech"; MessagesFile: "compiler:Languages\Czech.isl"
Name: "danish"; MessagesFile: "compiler:Languages\Danish.isl"
Name: "dutch"; MessagesFile: "compiler:Languages\Dutch.isl"
Name: "finnish"; MessagesFile: "compiler:Languages\Finnish.isl"
Name: "french"; MessagesFile: "compiler:Languages\French.isl"
Name: "german"; MessagesFile: "compiler:Languages\German.isl"
Name: "hebrew"; MessagesFile: "compiler:Languages\Hebrew.isl"
Name: "hungarian"; MessagesFile: "compiler:Languages\Hungarian.isl"
Name: "icelandic"; MessagesFile: "compiler:Languages\Icelandic.isl"
Name: "italian"; MessagesFile: "compiler:Languages\Italian.isl"
Name: "japanese"; MessagesFile: "compiler:Languages\Japanese.isl"
Name: "korean"; MessagesFile: "compiler:Languages\Korean.isl"
Name: "norwegian"; MessagesFile: "compiler:Languages\Norwegian.isl"
Name: "polish"; MessagesFile: "compiler:Languages\Polish.isl"
Name: "portuguese"; MessagesFile: "compiler:Languages\Portuguese.isl"
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"
Name: "slovak"; MessagesFile: "compiler:Languages\Slovak.isl"
Name: "slovenian"; MessagesFile: "compiler:Languages\Slovenian.isl"
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"
Name: "swedish"; MessagesFile: "compiler:Languages\Swedish.isl"
Name: "tamil"; MessagesFile: "compiler:Languages\Tamil.isl"
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"
Name: "ukrainian"; MessagesFile: "compiler:Languages\Ukrainian.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "autostart"; Description: "{cm:AutoStartProgram,{#RetroBarName}}"; GroupDescription: "{cm:AutoStartProgramGroupDescription}"; Flags: unchecked

[Files]
Source: "RetroBar\bin\Release\{#TargetFramework}\publish-ARM64\*"; DestDir: "{app}"; Check: PreferArm64Files; Flags: ignoreversion recursesubdirs
Source: "RetroBar\bin\Release\{#TargetFramework}\publish-x64\*"; DestDir: "{app}"; Check: PreferX64Files; Flags: solidbreak ignoreversion recursesubdirs
Source: "RetroBar\bin\Release\{#TargetFramework}\publish-x86\*"; DestDir: "{app}"; Check: PreferX86Files; Flags: solidbreak ignoreversion recursesubdirs

[UninstallDelete]
Type: filesandordirs; Name: "{localappdata}\RetroBar\Logs"
Type: files; Name: "{localappdata}\RetroBar\settings.json"
Type: dirifempty; Name: "{localappdata}\RetroBar"

[Icons]
Name: "{autoprograms}\{#RetroBarName}"; Filename: "{app}\{#RetroBarExeName}"
Name: "{autodesktop}\{#RetroBarName}"; Filename: "{app}\{#RetroBarExeName}"; Tasks: desktopicon

[Registry]
Root: HKA; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueName: "RetroBar"; ValueType: string; ValueData: "{app}\{#RetroBarExeName}"; Tasks: autostart; Flags: uninsdeletevalue

[Run]
Filename: "{tmp}\{#DotNetInstallerExe}"; StatusMsg:"Installing Microsoft .NET 6 Desktop Runtime..."; Parameters:"/install /norestart"; Description: "{cm:LaunchProgram,{#RetroBarName}}"; Check: DotNetRuntimeIsMissing; Flags: skipifdoesntexist
Filename: "{app}\{#RetroBarExeName}"; Description: "{cm:LaunchProgram,{#RetroBarName}}"; Flags: nowait postinstall skipifsilent

[Code]
var
  DownloadPage: TDownloadWizardPage;
  DotNetChecked: boolean;
  DotNetMissing: boolean;
 
function PreferArm64Files: Boolean;
begin
  Result := IsArm64;
end;

function PreferX64Files: Boolean;
begin
  Result := not PreferArm64Files and IsX64Compatible;
end;

function PreferX86Files: Boolean;
begin
  Result := not PreferArm64Files and not PreferX64Files;
end;

// Version check courtesy https://engy.us/blog/2021/02/28/installing-net-5-runtime-automatically-with-inno-setup/
function CompareVersion(V1, V2: string): Integer;
var
  P, N1, N2: Integer;
begin
  Result := 0;
  while (Result = 0) and ((V1 <> '') or (V2 <> '')) do
  begin
    P := Pos('.', V1);
    if P > 0 then
    begin
      N1 := StrToInt(Copy(V1, 1, P - 1));
      Delete(V1, 1, P);
    end
      else
    if V1 <> '' then
    begin
      N1 := StrToInt(V1);
      V1 := '';
    end
      else
    begin
      N1 := 0;
    end;
    P := Pos('.', V2);
    if P > 0 then
    begin
      N2 := StrToInt(Copy(V2, 1, P - 1));
      Delete(V2, 1, P);
    end
      else
    if V2 <> '' then
    begin
      N2 := StrToInt(V2);
      V2 := '';
    end
      else
    begin
      N2 := 0;
    end;
    if N1 < N2 then Result := -1
      else
    if N1 > N2 then Result := 1;
  end;
end;

function DotNetRuntimeIsMissing(): Boolean;
var
  runtimes: TArrayOfString;
  registryKey: string;
  I: Integer;
  meetsMinimumVersion: Boolean;
  meetsMaximumVersion: Boolean;
begin
  Result := True;
  
  if DotNetChecked then
  begin
    Result := DotNetMissing;
    Exit;
  end;

  DotNetChecked := True;
  DotNetMissing := True;
  if PreferArm64Files() then
    registryKey := 'SOFTWARE\WOW6432Node\dotnet\Setup\InstalledVersions\arm64\sharedfx\Microsoft.WindowsDesktop.App'
  else if PreferX64Files() then
    registryKey := 'SOFTWARE\WOW6432Node\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App'
  else if PreferX86Files() then
    registryKey := 'SOFTWARE\dotnet\Setup\InstalledVersions\x86\sharedfx\Microsoft.WindowsDesktop.App';
  if RegGetValueNames(HKLM, registryKey, runtimes) then
  begin
    for I := 0 to GetArrayLength(runtimes)-1 do
    begin
      meetsMinimumVersion := not (CompareVersion(runtimes[I], '{#DotNetVersionMinimum}') = -1);
      meetsMaximumVersion := CompareVersion(runtimes[I], '{#DotNetVersionMaximum}') = -1;
      if meetsMinimumVersion and meetsMaximumVersion then
      begin
        Log(Format('[.NET] Selecting %s', [runtimes[I]]));
        DotNetMissing := False;
        Result := False;
          Exit;
      end;
    end;
  end;
end;

// To be used if we switch to a version that actually gets updates
// function DownloadDotNetVersion(baseVersion: String): Boolean;
// begin
  // DownloadPage.Clear;
  // DownloadPage.Add(Format('https://dotnetcli.azureedge.net/dotnet/WindowsDesktop/%s/latest.version', [baseVersion]), '{#DotNetVersionFile}', '');
  // DownloadPage.Show;
  // try
    // try
      // DownloadPage.Download; // This downloads the files to {tmp}
      // Result := True;
    // except
      // if DownloadPage.AbortedByUser then
        // Log('Aborted by user.')
      // else
        // SuppressibleMsgBox(AddPeriod(GetExceptionMessage), mbCriticalError, MB_OK, IDOK);
      // Result := False;
    // end;
  // finally
    // DownloadPage.Hide;
  // end;
// end;

function DownloadDotNetRuntime(version: AnsiString): Boolean;
var
  baseUrl: String;
begin
  baseUrl := Format('https://dotnetcli.azureedge.net/dotnet/WindowsDesktop/%s/windowsdesktop-runtime-%s', [version, version]);
  DownloadPage.Clear;
  if PreferArm64Files() then
    DownloadPage.Add(Format('%s-win-arm64.exe', [baseUrl]), '{#DotNetInstallerExe}', '')
  else if PreferX64Files() then
    DownloadPage.Add(Format('%s-win-x64.exe', [baseUrl]), '{#DotNetInstallerExe}', '')
  else if PreferX86Files() then
    DownloadPage.Add(Format('%s-win-x86.exe', [baseUrl]), '{#DotNetInstallerExe}', '');
  DownloadPage.Show;
  try
    try
      DownloadPage.Download; // This downloads the files to {tmp}
      Result := True;
    except
      if DownloadPage.AbortedByUser then
        Log('Aborted by user.')
      else
        SuppressibleMsgBox(AddPeriod(GetExceptionMessage), mbCriticalError, MB_OK, IDOK);
      Result := False;
    end;
  finally
    DownloadPage.Hide;
  end;
end;

function OnDownloadProgress(const Url, FileName: String; const Progress, ProgressMax: Int64): Boolean;
begin
  if Progress = ProgressMax then
    Log(Format('Successfully downloaded file to {tmp}: %s', [FileName]));
  Result := True;
end;

procedure InitializeWizard;
begin
  DownloadPage := CreateDownloadPage(SetupMessage(msgWizardPreparing), SetupMessage(msgPreparingDesc), @OnDownloadProgress);
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;
  if CurPageID = wpReady then begin
    if not DotNetRuntimeIsMissing() then Exit;
    
    // To be used if we switch to a version that actually gets updates
    // if not DownloadDotNetVersion('6.0') then Exit;
    // if not LoadStringFromFile(ExpandConstant('{tmp}\{#DotNetVersionFile}'), downloadVersion) then Exit;
    
    // Allow the install to proceed even if the download fails
    // The user will be prompted again when they launch RetroBar
    DownloadDotNetRuntime('{#DotNetVersionDownload}');
  end;
end;