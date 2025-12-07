using ManagedShell.ShellFolders.Enums;

namespace ManagedShell.ShellFolders.Structs
{
    internal struct BackgroundFileOperation
    {
        internal string[] Paths;
        internal FileOperation Operation;
        internal string TargetPath;
    }
}
