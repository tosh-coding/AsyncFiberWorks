using AsyncFiberWorks.Channels;
using System;
using System.Threading;

namespace AsyncFiberWorks.Core
{
    internal sealed class TimerAction : IDisposable, IDisposableSubscriptionRegistry
    {
        private readonly object _lock = new object();
        private readonly Action _action;
        private readonly long _firstIntervalInMs;
        private readonly long _intervalInMs;
        private readonly Unsubscriber _unsubscriber = new Unsubscriber();

        private Timer _timer = null;
        private bool _canceled = false;

        public TimerAction(Action action, long firstIntervalInMs, long intervalInMs = Timeout.Infinite)
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
        }

        public static TimerAction StartNew(Action action, long firstIntervalInMs, long intervalInMs = Timeout.Infinite)
        {
            var timerAction = new TimerAction(action, firstIntervalInMs, intervalInMs);
            timerAction.Start();
            return timerAction;
        }

        public void Start()
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

        public Unsubscriber BeginSubscription()
        {
            return _unsubscriber.BeginSubscription();
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
            _unsubscriber.Dispose();
        }
    }
}