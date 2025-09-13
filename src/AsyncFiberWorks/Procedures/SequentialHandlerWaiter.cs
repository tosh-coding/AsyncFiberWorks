﻿using AsyncFiberWorks.Threading;
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
        private IDisposable _unsubscriber;
        private bool _executionRequested;
        private bool _isDisposed;
        private readonly ManualResetEventSlim _notifierExecutionRequested = new ManualResetEventSlim();
        private readonly ManualResetEventSlim _notifierExecutionFinished = new ManualResetEventSlim();
        private readonly UserThreadPool _thread;
        private bool _inExecuting;
        private CancellationToken _cancellationTokenExternal;
        private readonly CancellationTokenSource _onDispose = new CancellationTokenSource();
        private readonly ProcessedFlagEventArgs<T> _currentValue = new ProcessedFlagEventArgs<T>();

        /// <summary>
        /// Register a handler to the sequential handler list.
        /// </summary>
        /// <param name="handlerList"></param>
        /// <param name="cancellationToken"></param>
        public SequentialHandlerWaiter(ISequentialHandlerListRegistry<T> handlerList, CancellationToken cancellationToken = default)
        {
            _thread = UserThreadPool.StartNew(1);
            _cancellationTokenExternal = cancellationToken;
            _unsubscriber = handlerList.Add(async (arg) =>
            {
                _currentValue.Processed = false;
                _currentValue.Arg = arg;
                await ExecuteAsync().ConfigureAwait(false);
                return _currentValue.Processed;
            });

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
                        throw new ObjectDisposedException(nameof(SequentialTaskWaiter));
                    }
                }
            }

            lock (_lockObj)
            {
                _inExecuting = true;
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
