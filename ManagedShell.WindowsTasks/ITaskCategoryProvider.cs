using System;

namespace ManagedShell.WindowsTasks
{
    public interface ITaskCategoryProvider : IDisposable
    {
        string GetCategory(ApplicationWindow window);

        void SetCategoryChangeDelegate(TaskCategoryChangeDelegate changeDelegate);
    }
}
