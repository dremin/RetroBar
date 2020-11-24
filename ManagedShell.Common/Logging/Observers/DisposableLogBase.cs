namespace ManagedShell.Common.Logging.Observers
{
    public abstract class DisposableLogBase : DisposableObject, ILog
    {
        #region ILog Members

        public virtual void Log(object sender, LogEventArgs e)
        {
        }

        #endregion
    }
}