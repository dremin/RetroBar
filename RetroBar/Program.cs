using System;
using System.Windows.Forms;

namespace RetroBar
{
    internal sealed class Program
    {
        private const string MutexName = "RetroBar";
        private const int MutexAttempts = 10;
        private const int MutexWaitMs = 1000;

        // Signaled by "RetroBar.exe /exit" to make the running instance exit as if Exit was chosen from its menu
        private const string ExitEventName = "RetroBarExit";
        private const int ExitWaitMs = 15000;

        private static System.Threading.Mutex _retroBarMutex;

        /// <summary>
        /// The main entry point for the application
        /// </summary>
        [STAThread]
        public static int Main(string[] args)
        {
            if (Array.Exists(args, arg => arg.Equals("/exit", StringComparison.OrdinalIgnoreCase)))
            {
                return ExitRunningInstance() ? 0 : 1;
            }

            if (!SingleInstanceCheck())
            {
                return 1;
            }

            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

            App app = new App();
            app.InitializeComponent();

            using var exitEvent = new System.Threading.EventWaitHandle(false, System.Threading.EventResetMode.AutoReset, ExitEventName);
            var exitWait = System.Threading.ThreadPool.RegisterWaitForSingleObject(exitEvent,
                (_, _) => app.Dispatcher.BeginInvoke(new Action(app.ExitGracefully)), null, System.Threading.Timeout.Infinite, true);

            int result = app.Run();
            exitWait.Unregister(null);

            return result;
        }

        // Asks the running instance to exit, then waits until it has closed, so that it can be replaced or started again right away.
        // Returns false if no instance is running.
        private static bool ExitRunningInstance()
        {
            if (!System.Threading.EventWaitHandle.TryOpenExisting(ExitEventName, out var exitEvent))
            {
                return false;
            }

            using (exitEvent)
            {
                exitEvent.Set();
            }

            if (System.Threading.Mutex.TryOpenExisting(MutexName, out var mutex))
            {
                using (mutex)
                {
                    try
                    {
                        if (mutex.WaitOne(ExitWaitMs))
                        {
                            mutex.ReleaseMutex();
                        }
                    }
                    catch (System.Threading.AbandonedMutexException)
                    {
                        // The instance exited without releasing it: this thread owns it now
                        mutex.ReleaseMutex();
                    }
                }
            }

            return true;
        }

        private static bool GetMutex()
        {
            _retroBarMutex = new System.Threading.Mutex(true, MutexName, out bool ok);

            return ok;
        }

        private static bool SingleInstanceCheck()
        {
            for (int i = 0; i < MutexAttempts; i++)
            {
                if (!GetMutex())
                {
                    // Dispose the mutex, otherwise it will never create new
                    _retroBarMutex.Dispose();
                    System.Threading.Thread.Sleep(MutexWaitMs);
                }
                else
                {
                    return true;
                }
            }

            return false;
        }
    }
}
