using ManagedShell.Configuration;
using ManagedShell.WindowsTasks;
using ManagedShell.WindowsTray;

namespace ManagedShell.Management
{
    public class ShellManager
    {
        public ShellManager()
        {
            ShellSettings = new ShellSettings();
            TrayService = new TrayService();
            ExplorerTrayService = new ExplorerTrayService();
            NotificationArea = new NotificationArea(ShellSettings, TrayService, ExplorerTrayService);
            TasksService = new TasksService(ShellSettings);
            Tasks = new Tasks(TasksService);
        }

        public ShellSettings ShellSettings { get; }
        public NotificationArea NotificationArea { get; }
        public TasksService TasksService { get; }
        public Tasks Tasks { get; }

        public TrayService TrayService { get; }
        public ExplorerTrayService ExplorerTrayService { get; }
    }
}