using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using ManagedShell.Common.Logging;
using static ManagedShell.Interop.NativeMethods;
using ManagedShell.Common.Enums;
using ManagedShell.Common.Interfaces;

namespace ManagedShell.Common.Helpers
{
    public static class ShellHelper
    {
        private const int MAX_PATH = 260;

        public static string GetDisplayName(string filename)
        {
            SHFILEINFO shinfo = new SHFILEINFO();
            shinfo.szDisplayName = string.Empty;
            shinfo.szTypeName = string.Empty;
            SHGetFileInfo(filename, FILE_ATTRIBUTE_NORMAL, ref shinfo, (uint)Marshal.SizeOf(shinfo), (uint)(SHGFI.DisplayName));

            return shinfo.szDisplayName;
        }

        public static string UsersStartMenuPath => GetSpecialFolderPath((int)CSIDL.CSIDL_STARTMENU);

        public static string AllUsersStartMenuPath => GetSpecialFolderPath((int)CSIDL.CSIDL_COMMON_STARTMENU);

        public static string GetSpecialFolderPath(int FOLDER)
        {
            StringBuilder sbPath = new StringBuilder(MAX_PATH);
            SHGetFolderPath(IntPtr.Zero, FOLDER, IntPtr.Zero, 0, sbPath);
            return sbPath.ToString();
        }

        public static bool ExecuteProcess(string filename)
        {
            Process proc = new Process();
            proc.StartInfo.UseShellExecute = true;
            proc.StartInfo.FileName = filename;

            try
            {
                return proc.Start();
            }
            catch
            {
                // No 'Open' command associated with this filetype in the registry
                ShowOpenWithDialog(proc.StartInfo.FileName);
                return false;
            }
        }

        public static bool ActivateApplication(string appUserModelId, string args = "")
        {
            var aam = new ApplicationActivationManager() as IApplicationActivationManager;
            try
            {
                aam.ActivateApplication(appUserModelId, args, ActivateOptions.None);
                return true;
            }
            catch (Exception ex)
            {
                ShellLogger.Error($"Error activating application {appUserModelId} with args \"{args}\". ({ex.Message})");
                return false;
            }
        }

        public static bool StartProcess(string filename, string args = "", string verb = "", string workingDirectory = "")
        {
            try
            {
                if (!Environment.Is64BitProcess)
                {
                    filename.Replace("system32", "sysnative");
                }

                if (filename.StartsWith("appx:"))
                {
                    filename = "shell:appsFolder\\" + filename.Substring(5);
                }
                else if (filename.Contains("://") && string.IsNullOrEmpty(args))
                {
                    args = filename;
                    filename = "explorer.exe";
                }
                else if (EnvironmentHelper.IsAppRunningAsShell && filename.ToLower().EndsWith("explorer.exe") && string.IsNullOrEmpty(args))
                {
                    // if we are shell and launching explorer, give it a parameter so that it doesn't do shell things.
                    // this opens My Computer
                    args = ShellFolderPath.ComputerFolder.Value;
                }

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    UseShellExecute = true,
                    FileName = filename,
                    Arguments = args,
                    Verb = verb,
                    WorkingDirectory = workingDirectory
                };

                Process.Start(psi);

                return true;
            }
            catch (Exception ex)
            {
                ShellLogger.Error($"Error running the {verb} verb on {filename}. ({ex.Message})");

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

        public static bool GetFileExtensionsVisible()
        {
            RegistryKey hideFileExt =
                Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced",
                    false);
            object hideFileExtValue = hideFileExt?.GetValue("HideFileExt");
            
            return hideFileExtValue != null && hideFileExtValue.ToString() == "0";
        }

        public static bool IsFileVisible(string fileName)
        {
            if (Exists(fileName))
            {
                try
                {
                    FileAttributes attributes = File.GetAttributes(fileName);
                    return (((attributes & FileAttributes.Hidden) != FileAttributes.Hidden) && ((attributes & FileAttributes.System) != FileAttributes.System) && ((attributes & FileAttributes.Temporary) != FileAttributes.Temporary));
                }
                catch
                {
                    return false;
                }
            }

            return false;
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
                Environment.GetEnvironmentVariable("WINDIR") + @"\system32\shell32.dll,OpenAs_RunDLL " + fileName;
            owProc.Start();
        }

        public static void StartTaskManager()
        {
            StartProcess("taskmgr.exe");
        }

        public static void ShowRunDialog(string title, string info)
        {
            SHRunFileDialog(IntPtr.Zero, IntPtr.Zero, null, title, info, RunFileDialogFlags.None);
        }

        public static void ShowWindowSwitcher()
        {
            ShellKeyCombo(VK.LWIN, VK.TAB);
        }

        public static void ShowActionCenter()
        {
            ShellKeyCombo(VK.LWIN, VK.KEY_A);
        }

        public static void ShowNotificationCenter()
        {
            if (!EnvironmentHelper.IsWindows11OrBetter)
            {
                return;
            }

            ShellKeyCombo(VK.LWIN, VK.KEY_N);
        }

        public static void ShowStartMenu()
        {
            ShellKeyCombo(VK.LWIN, VK.LWIN);
        }

        public static void ShowStartContextMenu()
        {
            ShellKeyCombo(VK.LWIN, VK.KEY_X);
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
        
        public static void ToggleDesktopIcons(bool enable)
        {
            if (EnvironmentHelper.IsAppRunningAsShell)
            {
                return;
            }

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
                SendMessageTimeout(hWnd, (uint)WM.COMMAND, toggleDesktopCommand, IntPtr.Zero, 0, 5000, ref hWnd);
            }
        }

        public static string GetPathForWindowHandle(IntPtr hWnd)
        {
            StringBuilder outFileName = new StringBuilder(1024);

            // get process id
            var procId = GetProcIdForHandle(hWnd);

            if (procId != 0)
            {
                // open process
                // QueryLimitedInformation flag allows us to access elevated applications as well
                IntPtr hProc = OpenProcess(ProcessAccessFlags.QueryLimitedInformation, false, (int) procId);

                // get path
                int len = outFileName.Capacity;
                QueryFullProcessImageName(hProc, 0, outFileName, ref len);
                CloseHandle(hProc);
            }

            return outFileName.ToString();
        }

        public static uint GetProcIdForHandle(IntPtr hWnd)
        {
            uint procId;
            GetWindowThreadProcessId(hWnd, out procId);
            return procId;
        }

        public static string GetAppUserModelIdPropertyForHandle(IntPtr hWnd)
        {
            string aumid = string.Empty;

            IPropertyStore propStore;
            PropVariant prop;

            var g = new Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99");
            SHGetPropertyStoreForWindow(hWnd, ref g, out propStore);

            PROPERTYKEY PKEY_AppUserModel_ID = new PROPERTYKEY
            {
                fmtid = new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"),
                pid = 5
            };

            if (propStore != null)
            {
                propStore.GetValue(PKEY_AppUserModel_ID, out prop);

                try
                {
                    aumid = prop.Value.ToString();
                }
                catch
                { }

                prop.Clear();
            }

            return aumid;
        }

        public static string GetAppUserModelIdForHandle(IntPtr hWnd)
        {
            if (!EnvironmentHelper.IsWindows8OrBetter)
            {
                return GetAppUserModelIdPropertyForHandle(hWnd);
            }

            uint procId;
            GetWindowThreadProcessId(hWnd, out procId);

            if (procId > 0 && procId < int.MaxValue)
            {
                IntPtr hProcess = OpenProcess(ProcessAccessFlags.QueryLimitedInformation, false, (int)procId);

                uint len = 130;
                StringBuilder outAumid = new StringBuilder((int)len);

                GetApplicationUserModelId(hProcess, ref len, outAumid);
                CloseHandle(hProcess);

                if (outAumid.Length > 0)
                {
                    return outAumid.ToString();
                }
            }

            return GetAppUserModelIdPropertyForHandle(hWnd);
        }

        /// <summary>
        /// Calls the LockWorkStation method on the User32 API.
        /// </summary>
        public static void Lock()
        {
            LockWorkStation();
        }

        /// <summary>
        /// Calls the logoff method on the Win32 API.
        /// </summary>
        public static void Logoff()
        {
            ExitWindowsEx((uint)ExitWindows.Logoff, 0x0);
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
                            return false;
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

        private static void ShellKeyCombo(VK wVk_1, VK wVk_2)
        {
            INPUT[] inputs = new INPUT[4];

            inputs[0].type = INPUT_KEYBOARD;
            inputs[0].mkhi.ki.time = 0;
            inputs[0].mkhi.ki.wScan = 0;
            inputs[0].mkhi.ki.dwExtraInfo = GetMessageExtraInfo();
            inputs[0].mkhi.ki.wVk = (ushort)wVk_1;
            inputs[0].mkhi.ki.dwFlags = 0;

            inputs[1].type = INPUT_KEYBOARD;
            inputs[1].mkhi.ki.wScan = 0;
            inputs[1].mkhi.ki.dwExtraInfo = GetMessageExtraInfo();
            inputs[1].mkhi.ki.wVk = (ushort)wVk_2;
            inputs[1].mkhi.ki.dwFlags = 0;

            inputs[2].type = INPUT_KEYBOARD;
            inputs[2].mkhi.ki.wScan = 0;
            inputs[2].mkhi.ki.dwExtraInfo = GetMessageExtraInfo();
            inputs[2].mkhi.ki.wVk = (ushort)wVk_2;
            inputs[2].mkhi.ki.dwFlags = KEYEVENTF_KEYUP;

            inputs[3].type = INPUT_KEYBOARD;
            inputs[3].mkhi.ki.wScan = 0;
            inputs[3].mkhi.ki.dwExtraInfo = GetMessageExtraInfo();
            inputs[3].mkhi.ki.wVk = (ushort)wVk_1;
            inputs[3].mkhi.ki.dwFlags = KEYEVENTF_KEYUP;

            SendInput(4, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        public static void SetShellReadyEvent()
        {
            int hShellReadyEvent = OpenEvent(EVENT_MODIFY_STATE, true, @"ShellDesktopSwitchEvent");

            if (hShellReadyEvent != 0)
            {
                SetEvent(hShellReadyEvent);
                CloseHandle(hShellReadyEvent);
            }
        }

        public static bool IsSameFile(string path1, string path2)
        {
            if (path1 == path2) return true;

            using (var sfh1 = CreateFile(path1, FileAccess.Read, FileShare.ReadWrite,
                IntPtr.Zero, FileMode.Open, 0, IntPtr.Zero))
            {
                if (sfh1.IsInvalid)
                    ShellLogger.Error($"Win32 error occured when trying to open file {path1}", Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()));

                using (var sfh2 = CreateFile(path2, FileAccess.Read, FileShare.ReadWrite,
                    IntPtr.Zero, FileMode.Open, 0, IntPtr.Zero))
                {
                    if (sfh2.IsInvalid)
                        ShellLogger.Error($"Win32 error occured when trying to open file {path2}", Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()));

                    BY_HANDLE_FILE_INFORMATION fileInfo1;
                    var result1 = GetFileInformationByHandle(sfh1, out fileInfo1);
                    if (!result1)
                        ShellLogger.Error($"GetFileInformationByHandle has failed on {path1}");

                    BY_HANDLE_FILE_INFORMATION fileInfo2;
                    var result2 = GetFileInformationByHandle(sfh2, out fileInfo2);
                    if (!result2)
                        ShellLogger.Error($"GetFileInformationByHandle has failed on {path2}");

                    return fileInfo1.VolumeSerialNumber == fileInfo2.VolumeSerialNumber
                           && fileInfo1.FileIndexHigh == fileInfo2.FileIndexHigh
                           && fileInfo1.FileIndexLow == fileInfo2.FileIndexLow;
                }
            }
        }
    }
}
