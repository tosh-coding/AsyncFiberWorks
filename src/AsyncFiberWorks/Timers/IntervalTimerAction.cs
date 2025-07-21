using System;
using System.Threading;

namespace AsyncFiberWorks.Timers
{
    internal sealed class IntervalTimerAction : IDisposable
    {
        private readonly object _lock = new object();
        private readonly Action _action;

        private Timer _timer = null;
        private bool _canceled = false;

        private IntervalTimerAction(Action action, long firstIntervalInMs, long intervalInMs)
        {
            if (firstIntervalInMs < 0)
            {
                firstIntervalInMs = 0;
            }
            if (intervalInMs <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(intervalInMs));
            }
            _action = action;
            _timer = new Timer(x => ExecuteOnTimerThread(), null, firstIntervalInMs, intervalInMs);
        }

        public static IDisposable StartNew(Action action, long firstIntervalInMs, long intervalInMs)
        {
            return new IntervalTimerAction(action, firstIntervalInMs, intervalInMs);
        }

        private void ExecuteOnTimerThread()
        {
            lock (_lock)
            {
                if (_canceled)
                {
                    return;
                }
            }

            _action();
        }

        public void Dispose()
        {
            lock (_lock)
            {
                if (_canceled)
                {
                    return;
                }
                _canceled = true;
            }
            if (_timer != null)
            {
                _timer.Dispose();
            }
        }
    }
}