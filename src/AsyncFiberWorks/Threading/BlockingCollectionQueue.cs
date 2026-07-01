using System;
using System.Collections.Concurrent;
using System.Threading;
using AsyncFiberWorks.Core;

namespace AsyncFiberWorks.Threading
{
    /// <summary>
    /// Queue for consumer threads. Internally using BlockingCollection class.
    /// </summary>
    public class BlockingCollectionQueue : IDedicatedConsumerThreadWorkQueue
    {
        private readonly object _lockObj = new object();
        private readonly BlockingCollection<(WaitCallback, object)> _queue = new BlockingCollection<(WaitCallback, object)>();
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
        /// <param name="action">Action to be executed.</param>
        /// <param name="state">An object containing information to be used by the action. </param>
        public void Enqueue(WaitCallback action, object state)
        {
            _queue.Add((action, state));
        }

        /// <summary>
        /// Start consumption. Continue until stopped.
        /// Make the current thread available as an IThreadPool.
        /// </summary>
        public void Run()
        {
            while (this.ExecuteNextBatch()) { }
        }

        /// <summary>
        /// Perform pending actions.
        /// </summary>
        /// <returns>Still in operation. False if already stopped.</returns>
        bool ExecuteNextBatch()
        {
            if (_isDisposed)
            {
                return false;
            }

            var action = _queue.Take();
            do
            {
                try
                {
                    action.Item1?.Invoke(action.Item2);
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

            Enqueue((_) => {
                _canRunning = false;
            }, null);
        }
    }
}
