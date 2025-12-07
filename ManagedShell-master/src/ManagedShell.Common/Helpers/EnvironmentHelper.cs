using System;
using System.Diagnostics;
using ManagedShell.Interop;
using Microsoft.Win32;

namespace ManagedShell.Common.Helpers
{
    public static class EnvironmentHelper
    {
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

        public static bool IsWindows10RS1OrBetter
        {
            get
            {
                if (osVersionMajor == 0)
                {
                    getOSVersion();
                }

                return (osVersionMajor >= 10 && osVersionBuild >= 14393);
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

        public static bool IsWindows1020H1OrBetter
        {
            get
            {
                if (osVersionMajor == 0)
                {
                    getOSVersion();
                }

                return (osVersionMajor >= 10 && osVersionBuild >= 19041);
            }
        }

        public static bool IsWindows11OrBetter
        {
            get
            {
                if (osVersionMajor == 0)
                {
                    getOSVersion();
                }

                return (osVersionMajor >= 10 && osVersionBuild >= 22000);
            }
        }

        public static bool IsWindows1122H2OrBetter
        {
            get
            {
                if (osVersionMajor == 0)
                {
                    getOSVersion();
                }

                return (osVersionMajor >= 10 && osVersionBuild >= 22621);
            }
        }

        public static bool IsWindows1124H2OrBetter
        {
            get
            {
                if (osVersionMajor == 0)
                {
                    getOSVersion();
                }

                return (osVersionMajor >= 10 && osVersionBuild >= 26100);
            }
        }

        public static bool IsWindows10DarkModeSupported
        {
            get
            {
                if (IsServerCore)
                {
                    return false;
                }
                
                if (osVersionMajor == 0)
                {
                    getOSVersion();
                }

                return (osVersionMajor >= 10 && osVersionBuild >= 18362);
            }
        }

        private static bool? isAppConfiguredAsShell;

        /// <summary>
        /// Checks the currently configured shell, NOT the currently running shell! Use IsAppRunningAsShell for that.
        /// </summary>
        public static bool IsAppConfiguredAsShell
        {
            get
            {
                if (isAppConfiguredAsShell == null)
                {
                    // first check if we are the current user's shell
                    RegistryKey userShellKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\WinLogon", false);
                    string userShell = userShellKey?.GetValue("Shell") as string;
                    if (userShell != null)
                    {
                        isAppConfiguredAsShell = userShell.ToLower().Contains(AppDomain.CurrentDomain.FriendlyName.ToLower());
                    }
                    else
                    {
                        // check if we are the current system's shell
                        RegistryKey systemShellKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\WinLogon", false);
                        string systemShell = systemShellKey?.GetValue("Shell") as string;
                        if (systemShell != null)
                        {
                            isAppConfiguredAsShell = systemShell.ToLower().Contains(AppDomain.CurrentDomain.FriendlyName.ToLower());
                        }
                        else
                        {
                            isAppConfiguredAsShell = false;
                        }
                    }
                }

                return (bool)isAppConfiguredAsShell;
            }
            set
            {
                if (value != IsAppConfiguredAsShell)
                {
                    RegistryKey regKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\WinLogon", true);

                    if (value)
                    {
                        // set as the user's shell
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

                    isAppConfiguredAsShell = value;
                }
            }
        }

        public static bool IsAppRunningAsShell;

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

        private static string windowsProductName;

        public static string WindowsProductName
        {
            get
            {
                if (string.IsNullOrEmpty(windowsProductName))
                {
                    RegistryKey versionKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion", false);
                    windowsProductName = versionKey?.GetValue("ProductName") as string;
                }

                return windowsProductName;
            }
        }

        private static bool? isWow64;

        public static bool IsWow64
        {
            get
            {
                if (isWow64 == null)
                {
                    if ((Environment.OSVersion.Version.Major == 5 && Environment.OSVersion.Version.Minor >= 1) || Environment.OSVersion.Version.Major >= 6)
                    {
                        using (Process p = Process.GetCurrentProcess())
                        {
                            try
                            {
                                bool retVal;
                                
                                if (!NativeMethods.IsWow64Process(p.Handle, out retVal))
                                {
                                    isWow64 = false;
                                }
                                else
                                {
                                    isWow64 = retVal;
                                }
                            }
                            catch (Exception)
                            {
                                isWow64 = false;
                            }
                        }
                    }
                    else
                    {
                        isWow64 = false;
                    }
                }

                return (bool)isWow64;
            }
        }
    }
}
