using System;

namespace ManagedShell.WindowsTasks
{
    public interface ITaskCategoryProvider : IDisposable
    {
        /// <summary>
        /// Called by ApplicationWindow to request its category.
        /// </summary>
        /// <param name="window">ApplicationWindow to get category for</param>
        /// <returns>Category</returns>
        string GetCategory(ApplicationWindow window);

        /// <summary>
        /// Provides the ITaskCategoryProvider with delegate to call when ApplicationWindow
        /// categories need to be re-evaluated.
        /// </summary>
        /// <param name="changeDelegate">Delegate to call when categories change</param>
        void SetCategoryChangeDelegate(TaskCategoryChangeDelegate changeDelegate);
    }
}
