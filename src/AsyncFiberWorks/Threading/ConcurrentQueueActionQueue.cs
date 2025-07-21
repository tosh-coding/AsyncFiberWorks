using AsyncFiberWorks.Core;
using System;
using System.Collections.Concurrent;

namespace AsyncFiberWorks.Threading
{
    /// <summary>
    /// Task queue for consumer threads. Internally using ConcurrentQueue class.
    /// </summary>
    public class ConcurrentQueueActionQueue : IDedicatedConsumerThreadWork
    {
        private readonly ConcurrentQueue<Action> _queue = new ConcurrentQueue<Action>();
        private readonly IExecutor _executor;

        private bool _requestedToStop = false;

        /// <summary>
        /// Create a task queue with a simple executor.
        /// </summary>
        public ConcurrentQueueActionQueue()
            : this(SimpleExecutor.Instance)
        {
        }

        /// <summary>
        /// Create a task queue with the specified executor.
        /// </summary>
        /// <param name="executor"></param>
        public ConcurrentQueueActionQueue(IExecutor executor)
        {
            _executor = executor;
        }

        /// <summary>
        /// Enqueue a single action.
        /// </summary>
        /// <param name="action"></param>
        public void Enqueue(Action action)
        {
            _queue.Enqueue(action);
        }

        /// <summary>
        /// Perform pending actions.
        /// Non-blocking. Returns immediately if there is no task.
        /// </summary>
        /// <returns>Still in operation. False if already stopped.</returns>
        public bool ExecuteNextBatch()
        {
            if (_requestedToStop)
            {
                return false;
            }
            ExecuteAll();
            return true;
        }

        /// <summary>
        /// Execute until there are no more pending actions.
        /// </summary>
        public void ExecuteAll()
        {
            while (true)
            {
                if (!_queue.TryDequeue(out var toExecute))
                {
                    break;
                }
                _executor.Execute(toExecute);
            }
        }

        /// <summary>
        /// Execute only what is pending now.
        /// </summary>
        public void ExecuteOnlyPendingNow()
        {
            int count = _queue.Count;
            while (true)
            {
                if (!_queue.TryDequeue(out var toExecute))
                {
                    break;
                }
                _executor.Execute(toExecute);
                count -= 1;
                if (count <= 0)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Stop consumption.
        /// </summary>
        public void Stop()
        {
            _requestedToStop = true;
        }
    }
}
