using ManagedShell.WindowsTasks;
using ManagedShell.WindowsTray;

namespace ManagedShell.Management
{
    public class ShellManager
    {
        public ShellManager()
        {
            TrayService = new TrayService();
            ExplorerTrayService = new ExplorerTrayService();
            NotificationArea = new NotificationArea(TrayService, ExplorerTrayService);
            TasksService = new TasksService();
            Tasks = new Tasks(TasksService);
        }
        
        public NotificationArea NotificationArea { get; }
        public TasksService TasksService { get; }
        public Tasks Tasks { get; }

        public TrayService TrayService { get; }
        public ExplorerTrayService ExplorerTrayService { get; }
    }
}