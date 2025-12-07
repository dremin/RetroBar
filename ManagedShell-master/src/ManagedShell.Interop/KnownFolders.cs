using System;
using System.Runtime.InteropServices;

namespace ManagedShell.Interop
{
    /// <summary>
    /// Standard folders registered with the system that aren't provided by Environment.SpecialFolder.
    /// </summary>
    public enum KnownFolder
    {
        Contacts,
        Downloads,
        Links,
        SavedGames,
        SavedSearches
    }
    
    /// <summary>
    /// Class containing methods to retrieve specific file system paths that aren't provided by Environment.SpecialFolder.
    /// </summary>
    public static class KnownFolders
    {
        private static readonly string[] _knownFolderGuids = {
            "{56784854-C6CB-462B-8169-88E350ACB882}", // Contacts
            "{374DE290-123F-4565-9164-39C4925E467B}", // Downloads
            "{BFB9D5E0-C6A9-404C-B2B2-AE6DB6AF4968}", // Links
            "{4C5C32FF-BB9D-43B0-B5B4-2D72E54EAAA4}", // SavedGames
            "{7D1D3A04-DEBB-4115-95CF-2F29DA2920DA}" // SavedSearches
        };

        /// <summary>
        /// Gets the current path to the specified known folder as currently configured. This does
        /// not require the folder to exist.
        /// </summary>
        /// <param name="knownFolder">The known folder which current path will be returned.</param>
        /// <returns>The default path of the known folder, or an empty string if the path couldn't be retrieved.</returns>
        public static string GetPath(KnownFolder knownFolder)
        {
            return GetPath(knownFolder, NativeMethods.KnownFolderFlags.DontVerify);
        }

        /// <summary>
        /// Gets the current path to the specified known folder as currently configured.
        /// </summary>
        /// <param name="knownFolder">The known folder which current path will be returned.</param>
        /// <param name="flags">The known folder flags to use.</param>
        /// <returns>The default path of the known folder, or an empty string if the path couldn't be retrieved.</returns>
        public static string GetPath(KnownFolder knownFolder, NativeMethods.KnownFolderFlags flags)
        {
            return GetPath(knownFolder, flags, false);
        }

        /// <summary>
        /// Gets the current path to the specified known folder as currently configured.
        /// </summary>
        /// <param name="knownFolder">The known folder which current path will be returned.</param>
        /// <param name="flags">The known folder flags to use.</param>
        /// <param name="defaultUser">Specifies if the paths of the default user (user profile
        ///     template) will be used. This requires administrative rights.</param>
        /// <returns>The default path of the known folder, or an empty string if the path couldn't be retrieved.</returns>
        public static string GetPath(KnownFolder knownFolder, NativeMethods.KnownFolderFlags flags,
            bool defaultUser)
        {
            int result = NativeMethods.SHGetKnownFolderPath(new Guid(_knownFolderGuids[(int)knownFolder]),
                (uint)flags, new IntPtr(defaultUser ? -1 : 0), out IntPtr outPath);
            
            if (result >= 0)
            {
                return Marshal.PtrToStringUni(outPath);
            }

            return string.Empty;
        }
    }
}
