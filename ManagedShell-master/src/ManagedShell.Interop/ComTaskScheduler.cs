using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ManagedShell.Interop
{
    /* The MIT License (MIT)

    Copyright (c) 2014 Matthias

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.

    https://github.com/matthiaswelz/journeyofcode/blob/master/SingleThreadScheduler/SingleThreadScheduler/SingleThreadTaskScheduler.cs
    */
    /// <summary>
    ///     Represents a <see cref="TaskScheduler"/> which executes code on a dedicated, single thread whose <see cref="ApartmentState"/> can be configured.
    /// </summary>
    /// <remarks>
    ///     You can use this class if you want to perform operations on a non thread-safe library from a multi-threaded environment.
    /// </remarks>
    public sealed class ComTaskScheduler : TaskScheduler, IDisposable
    {
        private readonly Thread _thread;
        private readonly CancellationTokenSource _cancellationToken;
        private readonly BlockingCollection<Task> _tasks;
        private readonly Action _initAction;

        /// <summary>
        ///     The <see cref="System.Threading.ApartmentState"/> of the <see cref="Thread"/> this <see cref="ComTaskScheduler"/> uses to execute its work.
        /// </summary>
        public ApartmentState ApartmentState { get; private set; }

        /// <summary>
        ///     Indicates the maximum concurrency level this <see cref="T:System.Threading.Tasks.TaskScheduler"/> is able to support.
        /// </summary>
        /// 
        /// <returns>
        ///     Returns <c>1</c>.
        /// </returns>
        public override int MaximumConcurrencyLevel
        {
            get { return 1; }
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ComTaskScheduler"/>, optionally setting an <see cref="System.Threading.ApartmentState"/>.
        /// </summary>
        /// <param name="apartmentState">
        ///     The <see cref="ApartmentState"/> to use. Defaults to <see cref="System.Threading.ApartmentState.STA"/>
        /// </param>
        public ComTaskScheduler(ApartmentState apartmentState = ApartmentState.STA)
            : this(null, apartmentState)
        {

        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ComTaskScheduler"/> passsing an initialization action, optionally setting an <see cref="System.Threading.ApartmentState"/>.
        /// </summary>
        /// <param name="initAction">
        ///     An <see cref="Action"/> to perform in the context of the <see cref="Thread"/> this <see cref="ComTaskScheduler"/> uses to execute its work after it has been started.
        /// </param>
        /// <param name="apartmentState">
        ///     The <see cref="ApartmentState"/> to use. Defaults to <see cref="System.Threading.ApartmentState.STA"/>
        /// </param>
        public ComTaskScheduler(Action initAction, ApartmentState apartmentState = ApartmentState.STA)
        {
            if (apartmentState != ApartmentState.MTA && apartmentState != ApartmentState.STA)
                throw new ArgumentException("apartementState");

            this.ApartmentState = apartmentState;
            this._cancellationToken = new CancellationTokenSource();
            this._tasks = new BlockingCollection<Task>();
            this._initAction = initAction ?? (() => { });

            this._thread = new Thread(this.ThreadStart);
            this._thread.IsBackground = true;
            this._thread.TrySetApartmentState(apartmentState);
            this._thread.Start();
        }


        /// <summary>
        ///     Waits until all scheduled <see cref="Task"/>s on this <see cref="ComTaskScheduler"/> have executed and then disposes this <see cref="ComTaskScheduler"/>.
        /// </summary>
        /// <remarks>
        ///     Calling this method will block execution. It should only be called once.
        /// </remarks>
        /// <exception cref="TaskSchedulerException">
        ///     Thrown when this <see cref="ComTaskScheduler"/> already has been disposed by calling either <see cref="Wait"/> or <see cref="Dispose"/>.
        /// </exception>
        public void Wait()
        {
            if (this._cancellationToken.IsCancellationRequested)
                throw new TaskSchedulerException("Cannot wait after disposal.");

            this._tasks.CompleteAdding();
            this._thread.Join();

            this._cancellationToken.Cancel();
        }

        /// <summary>
        ///     Disposes this <see cref="ComTaskScheduler"/> by not accepting any further work and not executing previously scheduled tasks.
        /// </summary>
        /// <remarks>
        ///     Call <see cref="Wait"/> instead to finish all queued work. You do not need to call <see cref="Dispose"/> after calling <see cref="Wait"/>.
        /// </remarks>
        public void Dispose()
        {
            if (this._cancellationToken.IsCancellationRequested)
                return;

            this._tasks.CompleteAdding();
            this._cancellationToken.Cancel();
        }

        protected override void QueueTask(Task task)
        {
            this.VerifyNotDisposed();

            this._tasks.Add(task, this._cancellationToken.Token);
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            this.VerifyNotDisposed();

            if (this._thread != Thread.CurrentThread)
                return false;
            if (this._cancellationToken.Token.IsCancellationRequested)
                return false;

            this.TryExecuteTask(task);
            return true;
        }

        protected override IEnumerable<Task> GetScheduledTasks()
        {
            this.VerifyNotDisposed();

            return this._tasks.ToArray();
        }

        private void ThreadStart()
        {
            try
            {
                var token = this._cancellationToken.Token;

                this._initAction();

                foreach (var task in this._tasks.GetConsumingEnumerable(token))
                    this.TryExecuteTask(task);
            }
            finally
            {
                this._tasks.Dispose();
            }
        }

        private void VerifyNotDisposed()
        {
            if (this._cancellationToken.IsCancellationRequested)
                throw new ObjectDisposedException(typeof(ComTaskScheduler).Name);
        }
    }
}