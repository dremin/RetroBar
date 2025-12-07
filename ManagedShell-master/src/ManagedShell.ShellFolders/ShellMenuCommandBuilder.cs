using System.Collections.Generic;
using ManagedShell.ShellFolders.Enums;

namespace ManagedShell.ShellFolders
{
    public class ShellMenuCommandBuilder
    {
        public List<ShellMenuCommand> Commands = new List<ShellMenuCommand>();
        public uint DefaultItemUID;

        public void AddCommand(ShellMenuCommand command)
        {
            Commands.Add(command);
        }

        public void AddSeparator()
        {
            Commands.Add(new ShellMenuCommand {Flags = MFT.SEPARATOR, Label = string.Empty, UID = 0});
        }

        public void AddShellNewMenu()
        {
            Commands.Add(new ShellNewMenuCommand());
        }
    }
}
