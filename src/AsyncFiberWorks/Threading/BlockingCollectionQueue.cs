using System;
using System.Collections.Concurrent;

namespace AsyncFiberWorks.Threading
{
    /// <summary>
    /// Queue for consumer threads. Internally using BlockingCollection class.
    /// </summary>
    public class BlockingCollectionQueue : IDedicatedConsumerThread
    {
        private readonly object _lockObj = new object();
        private readonly BlockingCollection<Action> _queue = new BlockingCollection<Action>();

        private bool _isStarted = false;
        private bool _canRunning = true;
        private bool _isDisposed = false;

        /// <summary>
        /// Enqueue an action.
        /// </summary>
        /// <param name="action"></param>
        public void Enqueue(Action action)
        {
            _queue.Add(action);
        }

        private bool CanRunning
        {
            get
            {
                lock (_lockObj)
                {
                    return _canRunning;
                }
            }
        }

        /// <summary>
        /// Start consumption. Continue until stopped.
        /// </summary>
        public void Run()
        {
            lock (_lockObj)
            {
                if (_isDisposed)
                {
                    return;
                }
                if (_isStarted)
                {
                    return;
                }
                _isStarted = true;
            }
            try
            {
                Action action;
                while (CanRunning)
                {
                    action = _queue.Take();
                    action();
                    while (_queue.TryTake(out action))
                    {
                        action();
                    }
                }
            }
            finally
            {
                lock (_lockObj)
                {
                    _isDisposed = true;
                    _canRunning = false;
                    _queue.Dispose();
                }
            }
        }

        /// <summary>
        /// Stop consumption.
        /// </summary>
        public void Stop()
        {
            lock (_lockObj)
            {
                if (_isDisposed)
                {
                    return;
                }
                _isDisposed = true;

                _canRunning = false;
                if (!_isStarted)
                {
                    _queue.Dispose();
                }
                else
                {
                    Enqueue(() => { });
                }
            }
        }
    }
}
