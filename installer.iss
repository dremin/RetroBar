#define RetroBarName "RetroBar"
#define RetroBarPublisher "Sam Johnson"
#define RetroBarURL "https://github.com/dremin/RetroBar"
#define RetroBarReleasesURL RetroBarURL + "/releases"
#define RetroBarExeName "RetroBar.exe"

#define DotNetVersionDownload "6.0.36"
#define DotNetVersionMinimum "6.0.2"
#define DotNetVersionMaximum "6.1.0"
#define DotNetInstallerExe "RetroBar_DotNetRuntimeInstaller.exe"
#define DotNetInstallerTitle "Microsoft .NET 6 Desktop Runtime"

#define TargetFramework "net6.0-windows"
#define ReleasePath "RetroBar\bin\Release\" + TargetFramework
#define LanguagePath "RetroBar\Languages"
#define ThemePath "RetroBar\Themes"
#define ResourcesPath "RetroBar\Resources"
#define Excludes "Languages,Themes,Resources,System.Diagnostics.EventLog.Messages.dll"

#define Major
#define Minor
#define Revision
#define RetroBarVersion GetVersionComponents(ReleasePath + "\publish-x64\" + RetroBarExeName, Major, Minor, Revision, null), Str(Major) + "." + Str(Minor) + "." + Str(Revision)
#define VersionURL "https://dremin.github.io/updates/retrobar.json"

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
OutputBaseFilename=RetroBarInstaller
OutputDir=bin
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
LicenseFile=DistLicense.txt
WizardImageFile=compiler:WizClassicImage.bmp
WizardSmallImageFile=compiler:WizClassicSmallImage.bmp

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

[LangOptions]
spanish.LanguageName=español

[Messages]
spanish.StopDownload=¿Desea detener la descarga?
spanish.ExitSetupTitle=Salir de la instalación
spanish.SelectLanguageTitle=Seleccione el idioma de la instalación
spanish.WizardSelectTasks=Seleccione las tareas adicionales
spanish.WizardSelectDir=Seleccione la carpeta de destino
spanish.BrowseDialogTitle=Instalar: elegir una carpeta
spanish.WizardReady=Listo para instalar
spanish.WizardPreparing=Preparando la instalación
spanish.ConfirmUninstall=¿Seguro que desea desinstalar {#RetroBarName}?
spanish.WizardUninstalling=Estado de la desinstalación
spanish.TranslatorNote=Updated Spanish translation courtesy of Amaro Martínez for {#RetroBarPublisher}.
spanish.WizardLicense=Acuerdo de licencia
spanish.WizardSelectComponents=Seleccione los componentes
spanish.FullInstallation=Instalación completa
spanish.CompactInstallation=Instalación compacta
spanish.CustomInstallation=Instalación personalizada
spanish.NoUninstallWarningTitle=Componentes encontrados
spanish.ReadyMemoTasks=Tareas adicionales:
spanish.DirExistsTitle=La carpeta ya existe
spanish.DirExists=La carpeta "%1" ya existe. ¿Desea realizar la instalación en esa carpeta de todos modos?

[CustomMessages]
DependenciesMessage=Setup will also download and install required dependencies:
UpdateAvailableMessage=A new version of RetroBar is available!%n%nCurrent version: %s%nNew version: %s%n%nWould you like to visit the download page to get the latest version?
ConfirmDeleteSettingsMessage=Do you want to delete the RetroBar user settings?
InstallingDotNetRuntime=Installing {#DotNetInstallerTitle}...
LanguagesComponentName=Languages
ThemesComponentName=Themes

spanish.DependenciesMessage=La instalación también descargará e instalará las dependencias necesarias:
spanish.UpdateAvailableMessage=¡Una nueva versión de RetroBar está disponible!%n%nVersión actual: %s%nNueva versión: %s%n%n¿Desea visitar la página de descarga para obtener la última versión?
spanish.ConfirmDeleteSettingsMessage=¿Desea eliminar su configuración de usuario de RetroBar?
spanish.InstallingDotNetRuntime=Instalando {#DotNetInstallerTitle}...
spanish.LanguagesComponentName=Idiomas
spanish.ThemesComponentName=Temas

german.DependenciesMessage=Das Setup wird auch die erforderlichen Zusätze (Abhängigkeiten) herunterladen und installieren:
german.LanguagesComponentName=Sprachen
german.ThemesComponentName=Themen

[Components]
; [auto-generated components section]
; Components - Languages
Name: "languages"; Description: "{cm:LanguagesComponentName}"; Types: full compact custom
Name: "languages\en"; Description: "English"; Types: full compact custom; Flags: fixed
Name: "languages\ca"; Description: "català"; Types: full custom
Name: "languages\cs"; Description: "čeština"; Types: full custom
Name: "languages\de"; Description: "Deutsch"; Types: full custom
Name: "languages\en_gb"; Description: "English (United Kingdom)"; Types: full custom
Name: "languages\es"; Description: "español"; Types: full custom
Name: "languages\eu"; Description: "euskara"; Types: full custom
Name: "languages\fr"; Description: "français"; Types: full custom
Name: "languages\hr"; Description: "hrvatski"; Types: full custom
Name: "languages\id"; Description: "Indonesia"; Types: full custom
Name: "languages\it"; Description: "italiano"; Types: full custom
Name: "languages\lv"; Description: "latviešu"; Types: full custom
Name: "languages\lb"; Description: "Lëtzebuergesch"; Types: full custom
Name: "languages\lt"; Description: "lietuvių"; Types: full custom
Name: "languages\hu"; Description: "magyar"; Types: full custom
Name: "languages\ms"; Description: "Melayu"; Types: full custom
Name: "languages\nl"; Description: "Nederlands"; Types: full custom
Name: "languages\pl"; Description: "polski"; Types: full custom
Name: "languages\pt"; Description: "português"; Types: full custom
Name: "languages\ro"; Description: "română"; Types: full custom
Name: "languages\sk"; Description: "slovenčina"; Types: full custom
Name: "languages\sr"; Description: "srpski"; Types: full custom
Name: "languages\fi"; Description: "Suomi"; Types: full custom
Name: "languages\sv"; Description: "svenska"; Types: full custom
Name: "languages\vi"; Description: "Tiếng Việt"; Types: full custom
Name: "languages\tr"; Description: "Türkçe"; Types: full custom
Name: "languages\el"; Description: "ελληνικά"; Types: full custom
Name: "languages\bg"; Description: "български"; Types: full custom
Name: "languages\ru"; Description: "русский"; Types: full custom
Name: "languages\sr_cyrl"; Description: "српски"; Types: full custom
Name: "languages\uk"; Description: "українська"; Types: full custom
Name: "languages\he"; Description: "עברית"; Types: full custom
Name: "languages\ar"; Description: "العربية"; Types: full custom
Name: "languages\fa"; Description: "فارسی"; Types: full custom
Name: "languages\th"; Description: "ไทย"; Types: full custom
Name: "languages\ko"; Description: "한국어"; Types: full custom
Name: "languages\zh_hans"; Description: "中文(简体)"; Types: full custom
Name: "languages\zh_hant"; Description: "中文(繁體)"; Types: full custom
Name: "languages\ja"; Description: "日本語"; Types: full custom
; Components - Themes
Name: "themes"; Description: "{cm:ThemesComponentName}"; Types: full compact custom
Name: "themes\system"; Description: "System"; Types: full compact custom; Flags: checkablealone fixed
Name: "themes\system\systemvista"; Description: "System Vista"; Types: full custom
Name: "themes\system\systemxp"; Description: "System XP"; Types: full custom
Name: "themes\classic"; Description: "Classic Windows Themes"; Types: full compact custom
Name: "themes\classic\windows2000"; Description: "Windows 2000"; Types: full custom
Name: "themes\classic\windows9598"; Description: "Windows 95-98"; Types: full compact custom
Name: "themes\classic\windowsme"; Description: "Windows Me"; Types: full custom
Name: "themes\classic\windowsvistaclassic"; Description: "Windows Vista Classic"; Types: full custom
Name: "themes\classic\windowsxpclassic"; Description: "Windows XP Classic"; Types: full custom
Name: "themes\xp"; Description: "Windows XP Themes"; Types: full compact custom
Name: "themes\xp\windowsxpblue"; Description: "Windows XP Blue"; Types: full custom
Name: "themes\xp\windowsxpembeddedstyle"; Description: "Windows XP Embedded Style"; Types: full custom
Name: "themes\xp\windowsxpolivegreen"; Description: "Windows XP Olive Green"; Types: full custom
Name: "themes\xp\windowsxproyalenoir"; Description: "Windows XP Royale Noir"; Types: full custom
Name: "themes\xp\windowsxproyale"; Description: "Windows XP Royale"; Types: full custom
Name: "themes\xp\windowsxpsilver"; Description: "Windows XP Silver"; Types: full custom
Name: "themes\xp\windowsxpzunestyle"; Description: "Windows XP Zune Style"; Types: full custom
Name: "themes\vista"; Description: "Windows Vista Themes"; Types: full compact custom
Name: "themes\vista\windowsvistaaero"; Description: "Windows Vista Aero"; Types: full custom
Name: "themes\vista\windowsvistabasic"; Description: "Windows Vista Basic"; Types: full custom
Name: "themes\other"; Description: "Other Themes"; Types: full compact custom
Name: "themes\other\watercolor"; Description: "Watercolor"; Types: full custom
Name: "themes\other\windowslonghornaero"; Description: "Windows Longhorn Aero"; Types: full custom

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "autostart"; Description: "{cm:AutoStartProgram,{#RetroBarName}}"; GroupDescription: "{cm:AutoStartProgramGroupDescription}"; Flags: unchecked

[Files]
Source: "{#ReleasePath}\publish-ARM64\*"; DestDir: "{app}"; Check: PreferArm64Files; Flags: ignoreversion recursesubdirs; Excludes: "{#Excludes}"
Source: "{#ReleasePath}\publish-x64\*"; DestDir: "{app}"; Check: PreferX64Files; Flags: solidbreak ignoreversion recursesubdirs; Excludes: "{#Excludes}"
Source: "{#ReleasePath}\publish-x86\*"; DestDir: "{app}"; Check: PreferX86Files; Flags: solidbreak ignoreversion recursesubdirs; Excludes: "{#Excludes}"

Source: "{#ReleasePath}\publish-x64\System.Diagnostics.EventLog.Messages.dll"; DestDir: "{app}"; Flags: ignoreversion
; [auto-generated files section]
; Files - Languages
Source: "{#LanguagePath}\català.xaml"; DestDir: "{app}\Languages"; Components: "languages\ca"; Flags: ignoreversion
Source: "{#LanguagePath}\čeština.xaml"; DestDir: "{app}\Languages"; Components: "languages\cs"; Flags: ignoreversion
Source: "{#LanguagePath}\Deutsch.xaml"; DestDir: "{app}\Languages"; Components: "languages\de"; Flags: ignoreversion
Source: "{#LanguagePath}\English (United Kingdom).xaml"; DestDir: "{app}\Languages"; Components: "languages\en_gb"; Flags: ignoreversion
Source: "{#LanguagePath}\español.xaml"; DestDir: "{app}\Languages"; Components: "languages\es"; Flags: ignoreversion
Source: "{#LanguagePath}\euskara.xaml"; DestDir: "{app}\Languages"; Components: "languages\eu"; Flags: ignoreversion
Source: "{#LanguagePath}\français.xaml"; DestDir: "{app}\Languages"; Components: "languages\fr"; Flags: ignoreversion
Source: "{#LanguagePath}\hrvatski.xaml"; DestDir: "{app}\Languages"; Components: "languages\hr"; Flags: ignoreversion
Source: "{#LanguagePath}\Indonesia.xaml"; DestDir: "{app}\Languages"; Components: "languages\id"; Flags: ignoreversion
Source: "{#LanguagePath}\italiano.xaml"; DestDir: "{app}\Languages"; Components: "languages\it"; Flags: ignoreversion
Source: "{#LanguagePath}\latviešu.xaml"; DestDir: "{app}\Languages"; Components: "languages\lv"; Flags: ignoreversion
Source: "{#LanguagePath}\Lëtzebuergesch.xaml"; DestDir: "{app}\Languages"; Components: "languages\lb"; Flags: ignoreversion
Source: "{#LanguagePath}\lietuvių.xaml"; DestDir: "{app}\Languages"; Components: "languages\lt"; Flags: ignoreversion
Source: "{#LanguagePath}\magyar.xaml"; DestDir: "{app}\Languages"; Components: "languages\hu"; Flags: ignoreversion
Source: "{#LanguagePath}\Melayu.xaml"; DestDir: "{app}\Languages"; Components: "languages\ms"; Flags: ignoreversion
Source: "{#LanguagePath}\Nederlands.xaml"; DestDir: "{app}\Languages"; Components: "languages\nl"; Flags: ignoreversion
Source: "{#LanguagePath}\polski.xaml"; DestDir: "{app}\Languages"; Components: "languages\pl"; Flags: ignoreversion
Source: "{#LanguagePath}\português.xaml"; DestDir: "{app}\Languages"; Components: "languages\pt"; Flags: ignoreversion
Source: "{#LanguagePath}\română.xaml"; DestDir: "{app}\Languages"; Components: "languages\ro"; Flags: ignoreversion
Source: "{#LanguagePath}\slovenčina.xaml"; DestDir: "{app}\Languages"; Components: "languages\sk"; Flags: ignoreversion
Source: "{#LanguagePath}\srpski.xaml"; DestDir: "{app}\Languages"; Components: "languages\sr"; Flags: ignoreversion
Source: "{#LanguagePath}\Suomi.xaml"; DestDir: "{app}\Languages"; Components: "languages\fi"; Flags: ignoreversion
Source: "{#LanguagePath}\svenska.xaml"; DestDir: "{app}\Languages"; Components: "languages\sv"; Flags: ignoreversion
Source: "{#LanguagePath}\Tiếng Việt.xaml"; DestDir: "{app}\Languages"; Components: "languages\vi"; Flags: ignoreversion
Source: "{#LanguagePath}\Türkçe.xaml"; DestDir: "{app}\Languages"; Components: "languages\tr"; Flags: ignoreversion
Source: "{#LanguagePath}\ελληνικά.xaml"; DestDir: "{app}\Languages"; Components: "languages\el"; Flags: ignoreversion
Source: "{#LanguagePath}\български.xaml"; DestDir: "{app}\Languages"; Components: "languages\bg"; Flags: ignoreversion
Source: "{#LanguagePath}\русский.xaml"; DestDir: "{app}\Languages"; Components: "languages\ru"; Flags: ignoreversion
Source: "{#LanguagePath}\српски.xaml"; DestDir: "{app}\Languages"; Components: "languages\sr_cyrl"; Flags: ignoreversion
Source: "{#LanguagePath}\українська.xaml"; DestDir: "{app}\Languages"; Components: "languages\uk"; Flags: ignoreversion
Source: "{#LanguagePath}\עברית.xaml"; DestDir: "{app}\Languages"; Components: "languages\he"; Flags: ignoreversion
Source: "{#LanguagePath}\العربية.xaml"; DestDir: "{app}\Languages"; Components: "languages\ar"; Flags: ignoreversion
Source: "{#LanguagePath}\فارسی.xaml"; DestDir: "{app}\Languages"; Components: "languages\fa"; Flags: ignoreversion
Source: "{#LanguagePath}\ไทย.xaml"; DestDir: "{app}\Languages"; Components: "languages\th"; Flags: ignoreversion
Source: "{#LanguagePath}\한국어.xaml"; DestDir: "{app}\Languages"; Components: "languages\ko"; Flags: ignoreversion
Source: "{#LanguagePath}\中文(简体).xaml"; DestDir: "{app}\Languages"; Components: "languages\zh_hans"; Flags: ignoreversion
Source: "{#LanguagePath}\中文(繁體).xaml"; DestDir: "{app}\Languages"; Components: "languages\zh_hant"; Flags: ignoreversion
Source: "{#LanguagePath}\日本語.xaml"; DestDir: "{app}\Languages"; Components: "languages\ja"; Flags: ignoreversion
; Files - Themes
Source: "{#ThemePath}\System Vista.xaml"; DestDir: "{app}\Themes"; Components: "themes\system\systemvista"; Flags: ignoreversion
Source: "{#ResourcesPath}\startvistaclassic.png"; DestDir: "{app}\Resources"; Components: "themes\system\systemvista"; Flags: ignoreversion
Source: "{#ThemePath}\System XP.xaml"; DestDir: "{app}\Themes"; Components: "themes\system\systemxp"; Flags: ignoreversion
Source: "{#ResourcesPath}\startxpclassic.png"; DestDir: "{app}\Resources"; Components: "themes\system\systemxp"; Flags: ignoreversion
Source: "{#ResourcesPath}\desktopxp-sm.png"; DestDir: "{app}\Resources"; Components: "themes\system\systemxp"; Flags: ignoreversion
Source: "{#ResourcesPath}\desktopxp-lg.png"; DestDir: "{app}\Resources"; Components: "themes\system\systemxp"; Flags: ignoreversion
Source: "{#ThemePath}\Windows 2000.xaml"; DestDir: "{app}\Themes"; Components: "themes\classic\windows2000"; Flags: ignoreversion
Source: "{#ResourcesPath}\desktopMe2k-sm.png"; DestDir: "{app}\Resources"; Components: "themes\classic\windows2000"; Flags: ignoreversion
Source: "{#ResourcesPath}\desktopMe2k-lg.png"; DestDir: "{app}\Resources"; Components: "themes\classic\windows2000"; Flags: ignoreversion
Source: "{#ThemePath}\Windows 95-98.xaml"; DestDir: "{app}\Themes"; Components: "themes\classic\windows9598"; Flags: ignoreversion
Source: "{#ThemePath}\Windows Me.xaml"; DestDir: "{app}\Themes"; Components: "themes\classic\windowsme"; Flags: ignoreversion
Source: "{#ThemePath}\Windows Vista Classic.xaml"; DestDir: "{app}\Themes"; Components: "themes\classic\windowsvistaclassic"; Flags: ignoreversion
Source: "{#ThemePath}\Windows XP Classic.xaml"; DestDir: "{app}\Themes"; Components: "themes\classic\windowsxpclassic"; Flags: ignoreversion
Source: "{#ThemePath}\Windows XP Blue.xaml"; DestDir: "{app}\Themes"; Components: "themes\xp\windowsxpblue"; Flags: ignoreversion
Source: "{#ResourcesPath}\startxpblue.png"; DestDir: "{app}\Resources"; Components: "themes\xp\windowsxpblue"; Flags: ignoreversion
Source: "{#ResourcesPath}\startxpbluebutton.png"; DestDir: "{app}\Resources"; Components: "themes\xp\windowsxpblue"; Flags: ignoreversion
Source: "{#ResourcesPath}\desktopxp-sm.png"; DestDir: "{app}\Resources"; Components: "themes\xp\windowsxpblue"; Flags: ignoreversion
Source: "{#ResourcesPath}\desktopxp-lg.png"; DestDir: "{app}\Resources"; Components: "themes\xp\windowsxpblue"; Flags: ignoreversion
Source: "{#ThemePath}\Windows XP Embedded Style.xaml"; DestDir: "{app}\Themes"; Components: "themes\xp\windowsxpembeddedstyle"; Flags: ignoreversion
Source: "{#ResourcesPath}\startxpembeddedbutton.png"; DestDir: "{app}\Resources"; Components: "themes\xp\windowsxpembeddedstyle"; Flags: ignoreversion
Source: "{#ThemePath}\Windows XP Olive Green.xaml"; DestDir: "{app}\Themes"; Components: "themes\xp\windowsxpolivegreen"; Flags: ignoreversion
Source: "{#ResourcesPath}\startxpgreenbutton.png"; DestDir: "{app}\Resources"; Components: "themes\xp\windowsxpolivegreen"; Flags: ignoreversion
Source: "{#ThemePath}\Windows XP Royale Noir.xaml"; DestDir: "{app}\Themes"; Components: "themes\xp\windowsxproyalenoir"; Flags: ignoreversion
Source: "{#ResourcesPath}\startnoir.png"; DestDir: "{app}\Resources"; Components: "themes\xp\windowsxproyalenoir"; Flags: ignoreversion
Source: "{#ThemePath}\Windows XP Royale.xaml"; DestDir: "{app}\Themes"; Components: "themes\xp\windowsxproyale"; Flags: ignoreversion
Source: "{#ResourcesPath}\startxproyalebutton.png"; DestDir: "{app}\Resources"; Components: "themes\xp\windowsxproyale"; Flags: ignoreversion
Source: "{#ThemePath}\Windows XP Silver.xaml"; DestDir: "{app}\Themes"; Components: "themes\xp\windowsxpsilver"; Flags: ignoreversion
Source: "{#ResourcesPath}\startxpsilverbutton.png"; DestDir: "{app}\Resources"; Components: "themes\xp\windowsxpsilver"; Flags: ignoreversion
Source: "{#ThemePath}\Windows XP Zune Style.xaml"; DestDir: "{app}\Themes"; Components: "themes\xp\windowsxpzunestyle"; Flags: ignoreversion
Source: "{#ResourcesPath}\startxpzunebutton.png"; DestDir: "{app}\Resources"; Components: "themes\xp\windowsxpzunestyle"; Flags: ignoreversion
Source: "{#ThemePath}\Windows Vista Aero.xaml"; DestDir: "{app}\Themes"; Components: "themes\vista\windowsvistaaero"; Flags: ignoreversion
Source: "{#ResourcesPath}\startvistabuttonbot.png"; DestDir: "{app}\Resources"; Components: "themes\vista\windowsvistaaero"; Flags: ignoreversion
Source: "{#ResourcesPath}\startvistabuttonbotscaled.png"; DestDir: "{app}\Resources"; Components: "themes\vista\windowsvistaaero"; Flags: ignoreversion
Source: "{#ResourcesPath}\startvistabuttontop.png"; DestDir: "{app}\Resources"; Components: "themes\vista\windowsvistaaero"; Flags: ignoreversion
Source: "{#ResourcesPath}\startvistabuttontopscaled.png"; DestDir: "{app}\Resources"; Components: "themes\vista\windowsvistaaero"; Flags: ignoreversion
Source: "{#ResourcesPath}\startvistabuttonlg.png"; DestDir: "{app}\Resources"; Components: "themes\vista\windowsvistaaero"; Flags: ignoreversion
Source: "{#ResourcesPath}\startvistabuttonlgscaled.png"; DestDir: "{app}\Resources"; Components: "themes\vista\windowsvistaaero"; Flags: ignoreversion
Source: "{#ThemePath}\Windows Vista Basic.xaml"; DestDir: "{app}\Themes"; Components: "themes\vista\windowsvistabasic"; Flags: ignoreversion
Source: "{#ThemePath}\Watercolor.xaml"; DestDir: "{app}\Themes"; Components: "themes\other\watercolor"; Flags: ignoreversion
Source: "{#ResourcesPath}\desktopxp-sm.png"; DestDir: "{app}\Resources"; Components: "themes\other\watercolor"; Flags: ignoreversion
Source: "{#ResourcesPath}\desktopxp-lg.png"; DestDir: "{app}\Resources"; Components: "themes\other\watercolor"; Flags: ignoreversion
Source: "{#ThemePath}\Windows Longhorn Aero.xaml"; DestDir: "{app}\Themes"; Components: "themes\other\windowslonghornaero"; Flags: ignoreversion
Source: "{#ResourcesPath}\startlhaerobutton.png"; DestDir: "{app}\Resources"; Components: "themes\other\windowslonghornaero"; Flags: ignoreversion
Source: "{#ResourcesPath}\desktopxp-sm.png"; DestDir: "{app}\Resources"; Components: "themes\other\windowslonghornaero"; Flags: ignoreversion
Source: "{#ResourcesPath}\desktopxp-lg.png"; DestDir: "{app}\Resources"; Components: "themes\other\windowslonghornaero"; Flags: ignoreversion

[UninstallDelete]
Type: filesandordirs; Name: "{localappdata}\RetroBar\Logs"
Type: dirifempty; Name: "{localappdata}\RetroBar"

[Icons]
Name: "{autoprograms}\{#RetroBarName}"; Filename: "{app}\{#RetroBarExeName}"
Name: "{autodesktop}\{#RetroBarName}"; Filename: "{app}\{#RetroBarExeName}"; Tasks: desktopicon

[Registry]
Root: HKA; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueName: "RetroBar"; ValueType: string; ValueData: "{app}\{#RetroBarExeName}"; Tasks: autostart; Flags: uninsdeletevalue

[Run]
Filename: "{tmp}\{#DotNetInstallerExe}"; StatusMsg: "{cm:InstallingDotNetRuntime}"; Parameters:"/install /norestart"; Description: "{cm:LaunchProgram,{#RetroBarName}}"; Check: DotNetRuntimeIsMissing; Flags: skipifdoesntexist
Filename: "{app}\{#RetroBarExeName}"; Description: "{cm:LaunchProgram,{#RetroBarName}}"; Flags: nowait postinstall skipifsilent

[Code]
var
  DownloadPage: TDownloadWizardPage;
  DotNetChecked: boolean;
  DotNetMissing: boolean;

  TypesComboOnChangePrev: TNotifyEvent;
  ComponentsListClickCheckPrev: TNotifyEvent;
  IgnoreDependencyCheck: Boolean;

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

procedure CheckForUpdates;
var
  LatestVersion: string;
  CurrentVersion: string;
  JSONStr: AnsiString;
  PosStart, PosEnd: Integer;
  MsgResult: Integer;
begin
  CurrentVersion := '{#RetroBarVersion}';

  try
    DownloadTemporaryFile('{#VersionURL}', 'retrobar.json', '', nil);
  except
    Log('Failed to download update information: ' + GetExceptionMessage);
    Exit;
  end;

  if LoadStringFromFile(ExpandConstant('{tmp}\retrobar.json'), JSONStr) then
  begin
    PosStart := Pos('"version": "', JSONStr) + Length('"version": "');
    PosEnd := Pos('"', Copy(JSONStr, PosStart, Length(JSONStr))) + PosStart - 1;
    LatestVersion := Copy(JSONStr, PosStart, PosEnd - PosStart);

    case CompareVersion(CurrentVersion, LatestVersion) of
      -1: begin
        Log(Format('CheckForUpdates: Current version %s is older than latest version %s', [CurrentVersion, LatestVersion]));
        MsgResult := MsgBox(Format(CustomMessage('UpdateAvailableMessage'), [CurrentVersion, LatestVersion]), 
          mbInformation, MB_YESNO);
        if MsgResult = IDYES then
          ShellExec('open', '{#RetroBarReleasesURL}', '', '', SW_SHOW, ewNoWait, MsgResult);
          end;
       0: Log(Format('CheckForUpdates: Current version %s matches latest version %s', [CurrentVersion, LatestVersion]));
       1: Log(Format('CheckForUpdates: Current version %s is newer than latest version %s', [CurrentVersion, LatestVersion]));
    end;
  end
  else
  begin
    Log('Failed to read the version information file.');
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

procedure AddDependencies(Component: String; Dependencies: array of String);
var
  i: Integer;
begin
  if Pos(Component, WizardSelectedComponents(False)) > 0 then
    for i := Low(Dependencies) to High(Dependencies) do
      WizardSelectComponents(Dependencies[i]);
end;

procedure EnforceComponentDependencies;
begin
  if IgnoreDependencyCheck then
    Exit;
  IgnoreDependencyCheck := True;
  try
    // [auto-generated code section]
    AddDependencies('themes\system\systemvista', ['themes\system\systemxp']);
    AddDependencies('themes\classic\windowsme', ['themes\classic\windows2000']);
    AddDependencies('themes\classic\windowsvistaclassic', ['themes\classic\windows2000', 'themes\system\systemvista']);
    AddDependencies('themes\classic\windowsxpclassic', ['themes\classic\windows2000', 'themes\system\systemxp']);
    AddDependencies('themes\xp\windowsxpembeddedstyle', ['themes\xp\windowsxproyale']);
    AddDependencies('themes\xp\windowsxpolivegreen', ['themes\xp\windowsxpblue']);
    AddDependencies('themes\xp\windowsxproyalenoir', ['themes\xp\windowsxproyale']);
    AddDependencies('themes\xp\windowsxproyale', ['themes\xp\windowsxpblue']);
    AddDependencies('themes\xp\windowsxpsilver', ['themes\xp\windowsxpblue']);
    AddDependencies('themes\xp\windowsxpzunestyle', ['themes\xp\windowsxproyale']);
    AddDependencies('themes\vista\windowsvistabasic', ['themes\vista\windowsvistaaero']);

  finally
    IgnoreDependencyCheck := False;
  end;
end;

function OnDownloadProgress(const Url, FileName: String; const Progress, ProgressMax: Int64): Boolean;
begin
  if Progress = ProgressMax then
    Log(Format('Successfully downloaded file to {tmp}: %s', [FileName]));
  Result := True;
end;

procedure ComponentsListClickCheck(Sender: TObject);
begin
  ComponentsListClickCheckPrev(Sender);
  EnforceComponentDependencies;
end;

// run CheckForUpdates
function InitializeSetup: Boolean;
begin
  CheckForUpdates;
  Result := True;
end;

procedure TypesComboOnChange(Sender: TObject);
begin
  TypesComboOnChangePrev(Sender);
  EnforceComponentDependencies;
end;

procedure InitializeWizard;
begin
  DownloadPage := CreateDownloadPage(SetupMessage(msgWizardPreparing), SetupMessage(msgPreparingDesc), @OnDownloadProgress);
  IgnoreDependencyCheck := False;
  ComponentsListClickCheckPrev := WizardForm.ComponentsList.OnClickCheck;
  WizardForm.ComponentsList.OnClickCheck := @ComponentsListClickCheck;
  TypesComboOnChangePrev := WizardForm.TypesCombo.OnChange;
  WizardForm.TypesCombo.OnChange := @TypesComboOnChange;
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

function UpdateReadyMemo(Space, NewLine, MemoUserInfoInfo, MemoDirInfo, MemoTypeInfo, MemoComponentsInfo, MemoGroupInfo, MemoTasksInfo: String): String;
begin
  if DotNetRuntimeIsMissing() then Result := CustomMessage('DependenciesMessage') + NewLine + Space + '{#DotNetInstallerTitle}' + NewLine + NewLine;
  Result := Result + MemoTasksInfo;
end;

// ask if user wants to delete the settings.json file
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  settingsPath: string;
begin
  if CurUninstallStep <> usPostUninstall then
    Exit;

  settingsPath := ExpandConstant('{localappdata}\RetroBar\settings.json');
  if FileExists(settingsPath) and 
     (MsgBox(CustomMessage('ConfirmDeleteSettingsMessage'), mbConfirmation, MB_YESNO) = IDYES) then
  begin
    if not DeleteFile(settingsPath) then
      Log('Warning: Could not delete user settings file: ' + settingsPath);
  end;
end;
