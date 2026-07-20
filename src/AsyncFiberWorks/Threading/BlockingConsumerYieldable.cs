using AsyncFiberWorks.Core;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncFiberWorks.Threading
{
    /// <summary>
    /// A thread-safe queue that allows a dedicated consumer thread to process queued actions and can be yielded to stop processing.
    /// </summary>
    public class BlockingConsumerYieldable : IDedicatedConsumerThreadWorkQueue
    {
        private readonly object _lockObj = new object();
        private readonly Queue<(WaitCallback, object)> _queue = new Queue<(WaitCallback, object)>();
        private readonly IActionExceptionHandler _exceptionHandler;

        private long _isRunning = 0;
        private bool _canRunning = true;
        private TaskCompletionSource<bool> _requestedToYield = null;

        /// <summary>
        /// Initializes a new instance of the queue with the specified exception handler.
        /// </summary>
        /// <param name="exceptionHandler">Handler used to process exceptions thrown by queued actions.</param>
        public BlockingConsumerYieldable(IActionExceptionHandler exceptionHandler)
        {
            _exceptionHandler = exceptionHandler;
        }

        /// <summary>
        /// Initializes a new instance of the queue without an exception handler.
        /// </summary>
        public BlockingConsumerYieldable()
            : this(null)
        {
        }

        /// <summary>
        /// Enqueues an action to be executed by the dedicated consumer thread.
        /// </summary>
        /// <param name="action">Action to be executed.</param>
        /// <param name="state">An object containing information to be used by the action. </param>
        public void Enqueue(WaitCallback action, object state)
        {
            lock (_lockObj)
            {
                _queue.Enqueue((action, state));
                Monitor.Pulse(_lockObj);
            }
        }

        /// <summary>
        /// Starts processing the queued actions on the dedicated consumer thread until a yield is requested.
        /// This method blocks the calling thread until yielding occurs.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the queue is already running.</exception>
        public void RunUntilYield()
        {
            if (Interlocked.CompareExchange(ref _isRunning, 1, 0) != 0)
            {
                throw new InvalidOperationException("The queue has already in running.");
            }

            lock (_lockObj)
            {
                if ((_requestedToYield != null) && (!_requestedToYield.Task.IsCompleted))
                {
                    _requestedToYield.SetResult(true);
                }
            }

            while (true)
            {
                lock (_lockObj)
                {
                    while (_queue.Count == 0)
                    {
                        Monitor.Wait(_lockObj);
                    }
                }

                for (; ; )
                {
                    (WaitCallback action, object state) = _queue.Dequeue();
                    try
                    {
                        action?.Invoke(state);
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            _exceptionHandler?.Handle(ex);
                        }
                        catch { }
                    }

                    if (!_canRunning)
                    {
                        _canRunning = true;
                        Interlocked.Exchange(ref _isRunning, 0);
                        return;
                    }

                    lock (_lockObj)
                    {
                        if (_queue.Count <= 0)
                        {
                            break;
                        }
                        (action, state) = _queue.Dequeue();
                    }
                }
            }
        }

        /// <summary>
        /// Requests the dedicated consumer thread to yield, allowing it to stop processing queued actions.
        /// This method returns a task that completes when yielding occurs.
        /// </summary>
        /// <returns>A task that completes when yielding occurs.</returns>
        /// <exception cref="InvalidOperationException">Thrown if a yield request is already in progress.</exception>
        public async Task Yield()
        {
            lock (_lockObj)
            {
                if (_requestedToYield != null)
                {
                    throw new InvalidOperationException();
                }
                _requestedToYield = new TaskCompletionSource<bool>();
            }

            Enqueue((_) => {
                _canRunning = false;
            }, null);

            try
            {
                await _requestedToYield.Task.ConfigureAwait(false);
            }
            finally
            {
                lock (_lockObj)
                {
                    _requestedToYield = null;
                }
            }
        }
    }
}
