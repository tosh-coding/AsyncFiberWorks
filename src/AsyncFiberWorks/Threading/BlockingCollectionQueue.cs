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
        private readonly IExecutor _executorSingle;

        private bool _requestedToStop = false;
        private bool _canRunning = true;
        private bool _isDisposed = false;

        /// <summary>
        /// Create a queue with custom executor.
        /// </summary>
        /// <param name="executorSingle">The executor for each operation.</param>
        public BlockingCollectionQueue(IExecutor executorSingle)
        {
            _executorSingle = executorSingle ?? SimpleExecutor.Instance;
        }

        /// <summary>
        /// Create a queue with a simple executor.
        /// </summary>
        public BlockingCollectionQueue()
            : this(SimpleExecutor.Instance)
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
            _executorSingle.Execute(action);
            while (_queue.TryTake(out action))
            {
                _executorSingle.Execute(action);
            }

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
