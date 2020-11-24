using ManagedShell.Common.DesignPatterns;
using System;

namespace ManagedShell.Common.Logging
{
    /// <summary>
    /// Singleton logger class through which all log events are processed.
    /// </summary>
    /// <remarks>
    /// GoF Design Patterns: Singleton, Observer.
    /// </remarks>
    public sealed class CairoLogger : SingletonObject<CairoLogger>
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
        private bool _isDebug;
        private bool _isError;
        private bool _isFatal;
        private bool _isInfo;
        private bool _isWarning;
        private LogSeverity _severity;

        /// <summary>
        /// Private constructor. Initializes default severity to "Debug".
        /// </summary>
        private CairoLogger()
        {
            // Default severity is Debug level
            Severity = LogSeverity.Debug;
        }

        /// <summary>
        /// Gets and sets the severity level of logging activity.
        /// </summary>
        public LogSeverity Severity
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
        public event LogEventHandler Log;

        /// <summary>
        /// Log a message when severity level is "Debug" or higher.
        /// </summary>
        /// <param name="message">Log message</param>
        public void Debug(string message)
        {
            // if (_isDebug) // Removed due to the same condition exisiting in the DebugIf call
            DebugIf(true, message, null);
        }

        /// <summary>
        /// Log a message when severity level is "Debug" or higher AND condition is met.
        /// </summary>
        /// <param name="message">Log message</param>
        public void DebugIf(bool condition, string message)
        {
            // if (_isDebug) // Removed due to the same condition exisiting in the DebugIf call
            DebugIf(condition, message, null);
        }

        /// <summary>
        /// Log a message when severity level is "Debug" or higher.
        /// </summary>
        /// <param name="message">Log message.</param>
        /// <param name="exception">Inner exception.</param>
        public void Debug(string message, Exception exception)
        {
            // if (_isDebug) // Removed due to the same condition exisiting in the DebugIf call
            DebugIf(true, message, exception);
        }

        /// <summary>
        /// Log a message when severity level is "Debug" or higher AND condition is met.
        /// </summary>
        /// <param name="message">Log message.</param>
        /// <param name="exception">Inner exception.</param>
        public void DebugIf(bool condition, string message, Exception exception)
        {
            if (_isDebug && condition)
                OnLog(new LogEventArgs(LogSeverity.Debug, message, exception, DateTime.Now));
        }



        /// <summary>
        /// Log a message when severity level is "Info" or higher.
        /// </summary>
        /// <param name="message">Log message</param>
        public void Info(string message)
        {
            if (_isInfo)
                Info(message, null);
        }

        /// <summary>
        /// Log a message when severity level is "Info" or higher.
        /// </summary>
        /// <param name="message">Log message.</param>
        /// <param name="exception">Inner exception.</param>
        public void Info(string message, Exception exception)
        {
            if (_isInfo)
                OnLog(new LogEventArgs(LogSeverity.Info, message, exception, DateTime.Now));
        }

        /// <summary>
        /// Log a message when severity level is "Warning" or higher.
        /// </summary>
        /// <param name="message">Log message.</param>
        public void Warning(string message)
        {
            if (_isWarning)
                Warning(message, null);
        }

        /// <summary>
        /// Log a message when severity level is "Warning" or higher.
        /// </summary>
        /// <param name="message">Log message.</param>
        /// <param name="exception">Inner exception.</param>
        public void Warning(string message, Exception exception)
        {
            if (_isWarning)
                OnLog(new LogEventArgs(LogSeverity.Warning, message, exception, DateTime.Now));
        }

        /// <summary>
        /// Log a message when severity level is "Error" or higher.
        /// </summary>
        /// <param name="message">Log message</param>
        public void Error(string message)
        {
            if (_isError)
                Error(message, null);
        }

        /// <summary>
        /// Log a message when severity level is "Error" or higher.
        /// </summary>
        /// <param name="message">Log message.</param>
        /// <param name="exception">Inner exception.</param>
        public void Error(string message, Exception exception)
        {
            if (_isError)
                OnLog(new LogEventArgs(LogSeverity.Error, message, exception, DateTime.Now));
        }

        /// <summary>
        /// Log a message when severity level is "Fatal"
        /// </summary>
        /// <param name="message">Log message</param>
        public void Fatal(string message)
        {
            if (_isFatal)
                Fatal(message, null);
        }

        /// <summary>
        /// Log a message when severity level is "Fatal"
        /// </summary>
        /// <param name="message">Log message.</param>
        /// <param name="exception">Inner exception.</param>
        public void Fatal(string message, Exception exception)
        {
            if (_isFatal)
                OnLog(new LogEventArgs(LogSeverity.Fatal, message, exception, DateTime.Now));
        }

        /// <summary>
        /// Invoke the Log event.
        /// </summary>
        /// <param name="e">Log event parameters.</param>
        public void OnLog(LogEventArgs e)
        {
            if (Log != null)
            {
                Log(this, e);
            }
        }

        /// <summary>
        /// Attach a listening observer logging device to logger.
        /// </summary>
        /// <param name="observer">Observer (listening device).</param>
        public void Attach(ILog observer)
        {
            Log += observer.Log;
        }

        /// <summary>
        /// Detach a listening observer logging device from logger.
        /// </summary>
        /// <param name="observer">Observer (listening device).</param>
        public void Detach(ILog observer)
        {
            Log -= observer.Log;
        }

        public void Attach(ILog[] observers)
        {
            foreach (var observer in observers)
                Attach(observer);
        }

        public void Detach(ILog[] observers)
        {
            foreach (var observer in observers)
                Detach(observer);
        }
    }
}
