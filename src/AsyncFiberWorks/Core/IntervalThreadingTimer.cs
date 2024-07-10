using System;
using System.Threading;

namespace AsyncFiberWorks.Core
{
    /// <summary>
    /// Wrapper class for System.Threading.Timer.
    /// </summary>
    public class IntervalThreadingTimer : IIntervalTimer, IDisposable
    {
        readonly object _lockObj = new object();
        readonly Timer _timer;
        bool _scheduled = false;
        bool _disposed = false;
        Action _copiedAction = null;
        CancellationTokenRegistration? _tokenRegistration;

        /// <summary>
        /// Create a timer.
        /// </summary>
        public IntervalThreadingTimer()
        {
            _timer = new Timer(OnTimer);
        }

        /// <summary>
        /// Start a repeating timer.
        /// </summary>
        /// <param name="action">The process to be called when the timer expires.</param>
        /// <param name="firstIntervalMs">Initial wait time.</param>
        /// <param name="intervalMs">The waiting interval time after the second time.</param>
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
            if (token.CanBeCanceled && token.IsCancellationRequested)
            {
                return;
            }

            lock (_lockObj)
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(this.GetType().FullName);
                }
                if (_scheduled)
                {
                    throw new InvalidOperationException("This timer has already started.");
                }
                _scheduled = true;

                if (_tokenRegistration.HasValue)
                {
                    _tokenRegistration.Value.Dispose();
                    _tokenRegistration = null;
                }
                _copiedAction = action;

                _timer.Change(firstIntervalMs, intervalMs);
                if (token.CanBeCanceled)
                {
                    _tokenRegistration = token.Register(() =>
                    {
                        lock (_lockObj)
                        {
                            if (_scheduled)
                            {
                                _timer.Change(Timeout.Infinite, Timeout.Infinite);
                                _scheduled = false;
                            }
                        }
                    }, false);
                }
            }
        }

        void OnTimer(object state)
        {
            lock (_lockObj)
            {
                if ((!_scheduled) || _disposed)
                {
                    return;
                }
            }
            _copiedAction();
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
                if (_tokenRegistration.HasValue)
                {
                    _tokenRegistration.Value.Dispose();
                    _tokenRegistration = null;
                }
                _timer.Change(Timeout.Infinite, Timeout.Infinite);
                _timer.Dispose();
            }
        }
    }
}
