using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using ManagedShell.Common.Enums;
using ManagedShell.Common.Helpers;
using ManagedShell.Common.Logging;
using ManagedShell.Common.Structs;
using Microsoft.Win32;

namespace ManagedShell.Common.SupportingClasses
{
    public class StartupRunner
    {
        static readonly StartupLocation[] StartupEntries =
        {
            new StartupLocation { Type = StartupEntryType.RegistryKey,
                Location = @"Software\Microsoft\Windows\CurrentVersion\Run",
                ApprovedLocation = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run",
                Scope = StartupEntryScope.All },
            new StartupLocation { Type = StartupEntryType.RegistryKey,
                Location = @"Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Run",
                ApprovedLocation = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run32",
                Scope = StartupEntryScope.All },
            new StartupLocation { Type = StartupEntryType.RegistryKey,
                Location = @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer\Run",
                Scope = StartupEntryScope.All },
            new StartupLocation { Type = StartupEntryType.RegistryKey,
                Location = @"Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Policies\Explorer\Run",
                Scope = StartupEntryScope.All },
            new StartupLocation { Type = StartupEntryType.RegistryKey,
                Location = @"Software\Microsoft\Windows\CurrentVersion\RunOnce",
                Scope = StartupEntryScope.All },
            new StartupLocation { Type = StartupEntryType.RegistryKey,
                Location = @"Software\Wow6432Node\Microsoft\Windows\CurrentVersion\RunOnce",
                Scope = StartupEntryScope.All },
            new StartupLocation { Type = StartupEntryType.RegistryKey,
                Location = @"Software\Microsoft\Windows\CurrentVersion\RunOnceEx",
                Scope = StartupEntryScope.All },
            new StartupLocation { Type = StartupEntryType.RegistryKey,
                Location = @"Software\Wow6432Node\Microsoft\Windows\CurrentVersion\RunOnceEx",
                Scope = StartupEntryScope.All },
            new StartupLocation { Type = StartupEntryType.Directory,
                Location = @"%programdata%\Microsoft\Windows\Start Menu\Programs\Startup",
                ApprovedLocation = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\StartupFolder",
                Scope = StartupEntryScope.Machine },
            new StartupLocation { Type = StartupEntryType.Directory,
                Location = @"%appdata%\Microsoft\Windows\Start Menu\Programs\Startup",
                ApprovedLocation = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\StartupFolder",
                Scope = StartupEntryScope.User },
        };

        public async void Run()
        {
            await Task.Run(RunStartupApps);
        }

        #region Startup methods

        private void RunStartupApps()
        {
            foreach (StartupEntry app in GetStartupApps())
            {
                string[] procInfo = expandArgs(app);

                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.UseShellExecute = true;
                startInfo.FileName = procInfo[0];
                startInfo.Arguments = procInfo[1];

                ShellLogger.Debug($"StartupRunner: Starting program: {startInfo.FileName}");

                try
                {
                    Process.Start(startInfo);
                }
                catch
                {
                    ShellLogger.Info($"StartupRunner: Failed to start program: {startInfo.FileName}");
                }
            }
        }

        private List<StartupEntry> GetStartupApps()
        {
            List<StartupEntry> startupApps = new List<StartupEntry>();

            foreach (var entry in StartupEntries)
            {
                startupApps.AddRange(GetAppsFromEntry(entry));
            }

            return startupApps;
        }

        private List<string> GetDisallowedItems(StartupLocation location, StartupEntryScope? overrideScope = null)
        {
            List<string> disallowedApps = new List<string>();

            if (!string.IsNullOrEmpty(location.ApprovedLocation))
            {
                StartupEntryScope scope = overrideScope ?? location.Scope;
                RegistryKey[] roots = ScopeToRoots(scope);

                foreach (var root in roots)
                {
                    try
                    {
                        RegistryKey registryKey =
                            root.OpenSubKey(location.ApprovedLocation, false);

                        if (registryKey != null && registryKey.ValueCount > 0)
                        {
                            foreach (var valueName in registryKey.GetValueNames())
                            {
                                if (((byte[])registryKey.GetValue(valueName))[0] % 2 != 0) // if value is odd number, item is disabled
                                {
                                    disallowedApps.Add(valueName);
                                    ShellLogger.Debug($"StartupRunner: Skipping disabled entry: {valueName}");
                                }
                            }
                        }

                        // close key when finished
                        registryKey?.Close();
                    }
                    catch
                    {
                        ShellLogger.Warning($"StartupRunner: Unable to load allowed startup items list from registry key {location.ApprovedLocation}");
                    }
                }
            }

            return disallowedApps;
        }

        private List<StartupEntry> GetAppsFromEntry(StartupLocation location)
        {
            switch (location.Type)
            {
                case StartupEntryType.Directory:
                    return GetAppsFromDirectory(location);
                case StartupEntryType.RegistryKey:
                    return GetAppsFromRegistryKey(location);
                default:
                    ShellLogger.Debug("StartupRunner: Unknown startup location type");
                    break;
            }

            return new List<StartupEntry>();
        }

        private List<StartupEntry> GetAppsFromDirectory(StartupLocation location)
        {
            List<StartupEntry> startupApps = new List<StartupEntry>();
            List<string> disallowedItems = GetDisallowedItems(location);
            string locationExpanded = Environment.ExpandEnvironmentVariables(location.Location);

            try
            {
                if (ShellHelper.Exists(locationExpanded))
                {
                    foreach (string startupFile in Directory.EnumerateFiles(locationExpanded))
                    {
                        if (!ShellHelper.IsFileVisible(startupFile))
                        {
                            continue;
                        }
                        
                        // only add items that are not disabled
                        if (!disallowedItems.Contains(Path.GetFileName(startupFile)))
                        {
                            startupApps.Add(new StartupEntry
                            {
                                Location = location,
                                Path = startupFile
                            });
                        }
                    }
                }
            }
            catch
            {
                ShellLogger.Warning($"StartupRunner: Unable to load startup items from directory {location}");
            }

            return startupApps;
        }


        private List<StartupEntry> GetAppsFromRegistryKey(StartupLocation location)
        {
            RegistryKey[] roots = ScopeToRoots(location.Scope);
            List<StartupEntry> startupApps = new List<StartupEntry>();

            foreach (var root in roots)
            {
                bool isRunOnce = location.Location.Contains("RunOnce");

                try
                {
                    if (isRunOnce && root == Registry.LocalMachine)
                        continue; // skip HKLM RunOnce since we cannot delete these items, and would run them each startup

                    RegistryKey registryKey =
                        root.OpenSubKey(location.Location, root == Registry.CurrentUser); // open as writeable if HKCU

                    if (registryKey != null && registryKey.ValueCount > 0)
                    {
                        // get list of disallowed entries
                        List<string> disallowedItems = GetDisallowedItems(location, root == Registry.LocalMachine ? StartupEntryScope.Machine : StartupEntryScope.User);

                        // add items from registry key
                        foreach (var valueName in registryKey.GetValueNames())
                        {
                            // only add items that are not disabled
                            if (!disallowedItems.Contains(valueName))
                            {
                                startupApps.Add(new StartupEntry
                                {
                                    Location = location,
                                    Path = ((string)registryKey.GetValue(valueName)).Replace("\"", "")
                                });

                                // if this is a runonce key, remove the value after we grab it
                                if (isRunOnce)
                                {
                                    try
                                    {
                                        registryKey.DeleteValue(valueName);
                                    }
                                    catch
                                    {
                                        ShellLogger.Warning($"StartupRunner: Unable to delete RunOnce startup item {valueName}");
                                    }
                                }
                            }
                        }
                    }

                    // close key when finished
                    registryKey?.Close();
                }
                catch
                {
                    ShellLogger.Warning($"StartupRunner: Unable to load startup items from registry key {location.Location}");
                }
            }

            return startupApps;
        }

        private RegistryKey[] ScopeToRoots(StartupEntryScope scope)
        {
            RegistryKey[] roots = { };

            switch (scope)
            {
                case StartupEntryScope.All:
                    roots = new[] { Registry.LocalMachine, Registry.CurrentUser };
                    break;
                case StartupEntryScope.Machine:
                    roots = new[] { Registry.LocalMachine };
                    break;
                case StartupEntryScope.User:
                    roots = new[] { Registry.CurrentUser };
                    break;
            }

            return roots;
        }

        private static string[] expandArgs(StartupEntry startupEntry)
        {
            string[] procInfo = new string[2];

            // don't bother expanding paths we know have no args
            if (startupEntry.Location.Type != StartupEntryType.Directory)
            {
                int exeIndex = startupEntry.Path.IndexOf(".exe");

                // we may have args for an executable
                if (exeIndex > 0 && exeIndex + 4 != startupEntry.Path.Length)
                {
                    // argh, args!
                    procInfo[0] = startupEntry.Path.Substring(0, exeIndex + 4);
                    procInfo[1] = startupEntry.Path.Substring(exeIndex + 5, startupEntry.Path.Length - exeIndex - 5);
                }
                else
                {
                    procInfo[0] = startupEntry.Path;
                }
            }
            else
            {
                // no args to parse out
                procInfo[0] = startupEntry.Path;
            }

            return procInfo;
        }
        #endregion
    }
}