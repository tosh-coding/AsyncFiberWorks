using System;
using System.Threading;

namespace AsyncFiberWorks.Core
{
    /// <summary>
    /// Wrapper class for System.Threading.Timer.
    /// </summary>
    public class OneshotThreadingTimer : IOneshotTimer, IDisposable
    {
        readonly object _lockObj = new object();
        readonly Timer _timer;
        bool _scheduled = false;
        bool _disposed = false;
        Action _copiedAction = null;
        CancellationTokenRegistration? _registration;

        /// <summary>
        /// Create a timer.
        /// </summary>
        public OneshotThreadingTimer()
        {
            _timer = new Timer(OnTimer);
        }

        /// <summary>
        /// Start a timer.
        /// </summary>
        /// <param name="action">The process to be called when the timer expires.</param>
        /// <param name="intervalMs">Timer wait time.</param>
        /// <param name="token">A handle to cancel the timer.</param>
        public void Schedule(Action action, int intervalMs, CancellationToken token)
        {
            if (intervalMs < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(intervalMs), $"{nameof(intervalMs)} must be greater than or equal to 0.");
            }

            lock (_lockObj)
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(this.GetType().FullName);
                }
                if (_scheduled)
                {
                    Cancel();
                }
                else
                {
                    _scheduled = true;
                }

                if (token.CanBeCanceled)
                {
                    _registration = token.Register(Cancel);
                }

                _copiedAction = action;
                _timer.Change(intervalMs, Timeout.Infinite);
            }
        }

        void OnTimer(object state)
        {
            lock (_lockObj)
            {
                if (!_scheduled)
                {
                    return;
                }
                _scheduled = false;
                if (_registration.HasValue)
                {
                    _registration.Value.Dispose();
                    _registration = null;
                }
            }

            _copiedAction();
        }

        void Cancel()
        {
            lock (_lockObj)
            {
                if (_disposed)
                {
                    return;
                }
                if (!_scheduled)
                {
                    return;
                }
                _scheduled = false;
                if (_registration.HasValue)
                {
                    _registration.Value.Dispose();
                    _registration = null;
                }
                _timer.Change(Timeout.Infinite, Timeout.Infinite);
            }
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
            }
            _timer.Dispose();
        }
    }
}
