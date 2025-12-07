using ManagedShell.Common.Helpers;
using ManagedShell.ShellFolders;
using ManagedShell.WindowsTasks;
using System;
using System.IO;

namespace RetroBar.Utilities
{
    /// <summary>
    /// Helper class to match pinned shortcuts with running programs
    /// </summary>
    public static class TaskbarItemMatcher
    {
        /// <summary>
        /// Determines if a running window matches a pinned shortcut
        /// </summary>
        public static bool DoesWindowMatchPin(ApplicationWindow window, ShellFile pinnedItem)
        {
            if (window == null || pinnedItem == null)
                return false;

            // Get the pinned item's path (this is the .lnk shortcut path)
            string pinnedPath = pinnedItem.Path;
            if (string.IsNullOrEmpty(pinnedPath))
                return false;

            // Get the window's executable path
            string windowExe = window.WinFileName;
            if (string.IsNullOrEmpty(windowExe))
                return false;

            // Method 1: Try to resolve shortcut target
            string pinnedTarget = null;
            if (pinnedPath.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
            {
                pinnedTarget = GetShortcutTarget(pinnedPath);
            }
            else
            {
                pinnedTarget = pinnedPath;
            }

            if (!string.IsNullOrEmpty(pinnedTarget))
            {
                // Direct path comparison
                if (PathsMatch(windowExe, pinnedTarget))
                    return true;

                // Check if paths point to same file (handles different path formats)
                try
                {
                    string fullWindowPath = Path.GetFullPath(windowExe);
                    string fullPinnedPath = Path.GetFullPath(pinnedTarget);

                    if (string.Equals(fullWindowPath, fullPinnedPath, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
                catch
                {
                    // Path resolution failed
                }
            }

            // Method 2: Filename matching (for apps in same folders)
            try
            {
                string pinnedFileName = Path.GetFileNameWithoutExtension(pinnedPath);
                string windowFileName = Path.GetFileNameWithoutExtension(windowExe);

                if (string.Equals(pinnedFileName, windowFileName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            catch
            {
                // Filename extraction failed
            }

            return false;
        }

        /// <summary>
        /// Resolves a .lnk shortcut to its target path using IWshShell
        /// </summary>
        private static string GetShortcutTarget(string shortcutPath)
        {
            if (string.IsNullOrEmpty(shortcutPath))
                return null;

            try
            {
                // Use Windows Script Host to resolve shortcut
                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null)
                    return null;

                object shell = Activator.CreateInstance(shellType);
                object shortcut = shellType.InvokeMember("CreateShortcut",
                    System.Reflection.BindingFlags.InvokeMethod,
                    null, shell, new object[] { shortcutPath });

                Type shortcutType = shortcut.GetType();
                string target = shortcutType.InvokeMember("TargetPath",
                    System.Reflection.BindingFlags.GetProperty,
                    null, shortcut, null) as string;

                System.Runtime.InteropServices.Marshal.ReleaseComObject(shortcut);
                System.Runtime.InteropServices.Marshal.ReleaseComObject(shell);

                return target;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Compares two paths for equality, handling various edge cases
        /// </summary>
        private static bool PathsMatch(string path1, string path2)
        {
            if (string.IsNullOrEmpty(path1) || string.IsNullOrEmpty(path2))
                return false;

            // Direct comparison
            if (string.Equals(path1, path2, StringComparison.OrdinalIgnoreCase))
                return true;

            // Try comparing just the filenames (for apps in different locations)
            try
            {
                string file1 = Path.GetFileName(path1);
                string file2 = Path.GetFileName(path2);

                if (string.Equals(file1, file2, StringComparison.OrdinalIgnoreCase))
                {
                    // Same filename - do additional validation
                    // Check if both are in system directories or both are not
                    bool isSystem1 = IsSystemPath(path1);
                    bool isSystem2 = IsSystemPath(path2);

                    // If both are system paths or both are not, consider them a match
                    if (isSystem1 == isSystem2)
                        return true;
                }
            }
            catch
            {
                // Filename extraction failed
            }

            return false;
        }

        /// <summary>
        /// Checks if a path is in a system directory
        /// </summary>
        private static bool IsSystemPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            string upperPath = path.ToUpperInvariant();

            return upperPath.Contains("\\WINDOWS\\") ||
                   upperPath.Contains("\\PROGRAM FILES\\") ||
                   upperPath.Contains("\\PROGRAM FILES (X86)\\");
        }

        /// <summary>
        /// Gets a grouping key for a pinned item (used to match with running programs)
        /// </summary>
        public static string GetPinGroupKey(ShellFile pinnedItem)
        {
            if (pinnedItem == null)
                return null;

            string path = pinnedItem.Path;
            if (string.IsNullOrEmpty(path))
                return null;

            // Resolve shortcut if it's a .lnk file
            if (path.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
            {
                string target = GetShortcutTarget(path);
                if (!string.IsNullOrEmpty(target))
                {
                    path = target;
                }
            }

            try
            {
                return Path.GetFullPath(path).ToLowerInvariant();
            }
            catch
            {
                return path.ToLowerInvariant();
            }
        }

        /// <summary>
        /// Gets a grouping key for a running window (used to match with pinned items)
        /// </summary>
        public static string GetWindowGroupKey(ApplicationWindow window)
        {
            if (window == null)
                return null;

            // Prefer AppUserModelID for UWP apps
            if (!string.IsNullOrEmpty(window.AppUserModelID))
                return window.AppUserModelID.ToLowerInvariant();

            // Use executable path for Win32 apps
            if (!string.IsNullOrEmpty(window.WinFileName))
            {
                try
                {
                    return Path.GetFullPath(window.WinFileName).ToLowerInvariant();
                }
                catch
                {
                    return window.WinFileName.ToLowerInvariant();
                }
            }

            return window.Title?.ToLowerInvariant();
        }
    }
}
