using System;
using System.IO;
using ManagedShell.ShellFolders.Interfaces;

namespace ManagedShell.ShellFolders
{
    public class ShellFile : ShellItem
    {
        private long _fileSize;
        public long FileSize
        {
            get
            {
                if (_fileSize == 0)
                {
                    _fileSize = GetFileSize();
                }

                return _fileSize;
            }
        }
        
        public ShellFile(ShellFolder parentFolder, string parsingName) : base(parsingName)
        {
            _parentItem = parentFolder;
        }

        public ShellFile(ShellFolder parentFolder, IShellFolder parentShellFolder, IntPtr relativePidl, bool isAsync = false) : base(parentFolder.AbsolutePidl, parentShellFolder, relativePidl, isAsync)
        {
            _parentItem = parentFolder;
        }

        private long GetFileSize()
        {
            // TODO: Replace this using properties via IShellItem2
            using (var file = new FileStream(Path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                return file.Length;
            }
        }
    }
}
