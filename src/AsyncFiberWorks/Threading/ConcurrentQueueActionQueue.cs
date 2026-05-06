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
        private readonly IActionExceptionHandler _exceptionHandler;

        private bool _requestedToStop = false;

        /// <summary>
        /// Initializes a new instance of the queue without a custom exception handler.
        /// </summary>
        public ConcurrentQueueActionQueue()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the queue with the specified exception handler.
        /// </summary>
        /// <param name="exceptionHandler">Handler used to process exceptions thrown by queued actions.</param>
        public ConcurrentQueueActionQueue(IActionExceptionHandler exceptionHandler)
        {
            _exceptionHandler = exceptionHandler;
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
                try
                {
                    toExecute?.Invoke();
                }
                catch (Exception ex)
                {
                    try
                    {
                        _exceptionHandler?.Handle(ex);
                    }
                    catch { }
                }
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
                try
                {
                    toExecute?.Invoke();
                }
                catch (Exception ex)
                {
                    try
                    {
                        _exceptionHandler?.Handle(ex);
                    }
                    catch { }
                }
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
