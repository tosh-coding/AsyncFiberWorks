using AsyncFiberWorks.Threading;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncFiberWorks.Procedures
{
    /// <summary>
    /// Use "await" to simply wait for the start of a task on a sequential handler list.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SequentialHandlerWaiter<T> : IDisposable, ISequentialHandlerWaiter<T>
    {
        private readonly object _lockObj = new object();
        private bool _executionRequested;
        private bool _isDisposed;
        private readonly ManualResetEventSlim _notifierExecutionRequested = new ManualResetEventSlim();
        private readonly ManualResetEventSlim _notifierExecutionFinished = new ManualResetEventSlim();
        private readonly UserThreadPool _thread;
        private bool _isFirstTime = true;
        private bool _inWaiting;
        private CancellationToken _cancellationTokenExternal;
        private readonly CancellationTokenSource _onDispose = new CancellationTokenSource();
        private readonly ProcessedFlagEventArgs<T> _currentValue = new ProcessedFlagEventArgs<T>();
        private IDisposable _extraDisposable;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="cancellationToken"></param>
        public SequentialHandlerWaiter(CancellationToken cancellationToken = default)
        {
            _thread = UserThreadPool.StartNew(2);
            _cancellationTokenExternal = cancellationToken;
        }

        /// <summary>
        /// Register additional objects you want to destroy when Dispose is performed.
        /// </summary>
        /// <param name="disposable"></param>
        public void SetDisposable(IDisposable disposable)
        {
            _extraDisposable = disposable;
        }

        /// <summary>
        /// Execute a handler.
        /// </summary>
        /// <param name="arg">An argument.</param>
        /// <returns>Indicates whether it has been processed.</returns>
        public async Task<bool> Handler(T arg)
        {
            _currentValue.Processed = false;
            _currentValue.Arg = arg;
            await ExecuteAsync().ConfigureAwait(false);
            return _currentValue.Processed;
        }

        /// <summary>
        /// Execute a handler.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        async Task ExecuteAsync()
        {
            lock (_lockObj)
            {
                if (_isDisposed)
                {
                    return;
                }
                if (_cancellationTokenExternal.IsCancellationRequested)
                {
                    return;
                }
                if (_executionRequested)
                {
                    throw new InvalidOperationException();
                }

                _executionRequested = true;
                _notifierExecutionRequested.Set();
            }

            try
            {
                var cancellation = CancellationTokenSource.CreateLinkedTokenSource(_cancellationTokenExternal, _onDispose.Token);
                try
                {
                    await _thread.RegisterWaitForSingleObjectAsync(_notifierExecutionFinished.WaitHandle, cancellation.Token).ConfigureAwait(false);
                }
                finally
                {
                    lock (_lockObj)
                    {
                        _executionRequested = false;
                        _notifierExecutionFinished.Reset();
                    }
                }
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
                if (_cancellationTokenExternal.IsCancellationRequested)
                {
                    throw new OperationCanceledException();
                }
                if (_inWaiting)
                {
                    throw new InvalidOperationException();
                }
                _inWaiting = true;

                if (_isFirstTime)
                {
                    _isFirstTime = false;
                }
                else
                {
                    _notifierExecutionFinished.Set();
                }
            }

            using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_cancellationTokenExternal, _onDispose.Token))
            {
                try
                {
                    try
                    {
                        await _thread.RegisterWaitForSingleObjectAsync(_notifierExecutionRequested.WaitHandle, linkedCts.Token).ConfigureAwait(false);
                    }
                    finally
                    {
                        lock (_lockObj)
                        {
                            _inWaiting = false;
                            _notifierExecutionRequested.Reset();
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    if (_cancellationTokenExternal.IsCancellationRequested)
                    {
                        _cancellationTokenExternal.ThrowIfCancellationRequested();
                    }
                    else
                    {
                        throw new ObjectDisposedException(nameof(SequentialTaskWaiter));
                    }
                }
            }
            return _currentValue;
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

                _executionRequested = false;
                _onDispose.Cancel();
                _onDispose.Dispose();
                _notifierExecutionRequested.Dispose();
                _notifierExecutionFinished.Dispose();
            }
            _extraDisposable?.Dispose();
            _extraDisposable = null;
            _thread.Dispose();
        }
    }
}
