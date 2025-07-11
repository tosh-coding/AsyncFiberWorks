using AsyncFiberWorks.Core;
using AsyncFiberWorks.Threading;
using System;
using System.Threading;

namespace AsyncFiberWorks.Windows.Timer
{
    /// <summary>
    /// Timer using WaitableTimerEx in Windows.
    /// This timer starts a dedicated thread.
    /// </summary>
    public class IntervalWaitableTimerEx : IIntervalTimer, IDisposable
    {
        readonly ConsumerThread _thread;
        readonly WaitableTimerEx _waitableTimer;
        readonly ManualResetEventSlim _resetEvent = new ManualResetEventSlim();
        WaitHandle[] _waitHandles = null;
        int _scheduled = 0;
        bool _disposed = false;

        object _lockObj { get { return _waitableTimer; } }

        /// <summary>
        /// Create a timer.
        /// </summary>
        public IntervalWaitableTimerEx()
        {
            _thread = ConsumerThread.StartNew();
            _waitableTimer = new WaitableTimerEx(manualReset: false);
        }

        /// <summary>
        /// Start a repeating timer.
        /// </summary>
        /// <param name="action">The process to be called when the timer expires.</param>
        /// <param name="firstIntervalMs">Initial wait time. Must be greater than or equal to 0.</param>
        /// <param name="intervalMs">The waiting interval time after the second time. Must be greater than 0.</param>
        /// <param name="token">A handle to cancel the timer.</param>
        public void ScheduleOnInterval(Action action, int firstIntervalMs, int intervalMs, CancellationToken token = default)
        {
            if (firstIntervalMs < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(firstIntervalMs), $"{nameof(firstIntervalMs)} must be greater than or equal to 0.");
            }
            if (intervalMs <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(intervalMs), $"{nameof(intervalMs)} must be greater than 0.");
            }

            var copiedAction = action;
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

                    _waitableTimer.Set(firstIntervalMs * -10000L, intervalMs);
                    while (true)
                    {
                        int index = WaitHandle.WaitAny(_waitHandles);
                        if (index == 0)
                        {
                            copiedAction();
                        }
                        else
                        {
                            _waitableTimer.Cancel();
                            break;
                        }
                    }

                    lock (_lockObj)
                    {
                        _scheduled -= 1;
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
