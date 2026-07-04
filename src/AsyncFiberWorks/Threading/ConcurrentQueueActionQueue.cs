using AsyncFiberWorks.Core;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace AsyncFiberWorks.Threading
{
    /// <summary>
    /// A task queue that supports immediate manual pumping.
    /// </summary>
    public class ConcurrentQueueActionQueue : IDedicatedConsumerThreadWorkQueue
    {
        private readonly ConcurrentQueue<(WaitCallback, object)> _queue = new ConcurrentQueue<(WaitCallback, object)>();
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
        /// <param name="action">Action to be executed.</param>
        /// <param name="state">An object containing information to be used by the action. </param>
        public void Enqueue(WaitCallback action, object state)
        {
            _queue.Enqueue((action, state));
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

            while (true)
            {
                if (!_queue.TryDequeue(out var toExecute))
                {
                    break;
                }
                try
                {
                    toExecute.Item1?.Invoke(toExecute.Item2);
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
            return true;
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
