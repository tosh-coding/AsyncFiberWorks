using System;
using System.Threading;

namespace AsyncFiberWorks.Core
{
    internal sealed class TimerAction : IDisposable
    {
        private readonly object _lock = new object();
        private readonly Action _action;
        private readonly long _firstIntervalInMs;
        private readonly long _intervalInMs;
        private readonly Action _callbackOnDispose;

        private Timer _timer = null;
        private bool _canceled = false;

        private TimerAction(Action action, Action callbackOnDispose, long firstIntervalInMs, long intervalInMs = Timeout.Infinite)
        {
            if (firstIntervalInMs < 0)
            {
                firstIntervalInMs = 0;
            }
            if (intervalInMs <= 0)
            {
                intervalInMs = Timeout.Infinite;
            }
            _action = action;
            _firstIntervalInMs = firstIntervalInMs;
            _intervalInMs = intervalInMs;
            _callbackOnDispose = callbackOnDispose;
        }

        public static IDisposable StartNew(Action action, Action callbackOnDispose, long firstIntervalInMs, long intervalInMs = Timeout.Infinite)
        {
            var timerAction = new TimerAction(action, callbackOnDispose, firstIntervalInMs, intervalInMs);
            timerAction.Start();
            return timerAction;
        }

        private void Start()
        {
            lock (_lock)
            {
                if (_canceled)
                {
                    return;
                }
                if (_timer != null)
                {
                    return;
                }
                _timer = new Timer(x => ExecuteOnTimerThread(), null, _firstIntervalInMs, _intervalInMs);
            }
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
            if (_intervalInMs == Timeout.Infinite)
            {
                this.Dispose();
            }
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
            _callbackOnDispose?.Invoke();
        }
    }
}