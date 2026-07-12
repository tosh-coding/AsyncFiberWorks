using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using AsyncFiberWorks.Core;

namespace AsyncFiberWorks.Threading
{
    /// <summary>
    /// A task queue that performs manual pumping with blocking.
    /// </summary>
    public class BlockingConsumer : IDedicatedConsumerThreadWorkQueue
    {
        private readonly object _lockObj = new object();
        private readonly BlockingCollection<(WaitCallback, object)> _queue = new BlockingCollection<(WaitCallback, object)>();
        private readonly IActionExceptionHandler _exceptionHandler;
        private readonly TaskCompletionSource<bool> _stopCompletionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        private bool _isStarted = false;
        private bool _requestedToStop = false;
        private bool _canRunning = true;
        private bool _isDisposed = false;

        /// <summary>
        /// Initializes a new instance of the queue with the specified exception handler.
        /// </summary>
        /// <param name="exceptionHandler">Handler used to process exceptions thrown by queued actions.</param>
        public BlockingConsumer(IActionExceptionHandler exceptionHandler)
        {
            _exceptionHandler = exceptionHandler;
        }

        /// <summary>
        /// Initializes a new instance of the queue without a custom exception handler.
        /// </summary>
        public BlockingConsumer()
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
            lock (_lockObj)
            {
                if (_isDisposed)
                {
                    return;
                }
                _queue.Add((action, state));
            }
        }

        /// <summary>
        /// Start consumption. Continue until disposed.
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public void Run()
        {
            lock (_lockObj)
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(BlockingConsumer));
                }
                if (_isStarted)
                {
                    throw new InvalidOperationException("The consumer has already been started.");
                }
                _isStarted = true;
            }

            while (true)
            {
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

                    if (!_canRunning)
                    {
                        _isDisposed = true;
                        _queue.Dispose();
                        _stopCompletionSource.SetResult(true);
                        return;
                    }
                } while (_queue.TryTake(out action));
            }
        }

        /// <summary>
        /// Stop consumption.
        /// </summary>
        /// <returns></returns>
        public Task StopAsync()
        {
            lock (_lockObj)
            {
                if (_requestedToStop)
                {
                    return _stopCompletionSource.Task;
                }
                _requestedToStop = true;

                if (!_isStarted)
                {
                    _isDisposed = true;
                    _queue.Dispose();
                    _stopCompletionSource.SetResult(true);
                    return _stopCompletionSource.Task;
                }
            }

            Enqueue((arg) => {
                var me = (BlockingConsumer)arg;
                me._canRunning = false;
            }, this);
            return _stopCompletionSource.Task;
        }
    }
}
