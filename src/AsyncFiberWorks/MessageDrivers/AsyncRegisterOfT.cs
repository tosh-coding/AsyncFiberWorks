using System;
using System.Threading;
using System.Threading.Tasks;
using AsyncFiberWorks.MessageDrivers;

namespace AsyncFiberWorks.Procedures
{
    /// <summary>
    /// Keep only one value. It provides a task from setting the value
    /// to the completion of processing, and a task until it is set.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class AsyncRegister<T> : IDisposable
    {
        private readonly object _lockObj = new object();
        private IDisposable _subscription;
        private T _currentValue;
        private bool _hasValue;
        private bool _isDisposed;
        private readonly SemaphoreSlim _notifierSet = new SemaphoreSlim(0);
        private readonly SemaphoreSlim _notifierClear = new SemaphoreSlim(0);
        private bool _reading;

        /// <summary>
        /// Subscribe a driver.
        /// </summary>
        /// <param name="subscribable"></param>
        public AsyncRegister(IAsyncMessageDriverSubscriber<T> subscribable)
        {
            _subscription = subscribable.Subscribe(async (arg) =>
            {
                await SetValueAndWaitClearing(arg).ConfigureAwait(false);
            });
        }

        /// <summary>
        /// Set the value and wait for it to be cleared.
        /// </summary>
        /// <param name="newValue"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task SetValueAndWaitClearing(T newValue)
        {
            lock (_lockObj)
            {
                if (_isDisposed)
                {
                    return;
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

                _currentValue = newValue;
                _hasValue = true;
                _notifierSet.Release(1);
            }

            await _notifierClear.WaitAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Wait for the value to be set.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="OperationCanceledException"></exception>
        public async Task<T> WaitSetting(CancellationToken token = default)
        {
            lock (_lockObj)
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(GetType().FullName);
                }
                if (_hasValue && _reading)
                {
                    _currentValue = default;
                    _hasValue = false;
                    _reading = false;
                    _notifierClear.Release(1);
                }
            }

            await _notifierSet.WaitAsync(token).ConfigureAwait(false);

            lock (_lockObj)
            {
                _reading = true;
                return _currentValue;
            }
        }

        /// <summary>
        /// Unsubscribe.
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
                _currentValue = default;
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
