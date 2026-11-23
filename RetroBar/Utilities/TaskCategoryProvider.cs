using ManagedShell.WindowsTasks;

namespace RetroBar.Utilities
{
    public class TaskCategoryProvider : ITaskCategoryProvider
    {
        public string GetCategory(ApplicationWindow window)
        {
            if (!string.IsNullOrEmpty(window.AppUserModelID))
                return window.AppUserModelID;
            
            if (!string.IsNullOrEmpty(window.WinFileName))
                return window.WinFileName;
            
            return window.ProcId?.ToString() ?? "Unknown";
        }

        public void SetCategoryChangeDelegate(TaskCategoryChangeDelegate changeDelegate)
        {
        }

        public void Dispose()
        {
        }
    }
}