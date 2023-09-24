using System;
using System.Threading;

namespace Retlang.Core
{
    internal sealed class TimerAction : IDisposable
    {
        private readonly object _lock = new object();
        private readonly Action _action;
        private readonly long _firstIntervalInMs;
        private readonly long _intervalInMs;
        private readonly IExecutionContext _fiber;
        private readonly ISubscriptionRegistry _fallbackDisposer;

        private Timer _timer = null;
        private bool _canceled = false;

        public TimerAction(IExecutionContext fiber, Action action, long firstIntervalInMs, long intervalInMs = Timeout.Infinite, ISubscriptionRegistry fallbackDisposer = null)
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
            _fiber = fiber;
            _fallbackDisposer = fallbackDisposer;
            fallbackDisposer?.RegisterSubscription(this);
        }

        public static TimerAction StartNew(IExecutionContext fiber, Action action, long firstIntervalInMs, long intervalInMs = Timeout.Infinite, ISubscriptionRegistry fallbackDisposer = null)
        {
            var timerAction = new TimerAction(fiber, action, firstIntervalInMs, intervalInMs, fallbackDisposer);
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

            _fiber.Enqueue(ExecuteOnFiberThread);
        }

        public void ExecuteOnFiberThread()
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
            _fallbackDisposer?.DeregisterSubscription(this);
        }
    }
}