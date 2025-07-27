using AsyncFiberWorks.Threading;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncFiberWorks.Procedures
{
    /// <summary>
    /// Asynchronous context switching.
    /// </summary>
    public class FiberTaskWaiter : IDisposable
    {
        private readonly object _lockObj = new object();
        private IDisposable _unsubscriber;
        private bool _executionRequested;
        private bool _isDisposed;
        private readonly ManualResetEventSlim _notifierExecutionRequested = new ManualResetEventSlim();
        private readonly ManualResetEventSlim _notifierExecutionFinished = new ManualResetEventSlim();
        private readonly UserThreadPool _thread;
        private bool _inExecuting;
        private CancellationToken _cancellationTokenExternal;
        private readonly CancellationTokenSource _onDispose = new CancellationTokenSource();

        /// <summary>
        /// Register a task to the sequential task list.
        /// </summary>
        /// <param name="taskList"></param>
        /// <param name="cancellationToken"></param>
        public FiberTaskWaiter(ISequentialTaskListRegistry taskList, CancellationToken cancellationToken = default)
        {
            _thread = UserThreadPool.StartNew(1);
            _cancellationTokenExternal = cancellationToken;
            _unsubscriber = taskList.Add(ExecuteAsync);
        }

        /// <summary>
        /// Execute a task.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task ExecuteAsync()
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
                if (_inExecuting)
                {
                    throw new InvalidOperationException();
                }
                if (_notifierExecutionRequested.IsSet)
                {
                    throw new InvalidOperationException();
                }

                _executionRequested = true;
                _notifierExecutionFinished.Reset();
                _notifierExecutionRequested.Set();
            }

            try
            {
                var cancellation = CancellationTokenSource.CreateLinkedTokenSource(_cancellationTokenExternal, _onDispose.Token);
                await _thread.RegisterWaitForSingleObjectAsync(_notifierExecutionFinished.WaitHandle, cancellation.Token).ConfigureAwait(false);
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
        /// Wait for task execution.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="OperationCanceledException"></exception>
        public async Task ExecutionStarted()
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
                if (_executionRequested && _inExecuting)
                {
                    _executionRequested = false;
                    _inExecuting = false;
                    _notifierExecutionRequested.Reset();
                    _notifierExecutionFinished.Set();
                }
            }

            using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_cancellationTokenExternal, _onDispose.Token))
            {
                try
                {
                    await _thread.RegisterWaitForSingleObjectAsync(_notifierExecutionRequested.WaitHandle, linkedCts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    if (_cancellationTokenExternal.IsCancellationRequested)
                    {
                        _cancellationTokenExternal.ThrowIfCancellationRequested();
                    }
                    else
                    {
                        throw new ObjectDisposedException(nameof(FiberTaskWaiter));
                    }
                }
            }

            lock (_lockObj)
            {
                _inExecuting = true;
            }
        }

        /// <summary>
        /// Unregister.
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
                _inExecuting = false;
                _unsubscriber.Dispose();
                _onDispose.Cancel();
                _onDispose.Dispose();
                _notifierExecutionRequested.Dispose();
                _notifierExecutionFinished.Dispose();
            }
        }
    }
}
