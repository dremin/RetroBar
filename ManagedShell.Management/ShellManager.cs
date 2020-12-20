using ManagedShell.AppBar;
using ManagedShell.WindowsTasks;
using ManagedShell.WindowsTray;
using System;
using ManagedShell.Common.Helpers;

namespace ManagedShell.Management
{
    public class ShellManager : IDisposable
    {
        public ShellManager()
        {
            TrayService = new TrayService();
            ExplorerTrayService = new ExplorerTrayService();
            NotificationArea = new NotificationArea(TrayService, ExplorerTrayService);
            
            TasksService = new TasksService();
            Tasks = new Tasks(TasksService);

            FullScreenHelper = new FullScreenHelper();
            ExplorerHelper = new ExplorerHelper(NotificationArea);
            AppBarManager = new AppBarManager(ExplorerHelper);
        }

        public void Dispose()
        {
            Shell.DisposeIml();

            FullScreenHelper.Dispose();
            NotificationArea.Dispose();
            Tasks.Dispose();
        }

        public NotificationArea NotificationArea { get; }
        public TrayService TrayService { get; }
        public ExplorerTrayService ExplorerTrayService { get; }

        public TasksService TasksService { get; }
        public Tasks Tasks { get; }

        public AppBarManager AppBarManager { get; }
        public ExplorerHelper ExplorerHelper { get; }
        public FullScreenHelper FullScreenHelper { get; }
    }
}