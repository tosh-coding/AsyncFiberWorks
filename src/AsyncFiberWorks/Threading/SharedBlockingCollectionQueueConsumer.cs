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
        private readonly IExecutor _executor;
        private readonly BlockingCollection<Action> _actions;
        private readonly Action _callbackOnStop;
        private readonly UserWorkerThread _thread;

        private bool _running = true;
        private bool _disposed = false;

        /// <summary>
        /// Create a consumer with custom executor
        /// </summary>
        /// <param name="actions"></param>
        /// <param name="executor"></param>
        /// <param name="threadName"></param>
        /// <param name="callbackOnStop"></param>
        public SharedBlockingCollectionQueueConsumer(
            BlockingCollection<Action> actions,
            Action callbackOnStop,
            IExecutor executor,
            string threadName,
            bool isBackground = true,
            ThreadPriority priority = ThreadPriority.Normal)
        {
            _actions = actions;
            _executor = executor;
            _thread = new UserWorkerThread(this.Run, threadName, isBackground, priority);
            _callbackOnStop = callbackOnStop;
        }

        /// <summary>
        /// Start working.
        /// Does not return from the call until it stops.
        /// </summary>
        private void Run()
        {
            while (ExecuteNextBatch()) { }
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
                _actions.Add(() => { });
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
                            _actions.Add(() => { });
                            _callbackOnStop?.Invoke();
                        }
                        return false;
                    }
                }

                var act = _actions.Take();
                _executor.Execute(act);
                while (_actions.TryTake(out act))
                {
                    _executor.Execute(act);
                }
                return true;
            }
        }

        /// <summary>
        /// Worker thread.
        /// </summary>
        public Thread Thread
        {
            get { return _thread.Thread; }
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
        public async Task JoinAsync()
        {
            await _thread.JoinAsync();
        }
    }
}
