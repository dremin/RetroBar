using ManagedShell.AppBar;
using ManagedShell.WindowsTasks;
using ManagedShell.WindowsTray;
using System;
using ManagedShell.Common.Helpers;

namespace ManagedShell
{
    public class ShellManager : IDisposable
    {
        public static readonly ShellConfig DefaultShellConfig = new ShellConfig()
        {
            EnableTasksService = true,
            AutoStartTasksService = true,
            MultiMonAwareTasksService = true,
            TaskIconSize = TasksService.DEFAULT_ICON_SIZE,
            
            EnableTrayService = true,
            AutoStartTrayService = true,
            PinnedNotifyIcons = NotificationArea.DEFAULT_PINNED
        };

        /// <summary>
        /// Initializes ManagedShell with the default configuration.
        /// </summary>
        public ShellManager() : this(DefaultShellConfig)
        {
        }
        
        /// <summary>
        /// Initializes ManagedShell with a custom configuration.
        /// </summary>
        /// <param name="config">A ShellConfig struct containing desired initialization parameters.</param>
        public ShellManager(ShellConfig config)
        {
            if (config.EnableTrayService)
            {
                TrayService = new TrayService();
                ExplorerTrayService = new ExplorerTrayService();
                NotificationArea = new NotificationArea(config.PinnedNotifyIcons, TrayService, ExplorerTrayService);
            }

            if (config.EnableTasksService)
            {
                TasksService = new TasksService(config.TaskIconSize);
                Tasks = new Tasks(TasksService);
            }

            FullScreenHelper = new FullScreenHelper(TasksService);
            ExplorerHelper = new ExplorerHelper(NotificationArea);
            AppBarManager = new AppBarManager(ExplorerHelper);

            if (config.EnableTrayService && config.AutoStartTrayService)
            {
                NotificationArea.Initialize();
            }

            if (config.EnableTasksService && config.AutoStartTasksService)
            {
                Tasks.Initialize(config.MultiMonAwareTasksService);
            }
        }

        public void Dispose()
        {
            IconHelper.DisposeIml();

            AppBarManager.Dispose();
            FullScreenHelper.Dispose();
            NotificationArea?.Dispose();
            Tasks?.Dispose();
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