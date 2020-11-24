using System;
using System.Diagnostics;
using System.Windows.Threading;

namespace ManagedShell.Common.Logging.Observers
{
    /// <summary>
    /// Writes log events to the diagnostic trace.
    /// </summary>
    /// <remarks>
    /// GoF Design Pattern: Observer.
    /// The Observer Design Pattern allows this class to attach itself to an
    /// the logger and 'listen' to certain events and be notified of the event. 
    /// </remarks>
    public class ConsoleLog : ILog
    {
        #region ILog Members

        /// <summary>
        /// Writes a log request to the diagnostic trace on the page.
        /// </summary>
        /// <param name="sender">Sender of the log request.</param>
        /// <param name="e">Parameters of the log request.</param>
        public void Log(object sender, LogEventArgs e)
        {
            // Example code of entering a log event to output console
            string message = string.Format("[{0}] {1}: {2}", e.Date, e.SeverityString, e.Message);

            // Writes message to debug output window
            Debugger.Log(0, null, message + "\r\n");
        }

        #endregion
    }
}
