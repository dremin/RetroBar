using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using ManagedShell.Common.Logging;
using ManagedShell.Interop;
using ManagedShell.ShellFolders.Enums;
using ManagedShell.ShellFolders.Interfaces;

namespace ManagedShell.ShellFolders
{
    public static class ShellLinkHelper
    {
        public static void Save(IShellLink existingLink)
        {
            ((IPersistFile)existingLink).Save(null, true);
        }

        public static void Save(IShellLink link, string destinationPath)
        {
            ((IPersistFile)link).Save(destinationPath, true);
        }

        public static IShellLink Create()
        {
            ShellLink link = new ShellLink();
            IShellLink shellLink = (IShellLink)link;

            return shellLink;
        }

        public static void CreateAndSave(string linkTargetPath, string destinationPath)
        {
            ShellLink link = new ShellLink();
            IShellLink shellLink = (IShellLink)link;

            try
            {
                shellLink.SetPath(linkTargetPath);

                Save(shellLink, destinationPath);
            }
            catch (Exception e)
            {
                ShellLogger.Error($"ShellLinkHelper: Unable to create link from {linkTargetPath} to {destinationPath}", e);
            }

            Marshal.FinalReleaseComObject(link);
        }

        public static IShellLink Load(IntPtr userInputHwnd, string existingLinkPath)
        {
            ShellLink link = new ShellLink();
            IShellLink shellLink = (IShellLink)link;
            IPersistFile persistFile = (IPersistFile)link;

            try
            {
                // load from disk
                persistFile.Load(existingLinkPath, (int)STGM.READ);

                // attempt to resolve a broken shortcut
                SLR_FLAGS flags = new SLR_FLAGS();

                if (userInputHwnd == IntPtr.Zero)
                {
                    flags = SLR_FLAGS.SLR_NO_UI;
                }

                shellLink.Resolve(userInputHwnd, flags);
            }
            catch (Exception e)
            {
                ShellLogger.Error($"ShellLinkHelper: Unable to load link from path {existingLinkPath}", e);
            }

            return shellLink;
        }

        public static string GetLinkTarget(IntPtr userInputHwnd, string filePath)
        {
            IShellLink link = Load(userInputHwnd, filePath);
            string target = "";

            try
            {
                // First, query Windows Installer to see if this is an installed application shortcut
                StringBuilder product = new StringBuilder(39);
                StringBuilder feature = new StringBuilder(39);
                StringBuilder component = new StringBuilder(39);

                uint result = NativeMethods.MsiGetShortcutTarget(filePath, product, feature, component);

                if (result == 0)
                {
                    // This is a Windows Installer shortcut
                    int pathLength = 1024;
                    StringBuilder path = new StringBuilder(pathLength);
                    int installState = NativeMethods.MsiGetComponentPath(product.ToString(), component.ToString(), path, ref pathLength);
                    if (installState == 1)
                    {
                        // Locally installed application
                        target = path.ToString();
                    }
                }
            }
            catch (Exception e)
            {
                ShellLogger.Error($"ShellLinkHelper: Unable to query Windows Installer target for {filePath}", e);
            }

            if (string.IsNullOrEmpty(target))
            {
                try
                {
                    // Check for an associated identifier list to get an IShellItem object from
                    IntPtr pidl = IntPtr.Zero;
                    link.GetIDList(out pidl);

                    if (pidl != IntPtr.Zero)
                    {
                        IShellItem _shellItem;
                        Interop.SHCreateItemFromIDList(pidl, typeof(IShellItem).GUID, out _shellItem);
                        ShellItem item = new ShellItem(_shellItem);
                        target = item.Path;
                    }
                }
                catch (Exception e)
                {
                    ShellLogger.Error($"ShellLinkHelper: Unable to get ID list for {filePath}", e);
                }
            }

            if (string.IsNullOrEmpty(target))
            {
                try
                {
                    // Get the shortcut path as a last resort
                    StringBuilder builder = new StringBuilder(260);
                    link.GetPath(builder, 260, out Structs.WIN32_FIND_DATA pfd, SLGP_FLAGS.SLGP_RAWPATH);
                    target = builder.ToString();
                }
                catch (Exception e)
                {
                    ShellLogger.Error($"ShellLinkHelper: Unable to get path for {filePath}", e);
                }
            }

            return target;
        }
    }
}
