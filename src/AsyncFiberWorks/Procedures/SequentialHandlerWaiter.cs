using System;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncFiberWorks.Procedures
{
    /// <summary>
    /// Use "await" to simply wait for the start of a task on a sequential handler list.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SequentialHandlerWaiter<T> : IDisposable
    {
        private readonly object _lockObj = new object();
        private readonly IDisposable _subscription;
        private readonly ProcessedFlagEventArgs<T> _currentValue = new ProcessedFlagEventArgs<T>();
        private bool _hasValue;
        private bool _isDisposed;
        private readonly SemaphoreSlim _notifierSet = new SemaphoreSlim(0);
        private readonly SemaphoreSlim _notifierClear = new SemaphoreSlim(0);
        private bool _reading;
        private CancellationToken _cancellationToken;

        /// <summary>
        /// Register a handler to the sequential handler list.
        /// </summary>
        /// <param name="handlerList"></param>
        /// <param name="cancellationToken"></param>
        public SequentialHandlerWaiter(ISequentialHandlerListRegistry<T> handlerList, CancellationToken cancellationToken = default)
        {
            _cancellationToken = cancellationToken;
            _subscription = handlerList.Add((arg) => ExecuteAsync(arg));
        }

        /// <summary>
        /// Execute a handler.
        /// </summary>
        /// <param name="newValue"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        async Task<bool> ExecuteAsync(T newValue)
        {
            lock (_lockObj)
            {
                if (_isDisposed)
                {
                    return false;
                }
                if (_cancellationToken.IsCancellationRequested)
                {
                    return false;
                }
                if (_hasValue)
                {
                    throw new InvalidOperationException();
                }
                if (_reading)
                {
                    throw new InvalidOperationException();
                }
                if (_notifierClear.CurrentCount != 0)
                {
                    throw new InvalidOperationException();
                }

                _currentValue.Processed = false;
                _currentValue.Arg = newValue;
                _hasValue = true;
                _notifierSet.Release(1);
            }

            try
            {
                await _notifierClear.WaitAsync(_cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            return _currentValue.Processed;
        }

        /// <summary>
        /// Wait for handler execution.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="OperationCanceledException"></exception>
        public async Task<ProcessedFlagEventArgs<T>> ExecutionStarted()
        {
            lock (_lockObj)
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(GetType().FullName);
                }
                if (_cancellationToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException();
                }
                if (_hasValue && _reading)
                {
                    _currentValue.Arg = default;
                    _hasValue = false;
                    _reading = false;
                    _notifierClear.Release(1);
                }
            }

            await _notifierSet.WaitAsync(_cancellationToken).ConfigureAwait(false);

            lock (_lockObj)
            {
                _reading = true;
                return _currentValue;
            }
        }

        /// <summary>
        /// Unregister a handle from the sequential handler list.
        /// </summary>
        public void Dispose()
        {
            lock (_lockObj)
            {
                if (_isDisposed)
                {
                    return;
                }
                _isDisposed = true;
                _currentValue.Arg = default;
                _hasValue = false;
                _reading = false;
                _notifierClear.Release(1);
                _notifierClear.Dispose();
                _notifierSet.Dispose();
            }

            _subscription.Dispose();
        }
    }
}
