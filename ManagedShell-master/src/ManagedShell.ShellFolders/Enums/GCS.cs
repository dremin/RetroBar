using System;

namespace ManagedShell.ShellFolders.Enums
{
    // Flags specifying the information to return when calling IContextMenu::GetCommandString
    [Flags]
    public enum GCS : uint
    {
        VERBA = 0,
        HELPTEXTA = 1,
        VALIDATEA = 2,
        VERBW = 4,
        HELPTEXTW = 5,
        VALIDATEW = 6
    }
}
