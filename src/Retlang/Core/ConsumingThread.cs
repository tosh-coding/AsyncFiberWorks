using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Retlang.Core
{
    /// <summary>
    /// ConsumingThread does not use a backing thread or a thread pool for execution.
    /// Actions can be executed synchronously by the calling thread.
    /// </summary>
    public class ConsumingThread : IThreadPool
    {
        private readonly object _lock = new object();
        private readonly BlockingCollection<Action> _queue= new BlockingCollection<Action>();

        private bool _running = true;

        /// <summary>
        /// Enqueues action.
        /// </summary>
        /// <param name="callback"></param>
        public void Queue(WaitCallback callback)
        {
            _queue.Add(() => callback(null));
        }

        private bool IsRunning
        {
            get
            {
                lock (_lock)
                {
                    return _running;
                }
            }
        }

        /// <summary>
        /// Execute actions until stopped.
        /// </summary>
        public void Run()
        {
            Action action;
            while (IsRunning)
            {
                action = _queue.Take();
                action();
                while (_queue.TryTake(out action))
                {
                    action();
                }
            }
        }

        /// <summary>
        /// Stop consuming the actions.
        /// </summary>
        public void Stop()
        {
            lock (_lock)
            {
                _running = false;
                Queue((_) => { });
            }
        }
    }
}
