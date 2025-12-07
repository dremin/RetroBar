using System;
using System.Collections.Generic;

namespace ManagedShell.Common.Logging
{
    public static class ShellLogger
    {
        #region Delegates

        /// <summary>
        /// Delegate event handler that hooks up requests.
        /// </summary>
        /// <param name="sender">Sender of the event.</param>
        /// <param name="e">Event arguments.</param>
        /// <remarks>
        /// GoF Design Pattern: Observer, Singleton.
        /// The Observer Design Pattern allows Observer classes to attach itself to 
        /// this Logger class and be notified if certain events occur. 
        /// 
        /// The Singleton Design Pattern allows the application to have just one
        /// place that is aware of the application-wide LogSeverity setting.
        /// </remarks>
        public delegate void LogEventHandler(object sender, LogEventArgs e);

        #endregion

        // These Booleans are used strictly to improve performance.
        private static bool _isDebug;
        private static bool _isError;
        private static bool _isFatal;
        private static bool _isInfo;
        private static bool _isWarning;
        private static LogSeverity _severity;

        // Contains the orphaned log events recorded before any observers are attached.
        private static List<LogEventArgs> _orphanedEvents = new List<LogEventArgs>();

        /// <summary>
        /// Private constructor. Initializes default severity to "Debug".
        /// </summary>
        static ShellLogger()
        {
            // Default severity is Debug level
            Severity = LogSeverity.Debug;
        }

        /// <summary>
        /// Gets and sets the severity level of logging activity.
        /// </summary>
        public static LogSeverity Severity
        {
            get { return _severity; }
            set
            {
                _severity = value;

                // Set Booleans to help improve performance
                var severity = (int)_severity;

                _isDebug = ((int)LogSeverity.Debug) >= severity;
                _isInfo = ((int)LogSeverity.Info) >= severity;
                _isWarning = ((int)LogSeverity.Warning) >= severity;
                _isError = ((int)LogSeverity.Error) >= severity;
                _isFatal = ((int)LogSeverity.Fatal) >= severity;
            }
        }

        /// <summary>
        /// The Log event.
        /// </summary>
        public static event LogEventHandler Log;

        /// <summary>
        /// Log a message when severity level is "Debug" or higher.
        /// </summary>
        /// <param name="message">Log message</param>
        public static void Debug(string message)
        {
            // if (_isDebug) // Removed due to the same condition exisiting in the DebugIf call
            DebugIf(true, message, null);
        }

        /// <summary>
        /// Log a message when severity level is "Debug" or higher AND condition is met.
        /// </summary>
        /// <param name="message">Log message</param>
        public static void DebugIf(bool condition, string message)
        {
            // if (_isDebug) // Removed due to the same condition exisiting in the DebugIf call
            DebugIf(condition, message, null);
        }

        /// <summary>
        /// Log a message when severity level is "Debug" or higher.
        /// </summary>
        /// <param name="message">Log message.</param>
        /// <param name="exception">Inner exception.</param>
        public static void Debug(string message, Exception exception)
        {
            // if (_isDebug) // Removed due to the same condition exisiting in the DebugIf call
            DebugIf(true, message, exception);
        }

        /// <summary>
        /// Log a message when severity level is "Debug" or higher AND condition is met.
        /// </summary>
        /// <param name="message">Log message.</param>
        /// <param name="exception">Inner exception.</param>
        public static void DebugIf(bool condition, string message, Exception exception)
        {
            if (_isDebug && condition)
                OnLog(new LogEventArgs(LogSeverity.Debug, message, exception, DateTime.Now));
        }



        /// <summary>
        /// Log a message when severity level is "Info" or higher.
        /// </summary>
        /// <param name="message">Log message</param>
        public static void Info(string message)
        {
            if (_isInfo)
                Info(message, null);
        }

        /// <summary>
        /// Log a message when severity level is "Info" or higher.
        /// </summary>
        /// <param name="message">Log message.</param>
        /// <param name="exception">Inner exception.</param>
        public static void Info(string message, Exception exception)
        {
            if (_isInfo)
                OnLog(new LogEventArgs(LogSeverity.Info, message, exception, DateTime.Now));
        }

        /// <summary>
        /// Log a message when severity level is "Warning" or higher.
        /// </summary>
        /// <param name="message">Log message.</param>
        public static void Warning(string message)
        {
            if (_isWarning)
                Warning(message, null);
        }

        /// <summary>
        /// Log a message when severity level is "Warning" or higher.
        /// </summary>
        /// <param name="message">Log message.</param>
        /// <param name="exception">Inner exception.</param>
        public static void Warning(string message, Exception exception)
        {
            if (_isWarning)
                OnLog(new LogEventArgs(LogSeverity.Warning, message, exception, DateTime.Now));
        }

        /// <summary>
        /// Log a message when severity level is "Error" or higher.
        /// </summary>
        /// <param name="message">Log message</param>
        public static void Error(string message)
        {
            if (_isError)
                Error(message, null);
        }

        /// <summary>
        /// Log a message when severity level is "Error" or higher.
        /// </summary>
        /// <param name="message">Log message.</param>
        /// <param name="exception">Inner exception.</param>
        public static void Error(string message, Exception exception)
        {
            if (_isError)
                OnLog(new LogEventArgs(LogSeverity.Error, message, exception, DateTime.Now));
        }

        /// <summary>
        /// Log a message when severity level is "Fatal"
        /// </summary>
        /// <param name="message">Log message</param>
        public static void Fatal(string message)
        {
            if (_isFatal)
                Fatal(message, null);
        }

        /// <summary>
        /// Log a message when severity level is "Fatal"
        /// </summary>
        /// <param name="message">Log message.</param>
        /// <param name="exception">Inner exception.</param>
        public static void Fatal(string message, Exception exception)
        {
            if (_isFatal)
                OnLog(new LogEventArgs(LogSeverity.Fatal, message, exception, DateTime.Now));
        }

        /// <summary>
        /// Invoke the Log event.
        /// </summary>
        /// <param name="e">Log event parameters.</param>
        public static void OnLog(LogEventArgs e)
        {
            if (Log != null)
            {
                Log(null, e);
            }
            else
            {
                _orphanedEvents.Add(e);
            }
        }

        /// <summary>
        /// Attach a listening observer logging device to logger.
        /// </summary>
        /// <param name="observer">Observer (listening device).</param>
        /// <param name="flushOrphanedEvents">If this is the first observer, whether any log events emitted prior should be re-emitted.</param>
        public static void Attach(ILog observer, bool flushOrphanedEvents = false)
        {
            Log += observer.Log;

            if (flushOrphanedEvents) FlushOrphanedLogEvents();
        }

        /// <summary>
        /// Detach a listening observer logging device from logger.
        /// </summary>
        /// <param name="observer">Observer (listening device).</param>
        public static void Detach(ILog observer)
        {
            Log -= observer.Log;
        }

        public static void Attach(ILog[] observers, bool flushOrphanedEvents = false)
        {
            foreach (var observer in observers)
                Attach(observer);

            if (flushOrphanedEvents) FlushOrphanedLogEvents();
        }

        public static void Detach(ILog[] observers)
        {
            foreach (var observer in observers)
                Detach(observer);
        }

        /// <summary>
        /// Called from the Attach method to re-emit log events that occurred prior to the first observer being attached.
        /// </summary>
        private static void FlushOrphanedLogEvents()
        {
            if (Log == null)
            {
                return;
            }

            foreach (var log in _orphanedEvents)
            {
                OnLog(log);
            }

            _orphanedEvents.Clear();
        }
    }
}
