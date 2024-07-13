using AsyncFiberWorks.Core;
using AsyncFiberWorks.Fibers;
using System;
using System.Threading;

namespace AsyncFiberWorks.Windows.Timer
{
    /// <summary>
    /// Timer using WaitableTimerEx in Windows.
    /// This timer starts a dedicated thread.
    /// </summary>
    public class OneshotWaitableTimerEx : IOneshotTimer, IDisposable
    {
        readonly ThreadFiber _thread;
        readonly WaitableTimerEx _waitableTimer;
        readonly ManualResetEventSlim _resetEvent = new ManualResetEventSlim();
        WaitHandle[] _waitHandles = null;
        Action<object> _copiedAction;
        object _state;
        int _scheduled = 0;
        bool _disposed = false;

        object _lockObj { get { return _waitableTimer; } }

        /// <summary>
        /// Create a timer.
        /// </summary>
        public OneshotWaitableTimerEx()
        {
            _thread = new ThreadFiber();
            _waitableTimer = new WaitableTimerEx(manualReset: false);
        }

        /// <summary>
        /// Start a timer.
        /// </summary>
        /// <param name="action">The process to be called when the timer expires.</param>
        /// <param name="state">Arguments passed when that callback is invoked.</param>
        /// <param name="firstIntervalMs">Timer wait time. Must be greater than or equal to 0.</param>
        /// <param name="token">A handle to cancel the timer.</param>
        public void InternalSchedule(Action<object> action, object state, int firstIntervalMs, CancellationToken token = default)
        {
            if (firstIntervalMs < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(firstIntervalMs), $"{nameof(firstIntervalMs)} must be greater than or equal to 0.");
            }

            lock (_lockObj)
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(this.GetType().FullName);
                }
                if (_scheduled > 0)
                {
                    _resetEvent.Set();
                }
                _scheduled += 1;
                _copiedAction = action;
                _state = state;

                _thread.Enqueue(() =>
                {
                    lock (_lockObj)
                    {
                        if (_scheduled > 1)
                        {
                            _scheduled -= 1;
                            return;
                        }
                        if (_disposed)
                        {
                            return;
                        }
                        _resetEvent.Reset();
                        SetWaitHandles(token);
                    }

                    _waitableTimer.Set(firstIntervalMs * -10000L);
                    int index = WaitHandle.WaitAny(_waitHandles);
                    if (index == 0)
                    {
                        lock (_lockObj)
                        {
                            _scheduled -= 1;
                            _copiedAction(_state);
                        }
                    }
                    else
                    {
                        _waitableTimer.Cancel();
                        lock (_lockObj)
                        {
                            _scheduled -= 1;
                        }
                    }
                });
            }
        }

        private void SetWaitHandles(CancellationToken externalToken)
        {
            if (externalToken.CanBeCanceled)
            {
                const int needSize = 3;
                if ((_waitHandles?.Length ?? 0) != needSize)
                {
                    _waitHandles = new WaitHandle[needSize];
                }
                _waitHandles[0] = _waitableTimer;
                _waitHandles[1] = _resetEvent.WaitHandle;
                _waitHandles[2] = externalToken.WaitHandle;
            }
            else
            {
                const int needSize = 2;
                if ((_waitHandles?.Length ?? 0) != needSize)
                {
                    _waitHandles = new WaitHandle[needSize];
                }
                _waitHandles[0] = _waitableTimer;
                _waitHandles[1] = _resetEvent.WaitHandle;
            }
        }

        void DisposeResources()
        {
            _waitableTimer.Dispose();
            _resetEvent.Dispose();
            _thread.Dispose();
            _waitHandles = null;
        }

        /// <summary>
        /// Stop the timer.
        /// </summary>
        public void Dispose()
        {
            lock (_lockObj)
            {
                if (_disposed)
                {
                    return;
                }
                _disposed = true;
                _resetEvent.Set();
                _thread.Enqueue(DisposeResources);
            }
        }
    }
}
