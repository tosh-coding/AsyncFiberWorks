using AsyncFiberWorks.Threading;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncFiberWorks.Procedures
{
    /// <summary>
    /// Keep only one value. It provides a task from setting the flag
    /// to the completion of processing, and a task until it is set.
    /// </summary>
    public class AsyncRegister : IDisposable
    {
        private readonly object _lockObj = new object();
        private IDisposable _subscription;
        private bool _hasValue;
        private bool _isDisposed;
        private readonly ManualResetEventSlim _notifierSet = new ManualResetEventSlim();
        private readonly ManualResetEventSlim _notifierClear = new ManualResetEventSlim();
        private readonly UserThreadPool _thread;
        private bool _reading;
        private CancellationToken _cancellationToken;

        /// <summary>
        /// Subscribe a task list.
        /// </summary>
        /// <param name="taskList"></param>
        /// <param name="cancellationToken"></param>
        public AsyncRegister(ISequentialTaskListRegistry taskList, CancellationToken cancellationToken = default)
        {
            _thread = UserThreadPool.StartNew(1);
            _cancellationToken = cancellationToken;
            _subscription = taskList.Add(async () =>
            {
                await SetFlagAndWaitClearing().ConfigureAwait(false);
            });
        }

        /// <summary>
        /// Set the flag and wait for it to be cleared.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task SetFlagAndWaitClearing()
        {
            lock (_lockObj)
            {
                if (_isDisposed)
                {
                    return;
                }
                if (_cancellationToken.IsCancellationRequested)
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
                if (_notifierSet.IsSet)
                {
                    throw new InvalidOperationException();
                }

                _hasValue = true;
                _notifierClear.Reset();
                _notifierSet.Set();
            }

            try
            {
                await _thread.RegisterWaitForSingleObjectAsync(_notifierClear.WaitHandle, _cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (ObjectDisposedException)
            {
                return;
            }
        }

        /// <summary>
        /// Wait for the flag to be set.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="OperationCanceledException"></exception>
        public async Task WaitSetting()
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
                    _hasValue = false;
                    _reading = false;
                    _notifierSet.Reset();
                    _notifierClear.Set();
                }
            }

            await _thread.RegisterWaitForSingleObjectAsync(_notifierSet.WaitHandle, _cancellationToken).ConfigureAwait(false);

            lock (_lockObj)
            {
                _reading = true;
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
                _hasValue = false;
                _reading = false;
                _notifierClear.Dispose();
                _notifierSet.Dispose();
            }

            _subscription.Dispose();
        }
    }
}
