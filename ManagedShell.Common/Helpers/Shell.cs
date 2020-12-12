using ManagedShell.Interop;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using static ManagedShell.Interop.NativeMethods;

namespace ManagedShell.Common.Helpers
{
    // We have too many things in here
    // Lets focus more on single responsibility

    public partial class Shell
    {
        private const int MAX_PATH = 260;
        public static object ComLock = new object();

        // DPI at user logon to the system
        private static double? _oldDpiScale;
        public static double OldDpiScale
        {
            get
            {
                if (_oldDpiScale == null)
                {
                    _oldDpiScale = GetDpiScale();
                }

                return (double)_oldDpiScale;
            }
            set
            {
                _oldDpiScale = value;
            }
        }

        // Current system DPI; set on MenuBar startup and on WM_DPICHANGED
        private static double? _dpiScale;
        public static double DpiScale
        {
            get
            {
                if (_dpiScale == null)
                {
                    _dpiScale = GetDpiScale();
                }

                return (double)_dpiScale;
            }
            set
            {
                _dpiScale = value;
            }
        }

        // SystemParameters class returns values based on logon DPI only; this calculates how we should scale that number if logon DPI != current DPI.
        public static double DpiScaleAdjustment
        {
            get { return DpiScale / OldDpiScale; }
        }

        // IImageList references
        private static Guid iidImageList = new Guid("46EB5926-582E-4017-9FDF-E8998DAA0950");
        private static IImageList iml0; // 32pt
        private static IImageList iml1; // 16pt
        private static IImageList iml2; // 48pt
        public static ComTaskScheduler IconScheduler = new ComTaskScheduler();

        private static void initIml(int size)
        {
            // Initialize the appropriate IImageList for the desired icon size if it hasn't been already

            if (size == 0 && iml0 == null)
            {
                SHGetImageList(0, ref iidImageList, out iml0);
            }
            else if (size == 1 && iml1 == null)
            {
                SHGetImageList(1, ref iidImageList, out iml1);
            }
            else if (size == 2 && iml2 == null)
            {
                SHGetImageList(2, ref iidImageList, out iml2);
            }
        }

        public static void DisposeIml()
        {
            // Dispose any IImageList objects we instantiated.
            // Called by the main shutdown method.

            lock (ComLock)
            {
                if (iml0 != null)
                {
                    Marshal.ReleaseComObject(iml0);
                    iml0 = null;
                }
                if (iml1 != null)
                {
                    Marshal.ReleaseComObject(iml1);
                    iml1 = null;
                }
                if (iml2 != null)
                {
                    Marshal.ReleaseComObject(iml2);
                    iml2 = null;
                }
            }
        }

        public static IntPtr GetIconByFilename(string fileName, int size)
        {
            return GetIcon(fileName, size);
        }

        private static IntPtr GetIcon(string filename, int size)
        {
            lock (ComLock)
            {
                try
                {
                    filename = translateIconExceptions(filename);

                    SHFILEINFO shinfo = new SHFILEINFO();
                    shinfo.szDisplayName = string.Empty;
                    shinfo.szTypeName = string.Empty;
                    IntPtr hIconInfo;

                    if (!filename.StartsWith("\\") && (File.GetAttributes(filename) & FileAttributes.Directory) == FileAttributes.Directory)
                    {
                        hIconInfo = SHGetFileInfo(filename, FILE_ATTRIBUTE_NORMAL | FILE_ATTRIBUTE_DIRECTORY, ref shinfo, (uint)Marshal.SizeOf(shinfo), (uint)(SHGFI.SysIconIndex));
                    }
                    else
                    {
                        hIconInfo = SHGetFileInfo(filename, FILE_ATTRIBUTE_NORMAL, ref shinfo, (uint)Marshal.SizeOf(shinfo), (uint)(SHGFI.UseFileAttributes | SHGFI.SysIconIndex));
                    }

                    var iconIndex = shinfo.iIcon;

                    // Initialize the IImageList object
                    initIml(size);

                    IntPtr hIcon = IntPtr.Zero;
                    int ILD_TRANSPARENT = 1;

                    switch (size)
                    {
                        case 0:
                            iml0.GetIcon(iconIndex, ILD_TRANSPARENT, ref hIcon);
                            break;
                        case 1:
                            iml1.GetIcon(iconIndex, ILD_TRANSPARENT, ref hIcon);
                            break;
                        case 2:
                            iml2.GetIcon(iconIndex, ILD_TRANSPARENT, ref hIcon);
                            break;
                    }

                    return hIcon;
                }
                catch
                {
                    return IntPtr.Zero;
                }
            }
        }

        private static string translateIconExceptions(string filename)
        {
            if (filename.EndsWith(".settingcontent-ms"))
            {
                return "C:\\Windows\\ImmersiveControlPanel\\SystemSettings.exe";
            }

            return filename;
        }

        public static string GetDisplayName(string filename)
        {
            SHFILEINFO shinfo = new SHFILEINFO();
            shinfo.szDisplayName = string.Empty;
            shinfo.szTypeName = string.Empty;
            SHGetFileInfo(filename, FILE_ATTRIBUTE_NORMAL, ref shinfo, (uint)Marshal.SizeOf(shinfo), (uint)(SHGFI.DisplayName));

            return shinfo.szDisplayName;
        }

        public static string UsersStartMenuPath
        {
            get
            {
                return GetSpecialFolderPath((int)CSIDL.CSIDL_STARTMENU);
            }
        }

        public static string AllUsersStartMenuPath
        {
            get
            {
                return GetSpecialFolderPath((int)CSIDL.CSIDL_COMMON_STARTMENU);
            }
        }

        public static string GetSpecialFolderPath(int FOLDER)
        {
            StringBuilder sbPath = new StringBuilder(MAX_PATH);
            SHGetFolderPath(IntPtr.Zero, FOLDER, IntPtr.Zero, 0, sbPath);
            return sbPath.ToString();
        }

        public static bool ExecuteProcess(string filename)
        {
            System.Diagnostics.Process proc = new System.Diagnostics.Process();
            proc.StartInfo.UseShellExecute = true;
            proc.StartInfo.FileName = filename;

            try
            {
                return proc.Start();
            }
            catch
            {
                // No 'Open' command associated with this filetype in the registry
                Shell.ShowOpenWithDialog(proc.StartInfo.FileName);
                return false;
            }
        }

        public static bool StartProcess(string filename)
        {
            try
            {
                if (!Environment.Is64BitProcess)
                {
                    filename.Replace("system32", "sysnative");
                }

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    UseShellExecute = true
                };

                if (filename.StartsWith("appx:"))
                {
                    psi.FileName = "LaunchWinApp.exe";
                    psi.Arguments = "shell:appsFolder\\" + filename.Substring(5);
                }
                else if (filename.Contains("://"))
                {
                    psi.FileName = "explorer.exe";
                    psi.Arguments = filename;
                }
                else
                {
                    if (IsCairoRunningAsShell && filename.ToLower().EndsWith("explorer.exe"))
                    {
                        // if we are shell and launching explorer, give it a parameter so that it doesn't do shell things.
                        // this opens My Computer
                        psi.FileName = filename;
                        psi.Arguments = "::{20D04FE0-3AEA-1069-A2D8-08002B30309D}";
                    }
                    else
                    {
                        psi.FileName = filename;
                    }
                }

                Process.Start(psi);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool StartProcess(string filename, string args)
        {
            try
            {
                if (!Environment.Is64BitProcess)
                {
                    filename.Replace("system32", "sysnative");
                }

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    UseShellExecute = true,
                    FileName = filename,
                    Arguments = args
                };

                Process.Start(psi);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool StartProcess(string filename, string args, string verb)
        {
            try
            {
                if (!Environment.Is64BitProcess)
                {
                    filename.Replace("system32", "sysnative");
                }

                Process proc = new Process();
                proc.StartInfo.UseShellExecute = true;
                proc.StartInfo.FileName = filename;
                proc.StartInfo.Verb = verb;
                try
                {
                    proc.Start();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(String.Format("Error running the {0} verb on {1}. ({2})", verb, filename, ex.Message));
                    return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool Exists(string filename)
        {
            foreach (char invalid in Path.GetInvalidPathChars())
            {
                if (filename.Contains(invalid.ToString()))
                {
                    return false;
                }
            }

            return !filename.StartsWith("\\\\") && (File.Exists(filename) || Directory.Exists(filename));
        }

        public static void ShowWindowBottomMost(IntPtr handle)
        {
            SetWindowPos(
                handle,
                (IntPtr)WindowZOrder.HWND_BOTTOM,
                0,
                0,
                0,
                0,
                (int)SetWindowPosFlags.SWP_NOSIZE | (int)SetWindowPosFlags.SWP_NOMOVE | (int)SetWindowPosFlags.SWP_NOACTIVATE/* | SWP_NOZORDER | SWP_NOOWNERZORDER*/);
        }

        public static void ShowWindowTopMost(IntPtr handle)
        {
            SetWindowPos(
                handle,
                (IntPtr)WindowZOrder.HWND_TOPMOST,
                0,
                0,
                0,
                0,
                (int)SetWindowPosFlags.SWP_NOSIZE | (int)SetWindowPosFlags.SWP_NOMOVE | (int)SetWindowPosFlags.SWP_SHOWWINDOW/* | (int)SetWindowPosFlags.SWP_NOACTIVATE | SWP_NOZORDER | SWP_NOOWNERZORDER*/);
        }

        public static void ShowWindowDesktop(IntPtr hwnd)
        {
            IntPtr desktopHwnd = GetLowestDesktopParentHwnd();

            if (desktopHwnd != IntPtr.Zero)
            {
                IntPtr nextHwnd = GetWindow(desktopHwnd, GetWindow_Cmd.GW_HWNDPREV);
                SetWindowPos(
                    hwnd,
                    nextHwnd,
                    0,
                    0,
                    0,
                    0,
                    (int)SetWindowPosFlags.SWP_NOSIZE | (int)SetWindowPosFlags.SWP_NOMOVE | (int)SetWindowPosFlags.SWP_NOACTIVATE);
            }
            else
            {
                ShowWindowBottomMost(hwnd);
            }
        }

        public static IntPtr GetLowestDesktopParentHwnd()
        {
            IntPtr progmanHwnd = FindWindow("Progman", "Program Manager");
            IntPtr desktopHwnd = FindWindowEx(progmanHwnd, IntPtr.Zero, "SHELLDLL_DefView", null);

            if (desktopHwnd == IntPtr.Zero)
            {
                IntPtr workerHwnd = IntPtr.Zero;
                IntPtr shellIconsHwnd;
                do
                {
                    workerHwnd = FindWindowEx(IntPtr.Zero, workerHwnd, "WorkerW", null);
                    shellIconsHwnd = FindWindowEx(workerHwnd, IntPtr.Zero, "SHELLDLL_DefView", null);
                } while (shellIconsHwnd == IntPtr.Zero && workerHwnd != IntPtr.Zero);

                desktopHwnd = workerHwnd;
            }
            else
            {
                desktopHwnd = progmanHwnd;
            }

            return desktopHwnd;
        }

        public static IntPtr GetLowestDesktopChildHwnd()
        {
            IntPtr progmanHwnd = FindWindow("Progman", "Program Manager");
            IntPtr desktopHwnd = FindWindowEx(progmanHwnd, IntPtr.Zero, "SHELLDLL_DefView", null);

            if (desktopHwnd == IntPtr.Zero)
            {
                IntPtr workerHwnd = IntPtr.Zero;
                IntPtr shellIconsHwnd;
                do
                {
                    workerHwnd = FindWindowEx(IntPtr.Zero, workerHwnd, "WorkerW", null);
                    shellIconsHwnd = FindWindowEx(workerHwnd, IntPtr.Zero, "SHELLDLL_DefView", null);
                } while (shellIconsHwnd == IntPtr.Zero && workerHwnd != IntPtr.Zero);

                desktopHwnd = shellIconsHwnd;
            }

            return desktopHwnd;
        }

        public static int GetMenuDropAlignment()
        {
            int menuDropAlignment = 0;

            try
            {
                RegistryKey windowsKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Windows", false);

                if (windowsKey != null)
                {
                    var menuDropAlignmentValue = windowsKey.GetValue("MenuDropAlignment");

                    if (menuDropAlignmentValue != null)
                    {
                        menuDropAlignment = Convert.ToInt32(menuDropAlignmentValue);
                    }
                }
            }
            catch { }

            return menuDropAlignment;
        }

        public static bool ShowFileProperties(string Filename)
        {
            SHELLEXECUTEINFO info = new SHELLEXECUTEINFO();
            info.cbSize = Marshal.SizeOf(info);
            info.lpVerb = "properties";
            info.lpFile = Filename;
            info.nShow = (int)WindowShowStyle.Show;
            info.fMask = SEE_MASK_INVOKEIDLIST;
            return ShellExecuteEx(ref info);
        }

        /// <summary>
        /// Calls the Windows OpenWith dialog (shell32.dll) to open the file specified.
        /// </summary>
        /// <param name="fileName">Path to the file to open</param>
        public static void ShowOpenWithDialog(string fileName)
        {
            Process owProc = new Process();
            owProc.StartInfo.UseShellExecute = true;
            owProc.StartInfo.FileName = Environment.GetEnvironmentVariable("WINDIR") + @"\system32\rundll32.exe";
            owProc.StartInfo.Arguments =
                @"C:\WINDOWS\system32\shell32.dll,OpenAs_RunDLL " + fileName;
            owProc.Start();
        }

        public static void StartTaskManager()
        {
            Shell.StartProcess("taskmgr.exe");
        }

        public static void ShowRunDialog(string title, string info)
        {
            SHRunFileDialog(IntPtr.Zero, IntPtr.Zero, null, title, info, RunFileDialogFlags.None);
        }

        public static void ShowWindowSwitcher()
        {
            shellKeyCombo(VK_LWIN, VK_TAB);
        }

        public static void ShowActionCenter()
        {
            shellKeyCombo(VK_LWIN, 0x41);
        }

        public static void ShowStartMenu()
        {
            shellKeyCombo(VK_LWIN, VK_LWIN);
        }

        private static void shellKeyCombo(ushort wVk_1, ushort wVk_2)
        {
            INPUT[] inputs = new INPUT[4];

            inputs[0].type = INPUT_KEYBOARD;
            inputs[0].mkhi.ki.time = 0;
            inputs[0].mkhi.ki.wScan = 0;
            inputs[0].mkhi.ki.dwExtraInfo = GetMessageExtraInfo();
            inputs[0].mkhi.ki.wVk = wVk_1;
            inputs[0].mkhi.ki.dwFlags = 0;

            inputs[1].type = INPUT_KEYBOARD;
            inputs[1].mkhi.ki.wScan = 0;
            inputs[1].mkhi.ki.dwExtraInfo = GetMessageExtraInfo();
            inputs[1].mkhi.ki.wVk = wVk_2;
            inputs[1].mkhi.ki.dwFlags = 0;

            inputs[2].type = INPUT_KEYBOARD;
            inputs[2].mkhi.ki.wScan = 0;
            inputs[2].mkhi.ki.dwExtraInfo = GetMessageExtraInfo();
            inputs[2].mkhi.ki.wVk = wVk_2;
            inputs[2].mkhi.ki.dwFlags = KEYEVENTF_KEYUP;

            inputs[3].type = INPUT_KEYBOARD;
            inputs[3].mkhi.ki.wScan = 0;
            inputs[3].mkhi.ki.dwExtraInfo = GetMessageExtraInfo();
            inputs[3].mkhi.ki.wVk = wVk_1;
            inputs[3].mkhi.ki.dwFlags = KEYEVENTF_KEYUP;

            SendInput(4, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        /// <summary>
        /// Send file to recycle bin
        /// </summary>
        /// <param name="path">Location of directory or file to recycle</param>
        /// <param name="flags">FileOperationFlags to add in addition to FOF_ALLOWUNDO</param>
        public static bool SendToRecycleBin(string path, FileOperationFlags flags)
        {
            try
            {
                var fs = new SHFILEOPSTRUCT
                {
                    wFunc = FileOperationType.FO_DELETE,
                    pFrom = path + '\0' + '\0',
                    fFlags = FileOperationFlags.FOF_ALLOWUNDO | flags
                };
                SHFileOperation(ref fs);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Send file to recycle bin.  Display dialog, display warning if files are too big to fit (FOF_WANTNUKEWARNING)
        /// </summary>
        /// <param name="path">Location of directory or file to recycle</param>
        public static bool SendToRecycleBin(string path)
        {
            return SendToRecycleBin(path, FileOperationFlags.FOF_NOCONFIRMATION | FileOperationFlags.FOF_WANTNUKEWARNING);
        }

        /// <summary>
        /// Send file silently to recycle bin.  Surpress dialog, surpress errors, delete if too large.
        /// </summary>
        /// <param name="path">Location of directory or file to recycle</param>
        public static bool MoveToRecycleBin(string path)
        {
            return SendToRecycleBin(path, FileOperationFlags.FOF_NOCONFIRMATION | FileOperationFlags.FOF_NOERRORUI | FileOperationFlags.FOF_SILENT);

        }

        public static void HideWindowFromTasks(IntPtr hWnd)
        {
            SetWindowLong(hWnd, GWL_EXSTYLE, GetWindowLong(hWnd, GWL_EXSTYLE) | (int)ExtendedWindowStyles.WS_EX_TOOLWINDOW);

            ExcludeWindowFromPeek(hWnd);
        }

        public static void ExcludeWindowFromPeek(IntPtr hWnd)
        {
            int status = (int)DWMNCRENDERINGPOLICY.DWMNCRP_ENABLED;
            DwmSetWindowAttribute(hWnd,
                DWMWINDOWATTRIBUTE.DWMWA_EXCLUDED_FROM_PEEK,
                ref status,
                sizeof(int));
        }

        public static void PeekWindow(bool show, IntPtr targetHwnd, IntPtr callingHwnd)
        {
            uint enable = 0;
            if (show) enable = 1;

            if (IsWindows81OrBetter)
            {
                DwmActivateLivePreview(enable, targetHwnd, callingHwnd, AeroPeekType.Window, IntPtr.Zero);
            }
            else
            {
                DwmActivateLivePreview(enable, targetHwnd, callingHwnd, AeroPeekType.Window);
            }
        }

        public static void SetWindowBlur(IntPtr hWnd, bool enable)
        {
            if (IsWindows10OrBetter)
            {
                // https://github.com/riverar/sample-win32-acrylicblur
                // License: MIT
                var accent = new AccentPolicy();
                var accentStructSize = Marshal.SizeOf(accent);
                if (enable)
                {
                    if (IsWindows10RS4OrBetter)
                    {
                        accent.AccentState = AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND;
                        accent.GradientColor = (0 << 24) | (0xFFFFFF /* BGR */ & 0xFFFFFF);
                    }
                    else
                    {
                        accent.AccentState = AccentState.ACCENT_ENABLE_BLURBEHIND;
                    }
                }
                else
                {
                    accent.AccentState = AccentState.ACCENT_DISABLED;
                }

                var accentPtr = Marshal.AllocHGlobal(accentStructSize);
                Marshal.StructureToPtr(accent, accentPtr, false);

                var data = new WindowCompositionAttributeData();
                data.Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY;
                data.SizeOfData = accentStructSize;
                data.Data = accentPtr;

                SetWindowCompositionAttribute(hWnd, ref data);

                Marshal.FreeHGlobal(accentPtr);
            }
        }

        public static void ToggleDesktopIcons(bool enable)
        {
            if (!IsCairoRunningAsShell)
            {
                var toggleDesktopCommand = new IntPtr(0x7402);
                IntPtr hWnd = FindWindowEx(FindWindow("Progman", "Program Manager"), IntPtr.Zero, "SHELLDLL_DefView",
                    "");

                if (hWnd == IntPtr.Zero)
                {
                    EnumWindows((hwnd, lParam) =>
                    {
                        StringBuilder cName = new StringBuilder(256);
                        GetClassName(hwnd, cName, cName.Capacity);
                        if (cName.ToString() == "WorkerW")
                        {
                            IntPtr child = FindWindowEx(hwnd, IntPtr.Zero, "SHELLDLL_DefView", null);
                            if (child != IntPtr.Zero)
                            {
                                hWnd = child;
                                return true;
                            }
                        }

                        return true;
                    }, 0);
                }

                if (IsDesktopVisible() != enable)
                {
                    SendMessageTimeout(hWnd, (uint) WM.COMMAND, toggleDesktopCommand, IntPtr.Zero, 2, 200, ref hWnd);
                }
            }
        }

        private static bool IsDesktopVisible()
        {
            IntPtr hWnd = GetWindow(FindWindowEx(FindWindow("Progman", "Program Manager"), IntPtr.Zero, "SHELLDLL_DefView", ""), GetWindow_Cmd.GW_CHILD);


            if (hWnd == IntPtr.Zero)
            {
                EnumWindows((hwnd, lParam) =>
                {
                    StringBuilder cName = new StringBuilder(256);
                    GetClassName(hwnd, cName, cName.Capacity);
                    if (cName.ToString() == "WorkerW")
                    {
                        IntPtr child = FindWindowEx(hwnd, IntPtr.Zero, "SHELLDLL_DefView", null);
                        if (child != IntPtr.Zero)
                        {
                            hWnd = FindWindowEx(child, IntPtr.Zero, "SysListView32", "FolderView");
                            return true;
                        }
                    }
                    return true;
                }, 0);
            }

            WINDOWINFO info = new WINDOWINFO();
            info.cbSize = (uint)Marshal.SizeOf(info);
            GetWindowInfo(hWnd, ref info);
            return (info.dwStyle & 0x10000000) == 0x10000000;
        }

        public static string GetPathForHandle(IntPtr hWnd)
        {
            StringBuilder outFileName = new StringBuilder(1024);

            // get process id
            uint procId;
            GetWindowThreadProcessId(hWnd, out procId);

            if (procId != 0)
            {
                // open process
                // QueryLimitedInformation flag allows us to access elevated applications as well
                IntPtr hProc = OpenProcess(ProcessAccessFlags.QueryLimitedInformation, false, (int) procId);

                // get filename
                int len = outFileName.Capacity;
                QueryFullProcessImageName(hProc, 0, outFileName, ref len);

                outFileName.Replace("Excluded,", "");
                outFileName.Replace(",SFC protected", "");
            }

            return outFileName.ToString();
        }

        /// <summary>
        /// Transforms device independent units (1/96 of an inch)
        /// to pixels
        /// </summary>
        /// <param name="unitX">a device independent unit value X</param>
        /// <param name="unitY">a device independent unit value Y</param>
        /// <param name="pixelX">returns the X value in pixels</param>
        /// <param name="pixelY">returns the Y value in pixels</param>
        public static void TransformToPixels(double unitX, double unitY, out int pixelX, out int pixelY)
        {
            pixelX = (int)(DpiScale * unitX);
            pixelY = (int)(DpiScale * unitY);
        }

        public static void TransformFromPixels(double unitX, double unitY, out int pixelX, out int pixelY)
        {
            pixelX = (int)(unitX / DpiScale);
            pixelY = (int)(unitY / DpiScale);
        }

        private static double GetDpiScale()
        {
            using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
            {
                return (g.DpiX / 96);
            }
        }

        private static int osVersionMajor = 0;
        private static int osVersionMinor = 0;
        private static int osVersionBuild = 0;

        private static void getOSVersion()
        {
            osVersionMajor = Environment.OSVersion.Version.Major;
            osVersionMinor = Environment.OSVersion.Version.Minor;
            osVersionBuild = Environment.OSVersion.Version.Build;
        }

        public static bool IsWindows2kOrBetter
        {
            get
            {
                if (osVersionMajor == 0)
                {
                    getOSVersion();
                }

                return (osVersionMajor >= 5);
            }
        }

        public static bool IsWindowsVistaOrBetter
        {
            get
            {
                if (osVersionMajor == 0)
                {
                    getOSVersion();
                }

                return (osVersionMajor >= 6);
            }
        }

        public static bool IsWindows8OrBetter
        {
            get
            {
                if (osVersionMajor == 0)
                {
                    getOSVersion();
                }

                return (osVersionMajor > 6 || (osVersionMajor == 6 && osVersionMinor >= 2));
            }
        }

        public static bool IsWindows81OrBetter
        {
            get
            {
                if (osVersionMajor == 0)
                {
                    getOSVersion();
                }

                return (osVersionMajor > 6 || (osVersionMajor == 6 && osVersionMinor >= 2 && osVersionBuild >= 9600));
            }
        }

        public static bool IsWindows10OrBetter
        {
            get
            {
                if (osVersionMajor == 0)
                {
                    getOSVersion();
                }

                return (osVersionMajor >= 10);
            }
        }

        public static bool IsWindows10RS4OrBetter
        {
            get
            {
                if (osVersionMajor == 0)
                {
                    getOSVersion();
                }

                return (osVersionMajor >= 10 && osVersionBuild >= 16353);
            }
        }

        private static bool? isCairoConfiguredAsShell;

        /// <summary>
        /// Checks the currently configured shell, NOT the currently running shell! Use Shell.IsCairoRunningAsShell for that.
        /// </summary>
        public static bool IsCairoConfiguredAsShell
        {
            get
            {
                if (isCairoConfiguredAsShell == null)
                {
                    // first check if we are the current user's shell
                    RegistryKey userShellKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\WinLogon", false);
                    string userShell = userShellKey?.GetValue("Shell") as string;
                    if (userShell != null)
                    {
                        isCairoConfiguredAsShell = userShell.ToString().ToLower().Contains("cairodesktop");
                    }
                    else
                    {
                        // check if we are the current system's shell
                        RegistryKey systemShellKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\WinLogon", false);
                        string systemShell = systemShellKey?.GetValue("Shell") as string;
                        if (systemShell != null)
                        {
                            isCairoConfiguredAsShell = systemShell.ToLower().Contains("cairodesktop");
                        }
                        else
                        {
                            isCairoConfiguredAsShell = false;
                        }
                    }
                }

                return (bool)isCairoConfiguredAsShell;
            }
            set
            {
                if (value != IsCairoConfiguredAsShell)
                {
                    RegistryKey regKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\WinLogon", true);

                    if (value)
                    {
                        // set Cairo as the user's shell
                        regKey.SetValue("Shell", AppDomain.CurrentDomain.BaseDirectory + AppDomain.CurrentDomain.FriendlyName);
                    }
                    else
                    {
                        // reset user's shell to system default
                        object userShell = regKey.GetValue("Shell");

                        if (userShell != null)
                        {
                            regKey.DeleteValue("Shell");
                        }
                    }

                    isCairoConfiguredAsShell = value;
                }
            }
        }

        public static bool IsCairoRunningAsShell;

        private static bool? isServerCore;

        public static bool IsServerCore
        {
            get
            {
                if (isServerCore == null)
                {
                    RegistryKey installationTypeKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion", false);
                    string installationType = installationTypeKey?.GetValue("InstallationType") as string;

                    isServerCore = installationType == "Server Core";
                }

                return (bool)isServerCore;
            }
        }

        // lo = x; hi = y
        public static IntPtr MakeLParam(int loWord, int hiWord)
        {
            int i = ((short)hiWord << 16) | ((short)loWord & 0xffff);
            return new IntPtr(i);
        }
    }
}