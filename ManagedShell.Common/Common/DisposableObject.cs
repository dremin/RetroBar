using System;
using System.ComponentModel;
using System.Diagnostics;

namespace ManagedShell.Common
{
    /// <summary>
    /// Defines a class that is disposable.
    /// </summary>
    [DebuggerStepThrough]
    [Serializable]
    public abstract class DisposableObject : IDisposable
    {
        private readonly object _syncRoot = new object();
        private bool _disposed;

        /// <summary>
        /// Flag indicating whether the object has been disposed
        /// </summary>
        [Browsable(false)]
        public bool Disposed
        {
            get { return _disposed; }
        }

        /// <summary>
        /// Object that can be used to synchronize access
        /// </summary>
        [Browsable(false)]
        public object SyncRoot
        {
            get { return _syncRoot; }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Override to dispose of managed resources
        /// </summary>
        protected virtual void DisposeOfManagedResources()
        {
        }

        /// <summary>
        /// Override to dispose of unmanaged resources
        /// </summary>
        protected virtual void DisposeOfUnManagedResources()
        {
        }


        /// <summary>
        /// Internal disposal function to manage this object's disposed state
        /// </summary>
        /// <param name="disposing"></param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1063:Implement IDisposable Correctly", Justification = "This is a base class for exposing the IDisposable pattern in a reusable fashion.")]
        private void Dispose(bool disposing)
        {
            lock (SyncRoot)
            {
                if (!_disposed)
                {
                    if (disposing)
                    {
                        // dispose of managed resources here
                        DisposeOfManagedResources();
                    }

                    // dispose of unmanaged resources
                    DisposeOfUnManagedResources();

                    _disposed = true;
                }
            }
        }
    }
}