using System;
using System.Collections.Concurrent;
using AsyncFiberWorks.Core;

namespace AsyncFiberWorks.Threading
{
    /// <summary>
    /// Queue for consumer threads. Internally using BlockingCollection class.
    /// </summary>
    public class BlockingCollectionQueue : IDedicatedConsumerThreadWork
    {
        private readonly object _lockObj = new object();
        private readonly BlockingCollection<Action> _queue = new BlockingCollection<Action>();
        private readonly IActionExceptionHandler _exceptionHandler;

        private bool _requestedToStop = false;
        private bool _canRunning = true;
        private bool _isDisposed = false;

        /// <summary>
        /// Initializes a new instance of the queue with the specified exception handler.
        /// </summary>
        /// <param name="exceptionHandler">Handler used to process exceptions thrown by queued actions.</param>
        public BlockingCollectionQueue(IActionExceptionHandler exceptionHandler)
        {
            _exceptionHandler = exceptionHandler;
        }

        /// <summary>
        /// Initializes a new instance of the queue without a custom exception handler.
        /// </summary>
        public BlockingCollectionQueue()
            : this(null)
        {
        }

        /// <summary>
        /// Enqueue an action.
        /// </summary>
        /// <param name="action"></param>
        public void Enqueue(Action action)
        {
            _queue.Add(action);
        }

        /// <summary>
        /// Perform pending actions.
        /// </summary>
        /// <returns>Still in operation. False if already stopped.</returns>
        public bool ExecuteNextBatch()
        {
            if (_isDisposed)
            {
                return false;
            }

            Action action = _queue.Take();
            do
            {
                try
                {
                    action?.Invoke();
                }
                catch (Exception ex)
                {
                    try
                    {
                        _exceptionHandler?.Handle(ex);
                    }
                    catch { }
                }
            } while (_queue.TryTake(out action));

            if (!_canRunning)
            {
                _isDisposed = true;
                _queue.Dispose();
                return false;
            }
            return true;
        }

        /// <summary>
        /// Stop consumption.
        /// </summary>
        public void Stop()
        {
            lock (_lockObj)
            {
                if (_requestedToStop)
                {
                    return;
                }
                _requestedToStop = true;
            }

            Enqueue(() => {
                _canRunning = false;
            });
        }
    }
}
