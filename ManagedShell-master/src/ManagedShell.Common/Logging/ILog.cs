namespace ManagedShell.Common.Logging
{
    /// <summary>
    /// Defines a single method to write requested log events to an output device.
    /// </summary>
    public interface ILog
    {
        /// <summary>
        /// Write a log request to a given output device.
        /// </summary>
        /// <param name="sender">Sender of the log request.</param>
        /// <param name="e">Parameters of the log request.</param>
        void Log(object sender, LogEventArgs e);
    }
}
