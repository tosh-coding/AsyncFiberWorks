using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using AsyncFiberWorks.Core;

namespace AsyncFiberWorks.Threading
{
    /// <summary>
    /// Consumer sharing one queue.
    /// </summary>
    internal class SharedBlockingCollectionQueueConsumer
    {
        private readonly object _lock = new object();
        private readonly IActionExceptionHandler _exceptionHandler;
        private readonly BlockingCollection<(WaitCallback, object)> _actions;
        private readonly Action _callbackOnStop;
        private readonly Thread _thread;
        private readonly TaskCompletionSource<bool> _taskCompletionSource;

        private bool _running = true;
        private bool _disposed = false;

        /// <summary>
        /// Initializes a new instance of the queue with the specified exception handler.
        /// </summary>
        /// <param name="actions"></param>
        /// <param name="callbackOnStop"></param>
        /// <param name="exceptionHandler">Handler used to process exceptions thrown by queued actions.</param>
        /// <param name="threadName"></param>
        /// <param name="isBackground"></param>
        /// <param name="priority"></param>
        public SharedBlockingCollectionQueueConsumer(
            BlockingCollection<(WaitCallback, object)> actions,
            Action callbackOnStop,
            IActionExceptionHandler exceptionHandler,
            string threadName,
            bool isBackground = true,
            ThreadPriority priority = ThreadPriority.Normal)
        {
            if (threadName == null)
            {
                throw new ArgumentNullException(nameof(threadName));
            }
            _actions = actions;
            _exceptionHandler = exceptionHandler;
            _callbackOnStop = callbackOnStop;
            _thread = new Thread(() => this.Run());
            _thread.Name = threadName;
            _thread.IsBackground = isBackground;
            _thread.Priority = priority;
            _taskCompletionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        /// <summary>
        /// Start working.
        /// Does not return from the call until it stops.
        /// </summary>
        private void Run()
        {
            try
            {
                while (ExecuteNextBatch()) { }
            }
            finally
            {
                _taskCompletionSource.SetResult(true);
            }
        }

        /// <summary>
        /// Stop working.
        /// Once stopped, it cannot be restarted.
        /// </summary>
        public void Stop()
        {
            lock (_lock)
            {
                if (!_running)
                {
                    return;
                }
                _running = false;
                _actions.Add(((_) => { }, null));
            }
        }

        /// <summary>
        /// Remove all actions and execute.
        /// </summary>
        /// <returns></returns>
        public bool ExecuteNextBatch()
        {
            while (true)
            {
                lock (_lock)
                {
                    if (!_running)
                    {
                        if (!this._disposed)
                        {
                            this._disposed = true;
                            _actions.Add(((_) => { }, null));
                            _callbackOnStop?.Invoke();
                        }
                        return false;
                    }
                }

                var act = _actions.Take();
                do
                {
                    try
                    {
                        act.Item1?.Invoke(act.Item2);
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            _exceptionHandler?.Handle(ex);
                        }
                        catch { }
                    }
                } while (_actions.TryTake(out act));
                return true;
            }
        }

        /// <summary>
        /// Worker thread.
        /// </summary>
        public Thread Thread
        {
            get { return _thread; }
        }

        /// <summary>
        /// Start the thread.
        /// </summary>
        public void Start()
        {
            _thread.Start();
        }

        /// <summary>
        /// Returns a task waiting for thread termination.
        /// </summary>
        public Task JoinAsync()
        {
            return _taskCompletionSource.Task;
        }
    }
}
