using System;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncFiberWorks.Procedures
{
    /// <summary>
    /// Keep only one value. It provides a task from setting the value
    /// to the completion of processing, and a task until it is set.
    /// </summary>
    /// <typeparam name="TArg"></typeparam>
    /// <typeparam name="TRet"></typeparam>
    public class AsyncRegister<TArg, TRet> : IDisposable
    {
        private readonly object _lockObj = new object();
        private IDisposable _subscription;
        private TArg _currentValueArg;
        private TRet _currentValueRet;
        private bool _hasValue;
        private bool _isDisposed;
        private readonly SemaphoreSlim _notifierSet = new SemaphoreSlim(0);
        private readonly SemaphoreSlim _notifierClear = new SemaphoreSlim(0);
        private bool _reading;

        /// <summary>
        /// Subscribe a driver.
        /// </summary>
        /// <param name="subscribable"></param>
        public AsyncRegister(IAsyncActionSubscriber<TArg, TRet> subscribable)
        {
            _subscription = subscribable.Subscribe(async (arg) =>
            {
                return await SetValueAndWaitClearing(arg);
            });
        }

        /// <summary>
        /// Set the return value.
        /// </summary>
        /// <param name="value"></param>
        public void SetReturnValue(TRet value)
        {
            _currentValueRet = value;
        }

        /// <summary>
        /// Set the argument value and wait for it to be cleared.
        /// </summary>
        /// <param name="newValue"></param>
        /// <returns></returns>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<TRet> SetValueAndWaitClearing(TArg newValue)
        {
            lock (_lockObj)
            {
                if (_isDisposed)
                {
                    return _currentValueRet;
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

                _currentValueArg = newValue;
                _hasValue = true;
                _notifierSet.Release(1);
            }

            await _notifierClear.WaitAsync();
            var tmpValue = _currentValueRet;
            _currentValueRet = default;
            return tmpValue;
        }

        /// <summary>
        /// Wait for the argument value to be set.
        /// Call SetReturnValue in advance if necessary.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="OperationCanceledException"></exception>
        public async Task<TArg> WaitSetting(CancellationToken token = default)
        {
            lock (_lockObj)
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(GetType().FullName);
                }
                if (_hasValue && _reading)
                {
                    _currentValueArg = default;
                    _hasValue = false;
                    _reading = false;
                    _notifierClear.Release(1);
                }
            }

            await _notifierSet.WaitAsync(token);

            lock (_lockObj)
            {
                _reading = true;
                return _currentValueArg;
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
                _currentValueArg = default;
                _currentValueRet = default;
                _hasValue = false;
                _reading = false;
                _notifierClear.Release(1);
                _notifierClear.Dispose();
                _notifierSet.Dispose();
            }

            _subscription.Dispose();
        }

        /// <summary>
        /// Has this instance been destroyed?
        /// </summary>
        public bool IsDisposed
        {
            get
            {
                lock (_lockObj)
                {
                    return _isDisposed;
                }
            }
        }
    }
}
