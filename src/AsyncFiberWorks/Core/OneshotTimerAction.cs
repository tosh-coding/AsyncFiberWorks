using System;
using System.Threading;

namespace AsyncFiberWorks.Core
{
    internal sealed class OneshotTimerAction : IDisposable
    {
        private readonly object _lock = new object();
        private readonly Action _action;

        private Timer _timer = null;
        private bool _canceled = false;

        private OneshotTimerAction(Action action, long firstIntervalInMs)
        {
            if (firstIntervalInMs < 0)
            {
                firstIntervalInMs = 0;
            }
            _action = action;
            _timer = new Timer(ExecuteOnTimerThread, null, firstIntervalInMs, Timeout.Infinite);
        }

        public static IDisposable StartNew(Action action, long firstIntervalInMs)
        {
            return new OneshotTimerAction(action, firstIntervalInMs);
        }

        private void ExecuteOnTimerThread(object state)
        {
            lock (_lock)
            {
                if (_canceled)
                {
                    return;
                }
            }

            _action();
            this.Dispose();
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